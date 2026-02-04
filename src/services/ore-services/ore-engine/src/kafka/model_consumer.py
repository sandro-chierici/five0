"""Kafka consumer for model management commands"""

import asyncio
import json
import logging
from typing import TYPE_CHECKING, Callable, Dict, Any

from aiokafka import AIOKafkaConsumer, AIOKafkaProducer

if TYPE_CHECKING:
    from ..models.dynamic_manager import DynamicModelManager
    from ..config.settings import KafkaConfig

logger = logging.getLogger(__name__)


class ModelCommandConsumer:
    """Consumes model management commands from Kafka"""
    
    # Command types
    CMD_LOAD_MODEL = "load_model"
    CMD_RELOAD_MODEL = "reload_model"
    CMD_UPDATE_CLASSES = "update_classes"
    CMD_UNLOAD_MODEL = "unload_model"
    CMD_LIST_MODELS = "list_models"
    CMD_GET_MODEL_INFO = "get_model_info"
    
    def __init__(
        self,
        config: "KafkaConfig",
        model_manager: "DynamicModelManager",
        command_topic: str = "model-control",
        response_topic: str = "model-control-response"
    ):
        """
        Initialize model command consumer
        
        Args:
            config: Kafka configuration
            model_manager: Dynamic model manager instance
            command_topic: Topic to consume commands from
            response_topic: Topic to publish responses to
        """
        self.config = config
        self.model_manager = model_manager
        self.command_topic = command_topic
        self.response_topic = response_topic
        
        self._consumer = None
        self._producer = None
        self._running = False
        
        # Command handlers
        self._handlers: Dict[str, Callable] = {
            self.CMD_LOAD_MODEL: self._handle_load_model,
            self.CMD_RELOAD_MODEL: self._handle_reload_model,
            self.CMD_UPDATE_CLASSES: self._handle_update_classes,
            self.CMD_UNLOAD_MODEL: self._handle_unload_model,
            self.CMD_LIST_MODELS: self._handle_list_models,
            self.CMD_GET_MODEL_INFO: self._handle_get_model_info,
        }
        
        logger.info(f"Model command consumer initialized for topic: {command_topic}")
    
    async def start(self):
        """Start consuming model commands"""
        brokers = self.config.brokers
        if isinstance(brokers, list) and len(brokers) == 1:
            brokers = brokers[0]
        
        # Initialize consumer
        self._consumer = AIOKafkaConsumer(
            self.command_topic,
            bootstrap_servers=brokers,
            group_id=f"{self.config.consumer_group}-model-control",
            auto_offset_reset=self.config.auto_offset_reset,
            value_deserializer=lambda m: json.loads(m.decode('utf-8'))
        )
        
        # Initialize producer for responses
        self._producer = AIOKafkaProducer(
            bootstrap_servers=brokers,
            value_serializer=lambda v: json.dumps(v).encode('utf-8')
        )
        
        await self._consumer.start()
        await self._producer.start()
        
        self._running = True
        logger.info(f"Model command consumer started, listening on {self.command_topic}")
        
        try:
            async for message in self._consumer:
                if not self._running:
                    break
                    
                try:
                    await self._process_command(message)
                except Exception as e:
                    logger.error(f"Error processing command: {e}", exc_info=True)
        finally:
            await self.stop()
    
    async def stop(self):
        """Stop the consumer"""
        self._running = False
        
        if self._consumer:
            await self._consumer.stop()
            self._consumer = None
        
        if self._producer:
            await self._producer.stop()
            self._producer = None
        
        logger.info("Model command consumer stopped")
    
    async def _process_command(self, message):
        """Process a model command message"""
        try:
            data = message.value
            command = data.get("command")
            correlation_id = data.get("correlation_id")
            payload = data.get("payload", {})
            
            logger.info(f"Received command: {command}, correlation_id: {correlation_id}")
            
            handler = self._handlers.get(command)
            if not handler:
                await self._send_response(correlation_id, {
                    "success": False,
                    "error": f"Unknown command: {command}"
                })
                return
            
            # Execute handler
            result = await handler(payload)
            
            # Send response
            await self._send_response(correlation_id, result)
            
        except Exception as e:
            logger.error(f"Error processing command: {e}", exc_info=True)
            if correlation_id:
                await self._send_response(correlation_id, {
                    "success": False,
                    "error": str(e)
                })
    
    async def _send_response(self, correlation_id: str, result: Dict[str, Any]):
        """Send response back via Kafka"""
        if not correlation_id or not self._producer:
            return
        
        response = {
            "correlation_id": correlation_id,
            "result": result
        }
        
        try:
            await self._producer.send_and_wait(self.response_topic, response)
            logger.debug(f"Sent response for correlation_id: {correlation_id}")
        except Exception as e:
            logger.error(f"Failed to send response: {e}")
    
    async def _handle_load_model(self, payload: Dict) -> Dict:
        """Handle load model command"""
        model_id = payload.get("model_id")
        force_reload = payload.get("force_reload", False)
        
        if not model_id:
            return {"success": False, "error": "model_id is required"}
        
        model = await self.model_manager.load_model(model_id, force_reload=force_reload)
        if model:
            classes = self.model_manager.get_model_classes(model_id)
            return {
                "success": True,
                "model_id": model_id,
                "message": f"Model {model_id} loaded successfully",
                "classes_count": len(classes),
                "classes": classes
            }
        else:
            return {
                "success": False,
                "error": f"Failed to load model {model_id}"
            }
    
    async def _handle_reload_model(self, payload: Dict) -> Dict:
        """Handle reload model command"""
        model_id = payload.get("model_id")
        
        if not model_id:
            return {"success": False, "error": "model_id is required"}
        
        success = await self.model_manager.reload_model(model_id)
        if success:
            classes = self.model_manager.get_model_classes(model_id)
            return {
                "success": True,
                "model_id": model_id,
                "message": f"Model {model_id} reloaded successfully",
                "classes_count": len(classes),
                "classes": classes
            }
        else:
            return {
                "success": False,
                "error": f"Failed to reload model {model_id}"
            }
    
    async def _handle_update_classes(self, payload: Dict) -> Dict:
        """Handle update classes command"""
        model_id = payload.get("model_id")
        classes = payload.get("classes", [])
        
        if not model_id:
            return {"success": False, "error": "model_id is required"}
        if not classes:
            return {"success": False, "error": "classes array is required"}
        
        success = await self.model_manager.update_model_classes(model_id, classes)
        if success:
            return {
                "success": True,
                "model_id": model_id,
                "message": f"Classes updated for model {model_id}",
                "classes_count": len(classes),
                "classes": classes
            }
        else:
            return {
                "success": False,
                "error": f"Failed to update classes for model {model_id}"
            }
    
    async def _handle_unload_model(self, payload: Dict) -> Dict:
        """Handle unload model command"""
        model_id = payload.get("model_id")
        
        if not model_id:
            return {"success": False, "error": "model_id is required"}
        
        if model_id in self.model_manager._models:
            del self.model_manager._models[model_id]
            if model_id in self.model_manager._model_configs:
                del self.model_manager._model_configs[model_id]
            
            return {
                "success": True,
                "model_id": model_id,
                "message": f"Model {model_id} unloaded from cache"
            }
        else:
            return {
                "success": False,
                "error": f"Model {model_id} not in cache"
            }
    
    async def _handle_list_models(self, payload: Dict) -> Dict:
        """Handle list models command"""
        include_storage = payload.get("include_storage", True)
        
        result = {
            "success": True,
            "cached_models": self.model_manager.list_cached_models()
        }
        
        if include_storage:
            available = await self.model_manager.refresh_model_list()
            result["available_models"] = available
        
        return result
    
    async def _handle_get_model_info(self, payload: Dict) -> Dict:
        """Handle get model info command"""
        model_id = payload.get("model_id")
        
        if not model_id:
            return {"success": False, "error": "model_id is required"}
        
        config = await self.model_manager.get_model_config(model_id)
        if not config:
            return {
                "success": False,
                "error": f"Model {model_id} not found"
            }
        
        cached = model_id in self.model_manager.list_cached_models()
        
        return {
            "success": True,
            "model_id": config.model_id,
            "name": config.name,
            "description": config.description,
            "framework": config.framework,
            "classes": config.classes,
            "classes_count": len(config.classes),
            "weights_path": config.weights_path,
            "created_at": config.created_at,
            "updated_at": config.updated_at,
            "cached": cached
        }
