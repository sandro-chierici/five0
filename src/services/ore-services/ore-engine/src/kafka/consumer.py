"""Kafka consumer for frame references"""

import asyncio
import json
import logging
from typing import Optional

from confluent_kafka import Consumer, KafkaError, KafkaException

from ..config.settings import KafkaConfig
from ..processors.frame_processor import FrameProcessor

logger = logging.getLogger(__name__)


class FrameConsumer:
    """Kafka consumer for processing frame references"""
    
    def __init__(self, config: KafkaConfig, processor: FrameProcessor):
        """
        Initialize frame consumer
        
        Args:
            config: Kafka configuration
            processor: Frame processor instance
        """
        self.config = config
        self.processor = processor
        self.topic = config.topics['frame_input']
        self.running = False
        
        # Create consumer
        consumer_config = {
            'bootstrap.servers': ','.join(config.brokers),
            'group.id': config.consumer_group,
            'auto.offset.reset': config.auto_offset_reset,
            'enable.auto.commit': False,
            'max.poll.interval.ms': config.max_poll_interval_ms,
        }
        
        self.consumer = Consumer(consumer_config)
        logger.info(f"Kafka consumer initialized: {self.topic}")
    
    async def start(self):
        """Start consuming messages"""
        self.running = True
        
        try:
            # Subscribe to topic
            self.consumer.subscribe([self.topic])
            logger.info(f"Subscribed to topic: {self.topic}")
            
            while self.running:
                # Poll for messages
                msg = self.consumer.poll(timeout=1.0)
                
                if msg is None:
                    continue
                
                if msg.error():
                    if msg.error().code() == KafkaError._PARTITION_EOF:
                        continue
                    else:
                        raise KafkaException(msg.error())
                
                # Process message
                await self._process_message(msg)
                
                # Commit offset
                self.consumer.commit(msg)
                
        except KeyboardInterrupt:
            logger.info("Consumer interrupted")
        except Exception as e:
            logger.error(f"Consumer error: {e}", exc_info=True)
        finally:
            self.consumer.close()
            logger.info("Kafka consumer closed")
    
    async def stop(self):
        """Stop consuming messages"""
        logger.info("Stopping consumer...")
        self.running = False
    
    async def _process_message(self, msg):
        """
        Process a single Kafka message
        
        Args:
            msg: Kafka message
        """
        try:
            # Parse JSON message
            frame_ref = json.loads(msg.value().decode('utf-8'))
            
            logger.debug(
                f"Processing frame: session={frame_ref.get('session_id')}, "
                f"frame={frame_ref.get('frame_id')}"
            )
            
            # Process frame
            await self.processor.process_frame(frame_ref)
            
        except json.JSONDecodeError as e:
            logger.error(f"Failed to parse message: {e}")
        except Exception as e:
            logger.error(f"Failed to process frame: {e}", exc_info=True)
