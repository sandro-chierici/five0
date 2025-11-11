// Package handlers provides health check handler
package handlers

import (
	"net/http"

	"github.com/gin-gonic/gin"

	"customservice/internal/database"
	"customservice/internal/models"
)

// HealthHandler handles health check requests
type HealthHandler struct {
	dbClient *database.Client
}

// NewHealthHandler creates a new health handler
func NewHealthHandler(dbClient *database.Client) *HealthHandler {
	return &HealthHandler{
		dbClient: dbClient,
	}
}

// HealthCheck performs a health check on the service and its dependencies
func (h *HealthHandler) HealthCheck(c *gin.Context) {
	health := models.HealthResponse{
		Status:  "ok",
		Message: "CustomService is running",
	}

	// Check database health
	if err := h.dbClient.HealthCheck(); err != nil {
		health.Status = "error"
		health.Database = "disconnected"
		health.Message = "Database connection failed"
		c.JSON(http.StatusServiceUnavailable, health)
		return
	}

	health.Database = "connected"
	c.JSON(http.StatusOK, health)
}