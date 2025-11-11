// Package handlers provides HTTP handlers for the CustomService
package handlers

import (
	"net/http"

	"github.com/gin-gonic/gin"
	"go.mongodb.org/mongo-driver/bson/primitive"

	"customservice/internal/database"
	"customservice/internal/models"
	"customservice/pkg/errors"
)

// ResourceHandler handles resource-related HTTP requests
type ResourceHandler struct {
	repository database.ResourceRepository
}

// NewResourceHandler creates a new resource handler
func NewResourceHandler(repository database.ResourceRepository) *ResourceHandler {
	return &ResourceHandler{
		repository: repository,
	}
}

// CreateResource creates a new resource
func (h *ResourceHandler) CreateResource(c *gin.Context) {
	var req models.CreateResourceRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		appErr := errors.NewBadRequestError("Invalid request body", err)
		errors.HandleError(c, appErr)
		return
	}

	resource := &models.Resource{
		Specs: req.Specs,
	}

	createdResource, err := h.repository.Create(c.Request.Context(), resource)
	if err != nil {
		errors.HandleError(c, err)
		return
	}

	c.JSON(http.StatusCreated, createdResource)
}

// GetResource retrieves a resource by ID
func (h *ResourceHandler) GetResource(c *gin.Context) {
	id := c.Param("id")
	objectID, err := primitive.ObjectIDFromHex(id)
	if err != nil {
		appErr := errors.NewBadRequestError("Invalid resource ID", err)
		errors.HandleError(c, appErr)
		return
	}

	resource, err := h.repository.GetByID(c.Request.Context(), objectID)
	if err != nil {
		errors.HandleError(c, err)
		return
	}

	c.JSON(http.StatusOK, resource)
}

// ListResources retrieves all resources
func (h *ResourceHandler) ListResources(c *gin.Context) {
	resources, err := h.repository.List(c.Request.Context())
	if err != nil {
		errors.HandleError(c, err)
		return
	}

	response := models.ResourceListResponse{
		Resources: resources,
		Count:     len(resources),
	}

	c.JSON(http.StatusOK, response)
}

// UpdateResource updates an existing resource
func (h *ResourceHandler) UpdateResource(c *gin.Context) {
	id := c.Param("id")
	objectID, err := primitive.ObjectIDFromHex(id)
	if err != nil {
		appErr := errors.NewBadRequestError("Invalid resource ID", err)
		errors.HandleError(c, appErr)
		return
	}

	var req models.UpdateResourceRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		appErr := errors.NewBadRequestError("Invalid request body", err)
		errors.HandleError(c, appErr)
		return
	}

	resource := &models.Resource{
		Specs: req.Specs,
	}

	updatedResource, err := h.repository.Update(c.Request.Context(), objectID, resource)
	if err != nil {
		errors.HandleError(c, err)
		return
	}

	c.JSON(http.StatusOK, updatedResource)
}

// DeleteResource deletes a resource by ID
func (h *ResourceHandler) DeleteResource(c *gin.Context) {
	id := c.Param("id")
	objectID, err := primitive.ObjectIDFromHex(id)
	if err != nil {
		appErr := errors.NewBadRequestError("Invalid resource ID", err)
		errors.HandleError(c, appErr)
		return
	}

	err = h.repository.Delete(c.Request.Context(), objectID)
	if err != nil {
		errors.HandleError(c, err)
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Resource deleted successfully"})
}