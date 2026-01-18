"""MinIO client for object storage operations"""

import logging
from io import BytesIO
from typing import BinaryIO, Optional

from minio import Minio
from minio.error import S3Error

from ..config.settings import MinIOConfig

logger = logging.getLogger(__name__)


class MinIOClient:
    """Client for interacting with MinIO object storage"""
    
    def __init__(self, config: MinIOConfig):
        """
        Initialize MinIO client
        
        Args:
            config: MinIO configuration
        """
        self.config = config
        self.client = Minio(
            endpoint=config.endpoint,
            access_key=config.access_key,
            secret_key=config.secret_key,
            secure=config.secure
        )
        logger.info(f"MinIO client initialized: {config.endpoint}")
    
    async def ensure_buckets(self):
        """Ensure required buckets exist"""
        buckets = [self.config.bucket_frames, self.config.bucket_models]
        
        for bucket in buckets:
            try:
                if not self.client.bucket_exists(bucket):
                    self.client.make_bucket(bucket)
                    logger.info(f"Created bucket: {bucket}")
                else:
                    logger.info(f"Bucket exists: {bucket}")
            except S3Error as e:
                logger.error(f"Error ensuring bucket {bucket}: {e}")
                raise
    
    def download_frame(self, storage_url: str) -> bytes:
        """
        Download a frame from MinIO
        
        Args:
            storage_url: MinIO URL in format: minio://bucket/path/to/object
        
        Returns:
            Frame data as bytes
        """
        # Parse URL: minio://bucket/object_name
        if not storage_url.startswith("minio://"):
            raise ValueError(f"Invalid MinIO URL: {storage_url}")
        
        parts = storage_url[8:].split("/", 1)
        if len(parts) != 2:
            raise ValueError(f"Invalid MinIO URL format: {storage_url}")
        
        bucket, object_name = parts
        
        try:
            # Download object
            response = self.client.get_object(bucket, object_name)
            data = response.read()
            response.close()
            response.release_conn()
            
            logger.debug(f"Downloaded frame: {object_name} ({len(data)} bytes)")
            return data
            
        except S3Error as e:
            logger.error(f"Error downloading frame {object_name}: {e}")
            raise
    
    def upload_object(
        self,
        bucket: str,
        object_name: str,
        data: bytes,
        content_type: str = "application/octet-stream"
    ):
        """
        Upload an object to MinIO
        
        Args:
            bucket: Bucket name
            object_name: Object name/path
            data: Data to upload
            content_type: Content type
        """
        try:
            self.client.put_object(
                bucket,
                object_name,
                BytesIO(data),
                length=len(data),
                content_type=content_type
            )
            logger.debug(f"Uploaded object: {object_name}")
        except S3Error as e:
            logger.error(f"Error uploading object {object_name}: {e}")
            raise
    
    def delete_object(self, bucket: str, object_name: str):
        """
        Delete an object from MinIO
        
        Args:
            bucket: Bucket name
            object_name: Object name/path
        """
        try:
            self.client.remove_object(bucket, object_name)
            logger.debug(f"Deleted object: {object_name}")
        except S3Error as e:
            logger.error(f"Error deleting object {object_name}: {e}")
            raise
    
    def download_model(self, model_path: str) -> bytes:
        """
        Download a model file from MinIO
        
        Args:
            model_path: Path to model in models bucket
        
        Returns:
            Model data as bytes
        """
        try:
            response = self.client.get_object(self.config.bucket_models, model_path)
            data = response.read()
            response.close()
            response.release_conn()
            
            logger.info(f"Downloaded model: {model_path} ({len(data)} bytes)")
            return data
            
        except S3Error as e:
            logger.error(f"Error downloading model {model_path}: {e}")
            raise
