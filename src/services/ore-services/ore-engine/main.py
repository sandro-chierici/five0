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
from src.models.loader import ModelLoader
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

    # Start consuming
    logger.info("Starting frame consumption...")
    try:
        # Run consumer in background task
        consume_task = asyncio.create_task(consumer.start())
        
        # Wait for shutdown signal
        await shutdown_event.wait()
        
        # Stop consumer
        logger.info("Stopping consumer...")
        await consumer.stop()
        
        # Wait for consume task to finish
        await consume_task

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
