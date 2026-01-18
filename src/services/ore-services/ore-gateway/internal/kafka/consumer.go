package kafka

import (
	"context"
	"encoding/json"
	"fmt"
	"log"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/five0/ore/gateway/internal/websocket"
	kafkago "github.com/segmentio/kafka-go"
)

// Consumer handles consuming detection results from Kafka
type Consumer struct {
	reader *kafkago.Reader
	wsHub  *websocket.Hub
	config config.KafkaConfig
}

// NewConsumer creates a new Kafka consumer
func NewConsumer(cfg config.KafkaConfig, wsHub *websocket.Hub) (*Consumer, error) {
	reader := kafkago.NewReader(kafkago.ReaderConfig{
		Brokers:  cfg.Brokers,
		Topic:    cfg.Topics.DetectionResults,
		GroupID:  cfg.ConsumerGroup,
		MinBytes: 1,
		MaxBytes: 10e6, // 10MB
	})

	return &Consumer{
		reader: reader,
		wsHub:  wsHub,
		config: cfg,
	}, nil
}

// Start begins consuming messages
func (c *Consumer) Start(ctx context.Context) error {
	log.Println("Starting Kafka consumer for detection results...")

	for {
		select {
		case <-ctx.Done():
			log.Println("Consumer context cancelled, stopping...")
			return c.reader.Close()
		default:
			// Read message
			msg, err := c.reader.FetchMessage(ctx)
			if err != nil {
				if err == context.Canceled {
					return nil
				}
				log.Printf("Error fetching message: %v\n", err)
				continue
			}

			// Process message
			if err := c.processMessage(ctx, msg); err != nil {
				log.Printf("Error processing message: %v\n", err)
			}

			// Commit message
			if err := c.reader.CommitMessages(ctx, msg); err != nil {
				log.Printf("Error committing message: %v\n", err)
			}
		}
	}
}

// processMessage processes a detection result message
func (c *Consumer) processMessage(ctx context.Context, msg kafkago.Message) error {
	// Parse JSON message
	var result map[string]interface{}
	if err := json.Unmarshal(msg.Value, &result); err != nil {
		return fmt.Errorf("failed to unmarshal result: %w", err)
	}

	// Extract session ID
	sessionID, ok := result["session_id"].(string)
	if !ok {
		return fmt.Errorf("missing or invalid session_id in result")
	}

	// Forward to WebSocket hub
	if err := c.wsHub.BroadcastToSession(sessionID, msg.Value); err != nil {
		// Log but don't fail - client might have disconnected
		log.Printf("Failed to broadcast to session %s: %v\n", sessionID, err)
	}

	return nil
}

// Close closes the Kafka consumer
func (c *Consumer) Close() error {
	return c.reader.Close()
}
