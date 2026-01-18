"""Configuration management for the Object Recognition Engine"""

import os
from dataclasses import dataclass, field
from typing import Dict, List

import yaml


@dataclass
class KafkaConfig:
    brokers: List[str]
    topics: Dict[str, str]
    consumer_group: str
    auto_offset_reset: str = "earliest"
    max_poll_interval_ms: int = 300000

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class RedisConfig:
    url: str
    db: int = 0
    pool_size: int = 10

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class MinIOConfig:
    endpoint: str
    access_key: str
    secret_key: str
    secure: bool = False
    bucket_frames: str = "ore-frames"
    bucket_models: str = "ore-models"

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class InferenceConfig:
    device: str = "cpu"
    batch_size: int = 1
    max_batch_wait: int = 100
    model_cache_size: int = 3
    default_model_id: str = "yolov8n"
    confidence_threshold: float = 0.5
    iou_threshold: float = 0.45
    max_detections: int = 100

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class ModelConfig:
    model_id: str
    path: str
    framework: str
    classes: List[str] = field(default_factory=list)

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class PerformanceConfig:
    workers: int = 4
    prefetch_count: int = 10
    processing_timeout: int = 30

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class LoggingConfig:
    level: str = "INFO"
    format: str = "json"

    @classmethod
    def from_dict(cls, data: dict):
        return cls(**data)


@dataclass
class Settings:
    kafka: KafkaConfig
    redis: RedisConfig
    minio: MinIOConfig
    inference: InferenceConfig
    models: Dict[str, ModelConfig]
    performance: PerformanceConfig
    logging: LoggingConfig

    @classmethod
    def from_yaml(cls, filepath: str):
        """Load settings from YAML file"""
        with open(filepath, 'r') as f:
            data = yaml.safe_load(f)

        # Override with environment variables
        cls._override_from_env(data)

        # Parse nested configs
        kafka = KafkaConfig.from_dict(data['kafka'])
        redis = RedisConfig.from_dict(data['redis'])
        minio = MinIOConfig.from_dict(data['minio'])
        inference = InferenceConfig.from_dict(data['inference'])
        performance = PerformanceConfig.from_dict(data.get('performance', {}))
        logging_cfg = LoggingConfig.from_dict(data.get('logging', {}))

        # Parse models
        models = {}
        for name, model_data in data.get('models', {}).items():
            models[name] = ModelConfig.from_dict(model_data)

        return cls(
            kafka=kafka,
            redis=redis,
            minio=minio,
            inference=inference,
            models=models,
            performance=performance,
            logging=logging_cfg
        )

    @staticmethod
    def _override_from_env(data: dict):
        """Override configuration with environment variables"""
        # Kafka
        if brokers := os.getenv("KAFKA_BROKERS"):
            data['kafka']['brokers'] = brokers.split(',')

        # Redis
        if redis_url := os.getenv("REDIS_URL"):
            data['redis']['url'] = redis_url

        # MinIO
        if minio_endpoint := os.getenv("MINIO_ENDPOINT"):
            data['minio']['endpoint'] = minio_endpoint
        if minio_access := os.getenv("MINIO_ACCESS_KEY"):
            data['minio']['access_key'] = minio_access
        if minio_secret := os.getenv("MINIO_SECRET_KEY"):
            data['minio']['secret_key'] = minio_secret
        if minio_secure := os.getenv("MINIO_USE_SSL"):
            data['minio']['secure'] = minio_secure.lower() == 'true'

        # Inference
        if device := os.getenv("DEVICE"):
            data['inference']['device'] = device

        # Logging
        if log_level := os.getenv("LOG_LEVEL"):
            data['logging']['level'] = log_level
