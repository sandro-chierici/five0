package session

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/google/uuid"
	"github.com/redis/go-redis/v9"
)

// Session represents an active recognition session
type Session struct {
	ID              string                 `json:"id"`
	ClientType      string                 `json:"client_type"`
	ModelID         string                 `json:"model_id"`
	ConfThreshold   float32                `json:"confidence_threshold"`
	TargetClasses   []string               `json:"target_classes"`
	EnableTracking  bool                   `json:"enable_tracking"`
	MaxDetections   int32                  `json:"max_detections"`
	FramesProcessed int64                  `json:"frames_processed"`
	CreatedAt       int64                  `json:"created_at"`
	LastActivity    int64                  `json:"last_activity"`
	Status          string                 `json:"status"` // active, inactive, expired
	Metadata        map[string]interface{} `json:"metadata"`
}

// RedisStore manages session storage in Redis
type RedisStore struct {
	client *redis.Client
	ttl    time.Duration
}

// NewRedisStore creates a new Redis store instance
func NewRedisStore(ctx context.Context, cfg config.RedisConfig) (*RedisStore, error) {
	client := redis.NewClient(&redis.Options{
		Addr:     cfg.URL,
		Password: cfg.Password,
		DB:       cfg.DB,
		PoolSize: cfg.PoolSize,
	})

	// Test connection
	if err := client.Ping(ctx).Err(); err != nil {
		return nil, fmt.Errorf("failed to connect to Redis: %w", err)
	}

	return &RedisStore{
		client: client,
		ttl:    time.Duration(cfg.SessionTTL) * time.Second,
	}, nil
}

// CreateSession stores a new session in Redis
func (s *RedisStore) CreateSession(ctx context.Context, session *Session) error {
	key := fmt.Sprintf("sessions:%s", session.ID)

	// Serialize session to JSON
	data, err := json.Marshal(session)
	if err != nil {
		return fmt.Errorf("failed to marshal session: %w", err)
	}

	// Store in Redis with TTL
	if err := s.client.Set(ctx, key, data, s.ttl).Err(); err != nil {
		return fmt.Errorf("failed to store session: %w", err)
	}

	// Add to active sessions sorted set (score = last_activity)
	if err := s.client.ZAdd(ctx, "active_sessions", redis.Z{
		Score:  float64(session.LastActivity),
		Member: session.ID,
	}).Err(); err != nil {
		return fmt.Errorf("failed to add to active sessions: %w", err)
	}

	return nil
}

// GetSession retrieves a session from Redis
func (s *RedisStore) GetSession(ctx context.Context, sessionID string) (*Session, error) {
	key := fmt.Sprintf("sessions:%s", sessionID)

	data, err := s.client.Get(ctx, key).Bytes()
	if err != nil {
		if err == redis.Nil {
			return nil, fmt.Errorf("session not found: %s", sessionID)
		}
		return nil, fmt.Errorf("failed to get session: %w", err)
	}

	var session Session
	if err := json.Unmarshal(data, &session); err != nil {
		return nil, fmt.Errorf("failed to unmarshal session: %w", err)
	}

	return &session, nil
}

// UpdateSession updates an existing session
func (s *RedisStore) UpdateSession(ctx context.Context, session *Session) error {
	key := fmt.Sprintf("sessions:%s", session.ID)

	// Serialize session to JSON
	data, err := json.Marshal(session)
	if err != nil {
		return fmt.Errorf("failed to marshal session: %w", err)
	}

	// Update in Redis (keep existing TTL)
	if err := s.client.Set(ctx, key, data, s.ttl).Err(); err != nil {
		return fmt.Errorf("failed to update session: %w", err)
	}

	// Update in active sessions sorted set
	if err := s.client.ZAdd(ctx, "active_sessions", redis.Z{
		Score:  float64(session.LastActivity),
		Member: session.ID,
	}).Err(); err != nil {
		return fmt.Errorf("failed to update active sessions: %w", err)
	}

	return nil
}

// DeleteSession removes a session from Redis
func (s *RedisStore) DeleteSession(ctx context.Context, sessionID string) error {
	key := fmt.Sprintf("sessions:%s", sessionID)

	// Delete from Redis
	if err := s.client.Del(ctx, key).Err(); err != nil {
		return fmt.Errorf("failed to delete session: %w", err)
	}

	// Remove from active sessions
	if err := s.client.ZRem(ctx, "active_sessions", sessionID).Err(); err != nil {
		return fmt.Errorf("failed to remove from active sessions: %w", err)
	}

	return nil
}

// UpdateLastActivity updates the last activity timestamp
func (s *RedisStore) UpdateLastActivity(ctx context.Context, sessionID string) error {
	session, err := s.GetSession(ctx, sessionID)
	if err != nil {
		return err
	}

	session.LastActivity = time.Now().Unix()
	return s.UpdateSession(ctx, session)
}

// IncrementFramesProcessed increments the frames processed counter
func (s *RedisStore) IncrementFramesProcessed(ctx context.Context, sessionID string) error {
	session, err := s.GetSession(ctx, sessionID)
	if err != nil {
		return err
	}

	session.FramesProcessed++
	session.LastActivity = time.Now().Unix()
	return s.UpdateSession(ctx, session)
}

// ListActiveSessions retrieves all active sessions
func (s *RedisStore) ListActiveSessions(ctx context.Context, limit int64) ([]*Session, error) {
	// Get session IDs from sorted set (most recent first)
	sessionIDs, err := s.client.ZRevRange(ctx, "active_sessions", 0, limit-1).Result()
	if err != nil {
		return nil, fmt.Errorf("failed to list active sessions: %w", err)
	}

	sessions := make([]*Session, 0, len(sessionIDs))
	for _, id := range sessionIDs {
		session, err := s.GetSession(ctx, id)
		if err != nil {
			// Skip sessions that no longer exist
			continue
		}
		sessions = append(sessions, session)
	}

	return sessions, nil
}

// Close closes the Redis connection
func (s *RedisStore) Close() error {
	return s.client.Close()
}

// NewSession creates a new session instance
func NewSession(clientType, modelID string) *Session {
	now := time.Now().Unix()
	return &Session{
		ID:              uuid.New().String(),
		ClientType:      clientType,
		ModelID:         modelID,
		ConfThreshold:   0.5,
		TargetClasses:   []string{},
		EnableTracking:  false,
		MaxDetections:   100,
		FramesProcessed: 0,
		CreatedAt:       now,
		LastActivity:    now,
		Status:          "active",
		Metadata:        make(map[string]interface{}),
	}
}
