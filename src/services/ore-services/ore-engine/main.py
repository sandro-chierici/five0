#!/usr/bin/env python3
"""
Object Recognition Engine - Main Entry Point
"""

import asyncio
import logging
import signal
import sys
from pathlib import Path

from src.config.settings import Settings
from src.kafka.consumer import FrameConsumer
from src.kafka.producer import ResultProducer
from src.kafka.model_consumer import ModelCommandConsumer
from src.models.loader import ModelLoader
from src.models.dynamic_manager import DynamicModelManager
from src.processors.frame_processor import FrameProcessor
from src.storage.minio_client import MinIOClient
from src.utils.logger import setup_logger

# Global flag for graceful shutdown
shutdown_event = asyncio.Event()


def signal_handler(signum, frame):
    """Handle shutdown signals"""
    logging.info(f"Received signal {signum}, initiating graceful shutdown...")
    shutdown_event.set()


async def main():
    """Main application entry point"""
    # Setup logging
    logger = setup_logger("ore-engine")
    logger.info("Starting Object Recognition Engine...")

    # Load configuration
    try:
        config = Settings.from_yaml("config.yaml")
        logger.info("Configuration loaded successfully")
    except Exception as e:
        logger.error(f"Failed to load configuration: {e}")
        sys.exit(1)

    # Initialize components
    try:
        # MinIO client
        logger.info("Initializing MinIO client...")
        minio_client = MinIOClient(config.minio)
        await minio_client.ensure_buckets()

        # Model loader
        logger.info("Initializing model loader...")
        model_loader = ModelLoader(config.inference, config.models)
        
        # Load default model
        logger.info(f"Loading default model: {config.inference.default_model_id}")
        model_loader.load_model(config.inference.default_model_id)

        # Initialize dynamic model manager
        logger.info("Initializing dynamic model manager...")
        dynamic_model_manager = DynamicModelManager(
            minio_client=minio_client,
            inference_config=config.inference,
            minio_config=config.minio
        )

        # Initialize model command consumer (Kafka-based model management)
        model_control_topic = config.kafka.topics.get("model_control", "model-control")
        model_response_topic = config.kafka.topics.get("model_control_response", "model-control-response")
        logger.info(f"Initializing model command consumer on topic: {model_control_topic}")
        model_command_consumer = ModelCommandConsumer(
            config=config.kafka,
            model_manager=dynamic_model_manager,
            command_topic=model_control_topic,
            response_topic=model_response_topic
        )

        # Kafka producer
        logger.info("Initializing Kafka producer...")
        producer = ResultProducer(config.kafka)

        # Frame processor
        logger.info("Initializing frame processor...")
        processor = FrameProcessor(
            model_loader=model_loader,
            storage_client=minio_client,
            kafka_producer=producer,
            config=config.inference
        )

        # Kafka consumer
        logger.info("Initializing Kafka consumer...")
        consumer = FrameConsumer(
            config=config.kafka,
            processor=processor
        )

    except Exception as e:
        logger.error(f"Failed to initialize components: {e}")
        sys.exit(1)

    # Register signal handlers
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)

    # Start services
    logger.info("Starting services...")
    try:
        # Start model command consumer in background
        model_command_task = asyncio.create_task(model_command_consumer.start())
        
        # Run frame consumer in background task
        consume_task = asyncio.create_task(consumer.start())
        
        # Wait for shutdown signal
        await shutdown_event.wait()
        
        # Stop consumers
        logger.info("Stopping frame consumer...")
        await consumer.stop()
        
        logger.info("Stopping model command consumer...")
        await model_command_consumer.stop()
        
        # Wait for tasks to finish
        await asyncio.gather(consume_task, model_command_task, return_exceptions=True)

    except Exception as e:
        logger.error(f"Error during execution: {e}", exc_info=True)
    finally:
        # Cleanup
        logger.info("Cleaning up resources...")
        producer.close()
        logger.info("Object Recognition Engine stopped")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nShutdown requested by user")
    except Exception as e:
        print(f"Fatal error: {e}", file=sys.stderr)
        sys.exit(1)
