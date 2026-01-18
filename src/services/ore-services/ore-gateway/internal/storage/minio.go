package storage

import (
	"context"
	"fmt"
	"io"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/minio/minio-go/v7"
	"github.com/minio/minio-go/v7/pkg/credentials"
)

// MinIOClient wraps MinIO operations
type MinIOClient struct {
	client *minio.Client
	config config.MinIOConfig
}

// NewMinIOClient creates a new MinIO client
func NewMinIOClient(cfg config.MinIOConfig) (*MinIOClient, error) {
	client, err := minio.New(cfg.Endpoint, &minio.Options{
		Creds:  credentials.NewStaticV4(cfg.AccessKey, cfg.SecretKey, ""),
		Secure: cfg.UseSSL,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to create MinIO client: %w", err)
	}

	minioClient := &MinIOClient{
		client: client,
		config: cfg,
	}

	// Ensure buckets exist
	ctx := context.Background()
	if err := minioClient.EnsureBucket(ctx, cfg.BucketFrames); err != nil {
		return nil, err
	}
	if err := minioClient.EnsureBucket(ctx, cfg.BucketModels); err != nil {
		return nil, err
	}

	return minioClient, nil
}

// EnsureBucket creates a bucket if it doesn't exist
func (c *MinIOClient) EnsureBucket(ctx context.Context, bucketName string) error {
	exists, err := c.client.BucketExists(ctx, bucketName)
	if err != nil {
		return fmt.Errorf("failed to check bucket existence: %w", err)
	}

	if !exists {
		if err := c.client.MakeBucket(ctx, bucketName, minio.MakeBucketOptions{}); err != nil {
			return fmt.Errorf("failed to create bucket: %w", err)
		}
		fmt.Printf("Created bucket: %s\n", bucketName)
	}

	return nil
}

// GeneratePresignedPutURL generates a presigned URL for uploading
func (c *MinIOClient) GeneratePresignedPutURL(ctx context.Context, bucket, objectName string, expiry time.Duration) (string, error) {
	url, err := c.client.PresignedPutObject(ctx, bucket, objectName, expiry)
	if err != nil {
		return "", fmt.Errorf("failed to generate presigned PUT URL: %w", err)
	}

	return url.String(), nil
}

// GeneratePresignedGetURL generates a presigned URL for downloading
func (c *MinIOClient) GeneratePresignedGetURL(ctx context.Context, bucket, objectName string, expiry time.Duration) (string, error) {
	url, err := c.client.PresignedGetObject(ctx, bucket, objectName, expiry, nil)
	if err != nil {
		return "", fmt.Errorf("failed to generate presigned GET URL: %w", err)
	}

	return url.String(), nil
}

// PutObject uploads an object to MinIO
func (c *MinIOClient) PutObject(ctx context.Context, bucket, objectName string, reader io.Reader, size int64, contentType string) error {
	_, err := c.client.PutObject(ctx, bucket, objectName, reader, size, minio.PutObjectOptions{
		ContentType: contentType,
	})
	if err != nil {
		return fmt.Errorf("failed to put object: %w", err)
	}

	return nil
}

// GetObject downloads an object from MinIO
func (c *MinIOClient) GetObject(ctx context.Context, bucket, objectName string) (*minio.Object, error) {
	obj, err := c.client.GetObject(ctx, bucket, objectName, minio.GetObjectOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to get object: %w", err)
	}

	return obj, nil
}

// RemoveObject removes an object from MinIO
func (c *MinIOClient) RemoveObject(ctx context.Context, bucket, objectName string) error {
	if err := c.client.RemoveObject(ctx, bucket, objectName, minio.RemoveObjectOptions{}); err != nil {
		return fmt.Errorf("failed to remove object: %w", err)
	}

	return nil
}

// RemoveObjectsWithPrefix removes all objects with a given prefix
func (c *MinIOClient) RemoveObjectsWithPrefix(ctx context.Context, bucket, prefix string) error {
	objectsCh := c.client.ListObjects(ctx, bucket, minio.ListObjectsOptions{
		Prefix:    prefix,
		Recursive: true,
	})

	for object := range objectsCh {
		if object.Err != nil {
			return fmt.Errorf("error listing objects: %w", object.Err)
		}

		if err := c.RemoveObject(ctx, bucket, object.Key); err != nil {
			// Log error but continue
			fmt.Printf("Warning: failed to remove object %s: %v\n", object.Key, err)
		}
	}

	return nil
}

// StatObject gets object metadata
func (c *MinIOClient) StatObject(ctx context.Context, bucket, objectName string) (*minio.ObjectInfo, error) {
	info, err := c.client.StatObject(ctx, bucket, objectName, minio.StatObjectOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to stat object: %w", err)
	}

	return &info, nil
}
