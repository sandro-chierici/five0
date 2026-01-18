package session

import (
	"context"
	"fmt"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/five0/ore/gateway/internal/kafka"
	"github.com/five0/ore/gateway/internal/storage"
)

// Manager handles session lifecycle operations
type Manager struct {
	store         *RedisStore
	minioClient   *storage.MinIOClient
	kafkaProducer *kafka.Producer
	config        *config.Config
}

// NewManager creates a new session manager
func NewManager(
	store *RedisStore,
	minioClient *storage.MinIOClient,
	kafkaProducer *kafka.Producer,
	cfg *config.Config,
) *Manager {
	return &Manager{
		store:         store,
		minioClient:   minioClient,
		kafkaProducer: kafkaProducer,
		config:        cfg,
	}
}

// CreateSession creates a new recognition session
func (m *Manager) CreateSession(ctx context.Context, clientType, modelID string) (*Session, error) {
	// Create new session
	session := NewSession(clientType, modelID)

	// Store in Redis
	if err := m.store.CreateSession(ctx, session); err != nil {
		return nil, fmt.Errorf("failed to create session: %w", err)
	}

	// Publish session created event
	if err := m.publishSessionEvent(ctx, session, "created"); err != nil {
		// Log error but don't fail
		fmt.Printf("Warning: failed to publish session created event: %v\n", err)
	}

	return session, nil
}

// GetSession retrieves a session by ID
func (m *Manager) GetSession(ctx context.Context, sessionID string) (*Session, error) {
	return m.store.GetSession(ctx, sessionID)
}

// UpdateSession updates session configuration
func (m *Manager) UpdateSession(ctx context.Context, session *Session) error {
	session.LastActivity = time.Now().Unix()

	if err := m.store.UpdateSession(ctx, session); err != nil {
		return fmt.Errorf("failed to update session: %w", err)
	}

	// Publish session updated event
	if err := m.publishSessionEvent(ctx, session, "updated"); err != nil {
		fmt.Printf("Warning: failed to publish session updated event: %v\n", err)
	}

	return nil
}

// CloseSession closes an active session
func (m *Manager) CloseSession(ctx context.Context, sessionID string) error {
	// Get session
	session, err := m.store.GetSession(ctx, sessionID)
	if err != nil {
		return err
	}

	// Update status
	session.Status = "closed"

	// Delete from Redis
	if err := m.store.DeleteSession(ctx, sessionID); err != nil {
		return fmt.Errorf("failed to close session: %w", err)
	}

	// Publish session closed event
	if err := m.publishSessionEvent(ctx, session, "closed"); err != nil {
		fmt.Printf("Warning: failed to publish session closed event: %v\n", err)
	}

	// TODO: Cleanup MinIO files for this session (async)
	go m.cleanupSessionFiles(context.Background(), sessionID)

	return nil
}

// UpdateLastActivity updates session activity timestamp
func (m *Manager) UpdateLastActivity(ctx context.Context, sessionID string) error {
	return m.store.UpdateLastActivity(ctx, sessionID)
}

// IncrementFramesProcessed increments the frame counter
func (m *Manager) IncrementFramesProcessed(ctx context.Context, sessionID string) error {
	return m.store.IncrementFramesProcessed(ctx, sessionID)
}

// ListActiveSessions lists all active sessions
func (m *Manager) ListActiveSessions(ctx context.Context) ([]*Session, error) {
	return m.store.ListActiveSessions(ctx, 100)
}

// GenerateUploadURL generates a presigned URL for frame upload
func (m *Manager) GenerateUploadURL(ctx context.Context, sessionID string, frameID int64) (string, error) {
	objectName := fmt.Sprintf("%s/frame-%06d.jpg", sessionID, frameID)

	url, err := m.minioClient.GeneratePresignedPutURL(
		ctx,
		m.config.MinIO.BucketFrames,
		objectName,
		time.Duration(m.config.MinIO.PresignedURLExpiry)*time.Second,
	)
	if err != nil {
		return "", fmt.Errorf("failed to generate upload URL: %w", err)
	}

	return url, nil
}

// publishSessionEvent publishes session lifecycle events to Kafka
func (m *Manager) publishSessionEvent(ctx context.Context, session *Session, eventType string) error {
	// Create event message
	event := map[string]interface{}{
		"session_id":   session.ID,
		"event_type":   eventType,
		"timestamp_ms": time.Now().UnixMilli(),
		"details": map[string]interface{}{
			"client_type": session.ClientType,
			"model_id":    session.ModelID,
			"status":      session.Status,
		},
	}

	// Publish to Kafka
	return m.kafkaProducer.PublishSessionEvent(ctx, event)
}

// PublishFrameReference publishes a frame reference to Kafka for processing
func (m *Manager) PublishFrameReference(ctx context.Context, frameRef map[string]interface{}) error {
	return m.kafkaProducer.PublishFrameReference(ctx, frameRef)
}

// cleanupSessionFiles removes MinIO files for a closed session
func (m *Manager) cleanupSessionFiles(ctx context.Context, sessionID string) {
	prefix := fmt.Sprintf("%s/", sessionID)

	if err := m.minioClient.RemoveObjectsWithPrefix(
		ctx,
		m.config.MinIO.BucketFrames,
		prefix,
	); err != nil {
		fmt.Printf("Error cleaning up files for session %s: %v\n", sessionID, err)
	}
}
