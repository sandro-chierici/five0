"""Dynamic model manager for loading/updating YOLO models from MinIO"""

import asyncio
import json
import logging
import os
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional

import torch
from ultralytics import YOLO

from ..config.settings import MinIOConfig, InferenceConfig
from ..storage.minio_client import MinIOClient

logger = logging.getLogger(__name__)


@dataclass
class DynamicModelConfig:
    """Configuration for a dynamically loaded model"""
    model_id: str
    name: str
    description: str
    framework: str
    classes: List[str]
    weights_path: str
    classes_path: str
    created_at: str
    updated_at: str
    local_path: Optional[str] = None


class DynamicModelManager:
    """Manages dynamic loading and updating of YOLO models"""
    
    def __init__(
        self,
        minio_client: MinIOClient,
        inference_config: InferenceConfig,
        minio_config: MinIOConfig
    ):
        """
        Initialize dynamic model manager
        
        Args:
            minio_client: MinIO client for storage operations
            inference_config: Inference configuration
            minio_config: MinIO configuration
        """
        self.minio_client = minio_client
        self.inference_config = inference_config
        self.minio_config = minio_config
        
        self.device = inference_config.device
        if self.device == "cuda" and not torch.cuda.is_available():
            logger.warning("CUDA not available, falling back to CPU")
            self.device = "cpu"
        
        # Cache for loaded models
        self._models: Dict[str, YOLO] = {}
        self._model_configs: Dict[str, DynamicModelConfig] = {}
        
        # Local cache directory
        self._cache_dir = Path(tempfile.gettempdir()) / "ore-models"
        self._cache_dir.mkdir(parents=True, exist_ok=True)
        
        logger.info(f"Dynamic model manager initialized, device: {self.device}")
        logger.info(f"Model cache directory: {self._cache_dir}")
    
    async def refresh_model_list(self) -> List[str]:
        """
        Refresh list of available models from MinIO
        
        Returns:
            List of available model IDs
        """
        model_ids = []
        
        try:
            # List all objects in models prefix
            bucket = self.minio_config.bucket_models
            prefix = "models/"
            
            objects = self.minio_client.client.list_objects(
                bucket, prefix=prefix, recursive=True
            )
            
            for obj in objects:
                if obj.object_name.endswith("/metadata.json"):
                    # Extract model ID from path: models/{model_id}/metadata.json
                    parts = obj.object_name.split("/")
                    if len(parts) >= 3:
                        model_id = parts[1]
                        model_ids.append(model_id)
            
            logger.info(f"Found {len(model_ids)} models in storage")
            return model_ids
            
        except Exception as e:
            logger.error(f"Failed to refresh model list: {e}")
            return []
    
    async def get_model_config(self, model_id: str) -> Optional[DynamicModelConfig]:
        """
        Get model configuration from MinIO
        
        Args:
            model_id: Model identifier
        
        Returns:
            Model configuration or None
        """
        try:
            bucket = self.minio_config.bucket_models
            metadata_path = f"models/{model_id}/metadata.json"
            
            response = self.minio_client.client.get_object(bucket, metadata_path)
            metadata = json.loads(response.read().decode('utf-8'))
            response.close()
            response.release_conn()
            
            config = DynamicModelConfig(
                model_id=metadata.get("model_id", model_id),
                name=metadata.get("name", model_id),
                description=metadata.get("description", ""),
                framework=metadata.get("framework", "yolov8"),
                classes=metadata.get("classes", []),
                weights_path=metadata.get("weights_path", f"models/{model_id}/weights.pt"),
                classes_path=metadata.get("classes_path", f"models/{model_id}/classes.json"),
                created_at=metadata.get("created_at", ""),
                updated_at=metadata.get("updated_at", "")
            )
            
            return config
            
        except Exception as e:
            logger.error(f"Failed to get model config for {model_id}: {e}")
            return None
    
    async def download_model_weights(self, model_id: str) -> Optional[str]:
        """
        Download model weights from MinIO to local cache
        
        Args:
            model_id: Model identifier
        
        Returns:
            Local path to weights file or None
        """
        config = await self.get_model_config(model_id)
        if not config:
            logger.error(f"Model config not found: {model_id}")
            return None
        
        try:
            bucket = self.minio_config.bucket_models
            local_path = self._cache_dir / model_id / "weights.pt"
            local_path.parent.mkdir(parents=True, exist_ok=True)
            
            logger.info(f"Downloading weights for model {model_id}...")
            
            self.minio_client.client.fget_object(
                bucket,
                config.weights_path,
                str(local_path)
            )
            
            logger.info(f"Downloaded weights to {local_path}")
            return str(local_path)
            
        except Exception as e:
            logger.error(f"Failed to download weights for {model_id}: {e}")
            return None
    
    async def load_model(self, model_id: str, force_reload: bool = False) -> Optional[YOLO]:
        """
        Load a model dynamically from MinIO
        
        Args:
            model_id: Model identifier
            force_reload: Force reloading even if cached
        
        Returns:
            Loaded YOLO model or None
        """
        # Check cache first
        if not force_reload and model_id in self._models:
            logger.debug(f"Model {model_id} loaded from cache")
            return self._models[model_id]
        
        # Get model config
        config = await self.get_model_config(model_id)
        if not config:
            logger.error(f"Model not found: {model_id}")
            return None
        
        # Download weights
        local_path = await self.download_model_weights(model_id)
        if not local_path or not os.path.exists(local_path):
            logger.error(f"Failed to download weights for {model_id}")
            return None
        
        try:
            # Load YOLO model
            logger.info(f"Loading model {model_id} from {local_path}")
            model = YOLO(local_path)
            model.to(self.device)
            
            # Update class names if provided
            if config.classes:
                # Create a mapping from original names to new names
                model.names = {i: name for i, name in enumerate(config.classes)}
                logger.info(f"Set {len(config.classes)} custom classes for model {model_id}")
            
            # Cache model and config
            self._models[model_id] = model
            self._model_configs[model_id] = config
            config.local_path = local_path
            
            # Evict old models if cache is full
            if len(self._models) > self.inference_config.model_cache_size:
                oldest_key = next(iter(self._models))
                if oldest_key != model_id:
                    del self._models[oldest_key]
                    if oldest_key in self._model_configs:
                        del self._model_configs[oldest_key]
                    logger.info(f"Evicted model from cache: {oldest_key}")
            
            logger.info(f"Model {model_id} loaded successfully")
            return model
            
        except Exception as e:
            logger.error(f"Failed to load model {model_id}: {e}", exc_info=True)
            return None
    
    async def reload_model(self, model_id: str) -> bool:
        """
        Reload a model from MinIO (useful after weights/classes update)
        
        Args:
            model_id: Model identifier
        
        Returns:
            True if reload successful
        """
        # Remove from cache
        if model_id in self._models:
            del self._models[model_id]
        if model_id in self._model_configs:
            del self._model_configs[model_id]
        
        # Load fresh
        model = await self.load_model(model_id, force_reload=True)
        return model is not None
    
    async def update_model_classes(self, model_id: str, classes: List[str]) -> bool:
        """
        Update classes for a loaded model
        
        Args:
            model_id: Model identifier
            classes: New list of class names
        
        Returns:
            True if update successful
        """
        if model_id not in self._models:
            logger.warning(f"Model {model_id} not in cache, loading first")
            model = await self.load_model(model_id)
            if not model:
                return False
        else:
            model = self._models[model_id]
        
        try:
            # Update class names
            model.names = {i: name for i, name in enumerate(classes)}
            
            if model_id in self._model_configs:
                self._model_configs[model_id].classes = classes
            
            logger.info(f"Updated classes for model {model_id}: {len(classes)} classes")
            return True
            
        except Exception as e:
            logger.error(f"Failed to update classes for {model_id}: {e}")
            return False
    
    def get_model(self, model_id: str) -> Optional[YOLO]:
        """
        Get a cached model (synchronous)
        
        Args:
            model_id: Model identifier
        
        Returns:
            Cached model or None
        """
        return self._models.get(model_id)
    
    def get_model_classes(self, model_id: str) -> List[str]:
        """
        Get classes for a model
        
        Args:
            model_id: Model identifier
        
        Returns:
            List of class names
        """
        if model_id in self._model_configs:
            return self._model_configs[model_id].classes
        
        if model_id in self._models:
            model = self._models[model_id]
            return list(model.names.values())
        
        return []
    
    def list_cached_models(self) -> List[str]:
        """
        List all cached model IDs
        
        Returns:
            List of cached model IDs
        """
        return list(self._models.keys())
    
    def clear_cache(self):
        """Clear all cached models"""
        self._models.clear()
        self._model_configs.clear()
        logger.info("Model cache cleared")
