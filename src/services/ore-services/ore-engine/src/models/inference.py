"""Inference engine for object detection"""

import logging
import time
from typing import Dict, List, Tuple

import numpy as np
import torch
from ultralytics import YOLO

from ..config.settings import InferenceConfig
from ..utils.image import decode_image

logger = logging.getLogger(__name__)


class InferenceEngine:
    """Handles ML inference for object detection"""
    
    def __init__(self, model: YOLO, config: InferenceConfig):
        """
        Initialize inference engine
        
        Args:
            model: Loaded YOLO model
            config: Inference configuration
        """
        self.model = model
        self.config = config
        
        logger.info(f"Inference engine initialized on device: {config.device}")
    
    def detect_objects(
        self,
        image_data: bytes,
        conf_threshold: float = None,
        iou_threshold: float = None,
        max_detections: int = None
    ) -> Tuple[List[Dict], int]:
        """
        Perform object detection on image
        
        Args:
            image_data: Image bytes
            conf_threshold: Confidence threshold (overrides config)
            iou_threshold: IoU threshold for NMS
            max_detections: Maximum number of detections
        
        Returns:
            Tuple of (detections list, processing time in ms)
        """
        start_time = time.time()
        
        # Use config defaults if not provided
        conf_threshold = conf_threshold or self.config.confidence_threshold
        iou_threshold = iou_threshold or self.config.iou_threshold
        max_detections = max_detections or self.config.max_detections
        
        try:
            # Decode image
            img = decode_image(image_data)
            h, w = img.shape[:2]
            
            # Run inference
            results = self.model.predict(
                img,
                conf=conf_threshold,
                iou=iou_threshold,
                max_det=max_detections,
                verbose=False
            )
            
            # Parse results
            detections = []
            if len(results) > 0:
                result = results[0]
                
                if result.boxes is not None and len(result.boxes) > 0:
                    boxes = result.boxes
                    
                    for i in range(len(boxes)):
                        # Get box coordinates (xyxy format)
                        box = boxes.xyxy[i].cpu().numpy()
                        x1, y1, x2, y2 = box
                        
                        # Normalize coordinates
                        norm_x = x1 / w
                        norm_y = y1 / h
                        norm_width = (x2 - x1) / w
                        norm_height = (y2 - y1) / h
                        
                        # Get class and confidence
                        cls_id = int(boxes.cls[i].item())
                        confidence = float(boxes.conf[i].item())
                        
                        # Get class name
                        class_name = self.model.names[cls_id]
                        
                        detection = {
                            "class_name": class_name,
                            "confidence": confidence,
                            "bbox": {
                                "x": float(norm_x),
                                "y": float(norm_y),
                                "width": float(norm_width),
                                "height": float(norm_height)
                            },
                            "attributes": {}
                        }
                        
                        detections.append(detection)
            
            # Calculate processing time
            processing_time_ms = int((time.time() - start_time) * 1000)
            
            logger.debug(
                f"Detected {len(detections)} objects in {processing_time_ms}ms"
            )
            
            return detections, processing_time_ms
            
        except Exception as e:
            logger.error(f"Inference error: {e}", exc_info=True)
            raise
    
    def filter_detections_by_classes(
        self,
        detections: List[Dict],
        target_classes: List[str]
    ) -> List[Dict]:
        """
        Filter detections by target classes
        
        Args:
            detections: List of detections
            target_classes: List of target class names
        
        Returns:
            Filtered detections
        """
        if not target_classes:
            return detections
        
        filtered = [
            det for det in detections
            if det["class_name"] in target_classes
        ]
        
        logger.debug(
            f"Filtered {len(detections)} -> {len(filtered)} detections"
        )
        
        return filtered
