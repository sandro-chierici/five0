"""Model loader and management"""

import logging
import os
from pathlib import Path
from typing import Dict, Optional

import torch
from ultralytics import YOLO

from ..config.settings import InferenceConfig, ModelConfig

logger = logging.getLogger(__name__)


class ModelLoader:
    """Manages loading and caching of ML models"""
    
    def __init__(self, inference_config: InferenceConfig, models_config: Dict[str, ModelConfig]):
        """
        Initialize model loader
        
        Args:
            inference_config: Inference configuration
            models_config: Dictionary of model configurations
        """
        self.inference_config = inference_config
        self.models_config = models_config
        self.cache: Dict[str, YOLO] = {}
        self.device = inference_config.device
        
        logger.info(f"Model loader initialized with device: {self.device}")
        
        # Verify device availability
        if self.device == "cuda" and not torch.cuda.is_available():
            logger.warning("CUDA not available, falling back to CPU")
            self.device = "cpu"
    
    def load_model(self, model_id: str) -> YOLO:
        """
        Load a model by ID
        
        Args:
            model_id: Model identifier
        
        Returns:
            Loaded YOLO model
        """
        # Check cache
        if model_id in self.cache:
            logger.debug(f"Model {model_id} loaded from cache")
            return self.cache[model_id]
        
        # Get model config
        model_config = self.models_config.get("default")  # For now, use default
        if not model_config:
            raise ValueError(f"Model configuration not found: {model_id}")
        
        logger.info(f"Loading model: {model_id} from {model_config.path}")
        
        try:
            # Load YOLO model
            model = YOLO(model_config.path)
            
            # Move to device
            model.to(self.device)
            
            # Cache model
            if len(self.cache) >= self.inference_config.model_cache_size:
                # Remove oldest model
                oldest_key = next(iter(self.cache))
                del self.cache[oldest_key]
                logger.info(f"Evicted model from cache: {oldest_key}")
            
            self.cache[model_id] = model
            logger.info(f"Model {model_id} loaded successfully")
            
            return model
            
        except Exception as e:
            logger.error(f"Failed to load model {model_id}: {e}")
            raise
    
    def get_cached_model(self, model_id: str) -> Optional[YOLO]:
        """
        Get model from cache without loading
        
        Args:
            model_id: Model identifier
        
        Returns:
            Cached model or None
        """
        return self.cache.get(model_id)
    
    def switch_model(self, model_id: str) -> YOLO:
        """
        Switch to a different model
        
        Args:
            model_id: New model identifier
        
        Returns:
            Loaded model
        """
        logger.info(f"Switching to model: {model_id}")
        return self.load_model(model_id)
    
    def get_model_info(self, model_id: str) -> Dict:
        """
        Get information about a model
        
        Args:
            model_id: Model identifier
        
        Returns:
            Model information dictionary
        """
        model_config = self.models_config.get("default")
        if not model_config:
            return {}
        
        return {
            "model_id": model_id,
            "framework": model_config.framework,
            "classes": model_config.classes,
            "device": self.device,
            "cached": model_id in self.cache
        }
    
    def clear_cache(self):
        """Clear all cached models"""
        self.cache.clear()
        logger.info("Model cache cleared")
