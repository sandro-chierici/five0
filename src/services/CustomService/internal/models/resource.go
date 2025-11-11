// Package models defines the data models for the CustomService
package models

import (
	"go.mongodb.org/mongo-driver/bson/primitive"
)

// Resource represents a resource document in MongoDB
type Resource struct {
	ID    primitive.ObjectID     `json:"_id,omitempty" bson:"_id,omitempty"`
	Specs map[string]interface{} `json:"specs" bson:"specs"`
}

// CreateResourceRequest represents the request body for creating a resource
type CreateResourceRequest struct {
	Specs map[string]interface{} `json:"specs" binding:"required"`
}

// UpdateResourceRequest represents the request body for updating a resource
type UpdateResourceRequest struct {
	Specs map[string]interface{} `json:"specs" binding:"required"`
}

// ResourceListResponse represents the response for listing resources
type ResourceListResponse struct {
	Resources []Resource `json:"resources"`
	Count     int        `json:"count"`
}

// HealthResponse represents the health check response
type HealthResponse struct {
	Status   string `json:"status"`
	Database string `json:"database"`
	Message  string `json:"message"`
}