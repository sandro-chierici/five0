// Package database provides MongoDB connection and operations
package database

import (
	"context"
	"fmt"
	"log"
	"time"

	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"

	"customservice/internal/config"
)

// Client represents the database client
type Client struct {
	client     *mongo.Client
	collection *mongo.Collection
	config     *config.Config
}

// New creates a new database client
func New(cfg *config.Config) (*Client, error) {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	clientOptions := options.Client().ApplyURI(cfg.Database.MongoDBURI)
	
	client, err := mongo.Connect(ctx, clientOptions)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to MongoDB: %v", err)
	}

	// Test the connection
	if err := client.Ping(ctx, nil); err != nil {
		return nil, fmt.Errorf("failed to ping MongoDB: %v", err)
	}

	log.Printf("Successfully connected to MongoDB at %s", cfg.Database.MongoDBURI)

	collection := client.Database(cfg.Database.DatabaseName).Collection(cfg.Database.CollectionName)

	return &Client{
		client:     client,
		collection: collection,
		config:     cfg,
	}, nil
}

// Close closes the database connection
func (c *Client) Close() error {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	if err := c.client.Disconnect(ctx); err != nil {
		return fmt.Errorf("failed to disconnect from MongoDB: %v", err)
	}

	log.Println("Disconnected from MongoDB")
	return nil
}

// HealthCheck performs a health check on the database
func (c *Client) HealthCheck() error {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	return c.client.Ping(ctx, nil)
}

// GetCollection returns the MongoDB collection
func (c *Client) GetCollection() *mongo.Collection {
	return c.collection
}