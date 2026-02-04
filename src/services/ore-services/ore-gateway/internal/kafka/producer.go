package kafka

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	kafkago "github.com/segmentio/kafka-go"
)

// Producer handles publishing messages to Kafka
type Producer struct {
	frameWriter   *kafkago.Writer
	sessionWriter *kafkago.Writer
	modelWriter   *kafkago.Writer
	config        config.KafkaConfig
}

// NewProducer creates a new Kafka producer
func NewProducer(cfg config.KafkaConfig) (*Producer, error) {
	// Frame input writer
	frameWriter := &kafkago.Writer{
		Addr:         kafkago.TCP(cfg.Brokers...),
		Topic:        cfg.Topics.FrameInput,
		Balancer:     &kafkago.Hash{}, // Partition by key (session_id)
		BatchSize:    cfg.BatchSize,
		BatchTimeout: cfg.BatchTimeout,
		RequiredAcks: kafkago.RequireOne,
		Compression:  kafkago.Lz4,
	}

	// Session control writer
	sessionWriter := &kafkago.Writer{
		Addr:         kafkago.TCP(cfg.Brokers...),
		Topic:        cfg.Topics.SessionControl,
		Balancer:     &kafkago.Hash{},
		BatchSize:    10,
		BatchTimeout: 100 * time.Millisecond,
		RequiredAcks: kafkago.RequireOne,
	}

	// Model control writer
	modelControlTopic := cfg.Topics.ModelControl
	if modelControlTopic == "" {
		modelControlTopic = "model-control"
	}
	modelWriter := &kafkago.Writer{
		Addr:         kafkago.TCP(cfg.Brokers...),
		Topic:        modelControlTopic,
		Balancer:     &kafkago.RoundRobin{},
		BatchSize:    1,
		BatchTimeout: 50 * time.Millisecond,
		RequiredAcks: kafkago.RequireOne,
	}

	return &Producer{
		frameWriter:   frameWriter,
		sessionWriter: sessionWriter,
		modelWriter:   modelWriter,
		config:        cfg,
	}, nil
}

// PublishFrameReference publishes a frame reference to Kafka
func (p *Producer) PublishFrameReference(ctx context.Context, frameRef map[string]interface{}) error {
	// Serialize to JSON
	data, err := json.Marshal(frameRef)
	if err != nil {
		return fmt.Errorf("failed to marshal frame reference: %w", err)
	}

	// Get session ID for partitioning
	sessionID, ok := frameRef["session_id"].(string)
	if !ok {
		return fmt.Errorf("missing or invalid session_id")
	}

	// Create Kafka message
	msg := kafkago.Message{
		Key:   []byte(sessionID),
		Value: data,
		Time:  time.Now(),
	}

	// Write to Kafka
	if err := p.frameWriter.WriteMessages(ctx, msg); err != nil {
		return fmt.Errorf("failed to write frame reference: %w", err)
	}

	return nil
}

// PublishSessionEvent publishes a session lifecycle event
func (p *Producer) PublishSessionEvent(ctx context.Context, event map[string]interface{}) error {
	// Serialize to JSON
	data, err := json.Marshal(event)
	if err != nil {
		return fmt.Errorf("failed to marshal session event: %w", err)
	}

	// Get session ID for partitioning
	sessionID, ok := event["session_id"].(string)
	if !ok {
		return fmt.Errorf("missing or invalid session_id")
	}

	// Create Kafka message
	msg := kafkago.Message{
		Key:   []byte(sessionID),
		Value: data,
		Time:  time.Now(),
	}

	// Write to Kafka
	if err := p.sessionWriter.WriteMessages(ctx, msg); err != nil {
		return fmt.Errorf("failed to write session event: %w", err)
	}

	return nil
}

// PublishModelCommand publishes a model management command
func (p *Producer) PublishModelCommand(ctx context.Context, command map[string]interface{}) error {
	// Serialize to JSON
	data, err := json.Marshal(command)
	if err != nil {
		return fmt.Errorf("failed to marshal model command: %w", err)
	}

	// Get correlation ID for key
	correlationID, _ := command["correlation_id"].(string)

	// Create Kafka message
	msg := kafkago.Message{
		Key:   []byte(correlationID),
		Value: data,
		Time:  time.Now(),
	}

	// Write to Kafka
	if err := p.modelWriter.WriteMessages(ctx, msg); err != nil {
		return fmt.Errorf("failed to write model command: %w", err)
	}

	return nil
}

// Close closes the Kafka writers
func (p *Producer) Close() error {
	if err := p.frameWriter.Close(); err != nil {
		return err
	}
	if err := p.sessionWriter.Close(); err != nil {
		return err
	}
	if err := p.modelWriter.Close(); err != nil {
		return err
	}
	return nil
}
