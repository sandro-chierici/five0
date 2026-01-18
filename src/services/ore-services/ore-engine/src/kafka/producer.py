"""Kafka producer for publishing detection results"""

import json
import logging
from typing import Dict

from confluent_kafka import Producer

from ..config.settings import KafkaConfig

logger = logging.getLogger(__name__)


class ResultProducer:
    """Kafka producer for detection results"""
    
    def __init__(self, config: KafkaConfig):
        """
        Initialize result producer
        
        Args:
            config: Kafka configuration
        """
        self.config = config
        self.topic = config.topics['detection_results']
        
        # Create producer
        producer_config = {
            'bootstrap.servers': ','.join(config.brokers),
            'compression.type': 'lz4',
            'linger.ms': 10,
            'batch.size': 16384,
        }
        
        self.producer = Producer(producer_config)
        logger.info(f"Kafka producer initialized: {self.topic}")
    
    def publish_result(self, result: Dict):
        """
        Publish detection result to Kafka
        
        Args:
            result: Detection result dictionary
        """
        try:
            # Serialize to JSON
            value = json.dumps(result).encode('utf-8')
            
            # Get session ID for partitioning
            session_id = result.get('session_id', '')
            key = session_id.encode('utf-8')
            
            # Produce message
            self.producer.produce(
                topic=self.topic,
                key=key,
                value=value,
                callback=self._delivery_callback
            )
            
            # Trigger delivery (async)
            self.producer.poll(0)
            
        except Exception as e:
            logger.error(f"Failed to publish result: {e}", exc_info=True)
            raise
    
    def flush(self, timeout: float = 5.0):
        """
        Flush pending messages
        
        Args:
            timeout: Flush timeout in seconds
        """
        remaining = self.producer.flush(timeout)
        if remaining > 0:
            logger.warning(f"{remaining} messages not delivered before timeout")
    
    def close(self):
        """Close the producer"""
        self.flush()
        logger.info("Kafka producer closed")
    
    def _delivery_callback(self, err, msg):
        """Callback for message delivery confirmation"""
        if err:
            logger.error(f"Message delivery failed: {err}")
        else:
            logger.debug(
                f"Message delivered to {msg.topic()} [{msg.partition()}] "
                f"at offset {msg.offset()}"
            )
