"""Frame processor - main processing pipeline"""

import logging
import socket
import time
from typing import Dict

from ..config.settings import InferenceConfig
from ..kafka.producer import ResultProducer
from ..models.inference import InferenceEngine
from ..models.loader import ModelLoader
from ..storage.minio_client import MinIOClient

logger = logging.getLogger(__name__)


class FrameProcessor:
    """Processes frames for object detection"""
    
    def __init__(
        self,
        model_loader: ModelLoader,
        storage_client: MinIOClient,
        kafka_producer: ResultProducer,
        config: InferenceConfig
    ):
        """
        Initialize frame processor
        
        Args:
            model_loader: Model loader instance
            storage_client: MinIO client
            kafka_producer: Kafka producer for results
            config: Inference configuration
        """
        self.model_loader = model_loader
        self.storage_client = storage_client
        self.kafka_producer = kafka_producer
        self.config = config
        self.hostname = socket.gethostname()
        
        # Load default model
        self.current_model_id = config.default_model_id
        model = self.model_loader.load_model(self.current_model_id)
        self.inference_engine = InferenceEngine(model, config)
        
        logger.info(f"Frame processor initialized on {self.hostname}")
    
    async def process_frame(self, frame_ref: Dict):
        """
        Process a single frame
        
        Args:
            frame_ref: Frame reference from Kafka
        """
        session_id = frame_ref.get('session_id')
        frame_id = frame_ref.get('frame_id')
        storage_url = frame_ref.get('storage_url')
        timestamp_ms = frame_ref.get('timestamp_ms', int(time.time() * 1000))
        
        logger.info(f"Processing frame {frame_id} for session {session_id}")
        
        try:
            # Download frame from MinIO
            frame_data = self.storage_client.download_frame(storage_url)
            
            # Get session config
            session_config = frame_ref.get('config', {})
            conf_threshold = session_config.get('confidence_threshold', self.config.confidence_threshold)
            target_classes = session_config.get('target_classes', [])
            max_detections = session_config.get('max_detections_per_frame', self.config.max_detections)
            
            # Check if model needs to be switched
            model_id = frame_ref.get('model_id', self.current_model_id)
            if model_id != self.current_model_id:
                logger.info(f"Switching model to: {model_id}")
                model = self.model_loader.load_model(model_id)
                self.inference_engine = InferenceEngine(model, self.config)
                self.current_model_id = model_id
            
            # Run inference
            detections, processing_time_ms = self.inference_engine.detect_objects(
                frame_data,
                conf_threshold=conf_threshold,
                max_detections=max_detections
            )
            
            # Filter by target classes if specified
            if target_classes:
                detections = self.inference_engine.filter_detections_by_classes(
                    detections, target_classes
                )
            
            # Create result
            result = {
                'session_id': session_id,
                'frame_id': frame_id,
                'timestamp_ms': timestamp_ms,
                'processing_time_ms': processing_time_ms,
                'detections': detections,
                'model_version': self.current_model_id,
                'metadata': {
                    'total_objects': len(detections),
                    'avg_confidence': (
                        sum(d['confidence'] for d in detections) / len(detections)
                        if detections else 0.0
                    ),
                    'processing_node': self.hostname
                }
            }
            
            # Publish result to Kafka
            self.kafka_producer.publish_result(result)
            
            logger.info(
                f"Frame {frame_id} processed: {len(detections)} objects in {processing_time_ms}ms"
            )
            
        except Exception as e:
            logger.error(
                f"Error processing frame {frame_id} for session {session_id}: {e}",
                exc_info=True
            )
            
            # Publish error result
            error_result = {
                'session_id': session_id,
                'frame_id': frame_id,
                'timestamp_ms': timestamp_ms,
                'processing_time_ms': 0,
                'detections': [],
                'model_version': self.current_model_id,
                'error': str(e),
                'metadata': {
                    'total_objects': 0,
                    'avg_confidence': 0.0,
                    'processing_node': self.hostname
                }
            }
            
            self.kafka_producer.publish_result(error_result)
