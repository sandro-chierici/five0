package handlers

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strings"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/five0/ore/gateway/internal/kafka"
	"github.com/five0/ore/gateway/internal/storage"
	"github.com/five0/ore/gateway/pkg/logger"
	"github.com/google/uuid"
)

// ModelHandler handles model management REST endpoints
type ModelHandler struct {
	minioClient   *storage.MinIOClient
	kafkaProducer *kafka.Producer
	config        *config.Config
	logger        *logger.Logger
}

// NewModelHandler creates a new model handler
func NewModelHandler(minioClient *storage.MinIOClient, kafkaProducer *kafka.Producer, cfg *config.Config, log *logger.Logger) *ModelHandler {
	return &ModelHandler{
		minioClient:   minioClient,
		kafkaProducer: kafkaProducer,
		config:        cfg,
		logger:        log,
	}
}

// ModelUploadRequest represents a request to upload a new model
type ModelUploadRequest struct {
	ModelID     string   `json:"model_id"`
	Name        string   `json:"name"`
	Description string   `json:"description,omitempty"`
	Framework   string   `json:"framework"` // yolov8, yolov5, etc.
	Classes     []string `json:"classes"`
}

// ModelUploadResponse represents the response after uploading a model
type ModelUploadResponse struct {
	ModelID       string `json:"model_id"`
	UploadURL     string `json:"upload_url"`
	WeightsPath   string `json:"weights_path"`
	ExpiresIn     int    `json:"expires_in_seconds"`
	ClassesPath   string `json:"classes_path,omitempty"`
	ClassesStored bool   `json:"classes_stored"`
}

// ModelInfo represents model metadata
type ModelInfo struct {
	ModelID     string   `json:"model_id"`
	Name        string   `json:"name"`
	Description string   `json:"description,omitempty"`
	Framework   string   `json:"framework"`
	Classes     []string `json:"classes"`
	WeightsPath string   `json:"weights_path"`
	ClassesPath string   `json:"classes_path"`
	CreatedAt   string   `json:"created_at"`
	UpdatedAt   string   `json:"updated_at"`
	Size        int64    `json:"size_bytes,omitempty"`
}

// ListModelsResponse represents the response when listing models
type ListModelsResponse struct {
	Models []ModelInfo `json:"models"`
	Count  int         `json:"count"`
}

// CreateModel initiates model upload by returning a presigned URL and storing metadata
// POST /api/v1/models
func (h *ModelHandler) CreateModel(w http.ResponseWriter, r *http.Request) {
	var req ModelUploadRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.writeError(w, http.StatusBadRequest, fmt.Sprintf("Invalid request body: %v", err))
		return
	}

	// Validate required fields
	if req.ModelID == "" {
		h.writeError(w, http.StatusBadRequest, "model_id is required")
		return
	}
	if req.Framework == "" {
		h.writeError(w, http.StatusBadRequest, "framework is required")
		return
	}
	if len(req.Classes) == 0 {
		h.writeError(w, http.StatusBadRequest, "classes array is required and cannot be empty")
		return
	}

	// Sanitize model ID
	modelID := strings.ToLower(strings.ReplaceAll(req.ModelID, " ", "-"))

	// Define storage paths
	weightsPath := fmt.Sprintf("models/%s/weights.pt", modelID)
	classesPath := fmt.Sprintf("models/%s/classes.json", modelID)
	metadataPath := fmt.Sprintf("models/%s/metadata.json", modelID)

	ctx := r.Context()

	// Store classes as JSON
	classesJSON, err := json.Marshal(req.Classes)
	if err != nil {
		h.logger.Error("Failed to marshal classes", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to process classes")
		return
	}

	classesReader := strings.NewReader(string(classesJSON))
	if err := h.minioClient.PutObject(ctx, h.config.MinIO.BucketModels, classesPath, classesReader, int64(len(classesJSON)), "application/json"); err != nil {
		h.logger.Error("Failed to store classes", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to store classes")
		return
	}

	// Store metadata
	metadata := ModelInfo{
		ModelID:     modelID,
		Name:        req.Name,
		Description: req.Description,
		Framework:   req.Framework,
		Classes:     req.Classes,
		WeightsPath: weightsPath,
		ClassesPath: classesPath,
		CreatedAt:   time.Now().UTC().Format(time.RFC3339),
		UpdatedAt:   time.Now().UTC().Format(time.RFC3339),
	}

	metadataJSON, _ := json.Marshal(metadata)
	metadataReader := strings.NewReader(string(metadataJSON))
	if err := h.minioClient.PutObject(ctx, h.config.MinIO.BucketModels, metadataPath, metadataReader, int64(len(metadataJSON)), "application/json"); err != nil {
		h.logger.Error("Failed to store metadata", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to store model metadata")
		return
	}

	// Generate presigned URL for weights upload
	expiry := time.Duration(h.config.MinIO.PresignedURLExpiry) * time.Second
	if expiry == 0 {
		expiry = 15 * time.Minute
	}

	uploadURL, err := h.minioClient.GeneratePresignedPutURL(ctx, h.config.MinIO.BucketModels, weightsPath, expiry)
	if err != nil {
		h.logger.Error("Failed to generate upload URL", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to generate upload URL")
		return
	}

	resp := ModelUploadResponse{
		ModelID:       modelID,
		UploadURL:     uploadURL,
		WeightsPath:   weightsPath,
		ExpiresIn:     int(expiry.Seconds()),
		ClassesPath:   classesPath,
		ClassesStored: true,
	}

	h.writeJSON(w, http.StatusCreated, resp)
	h.logger.Info("Model created", "model_id", modelID, "classes_count", len(req.Classes))
}

// GetModel retrieves model information
// GET /api/v1/models/{id}
func (h *ModelHandler) GetModel(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	ctx := r.Context()
	metadataPath := fmt.Sprintf("models/%s/metadata.json", modelID)

	// Get metadata from MinIO
	obj, err := h.minioClient.GetObject(ctx, h.config.MinIO.BucketModels, metadataPath)
	if err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Model not found: %s", modelID))
		return
	}
	defer obj.Close()

	metadataBytes, err := io.ReadAll(obj)
	if err != nil {
		h.logger.Error("Failed to read metadata", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to read model metadata")
		return
	}

	var modelInfo ModelInfo
	if err := json.Unmarshal(metadataBytes, &modelInfo); err != nil {
		h.logger.Error("Failed to parse metadata", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to parse model metadata")
		return
	}

	// Try to get weights size
	weightsInfo, err := h.minioClient.StatObject(ctx, h.config.MinIO.BucketModels, modelInfo.WeightsPath)
	if err == nil {
		modelInfo.Size = weightsInfo.Size
	}

	h.writeJSON(w, http.StatusOK, modelInfo)
}

// UpdateModelClasses updates the classes for an existing model
// PUT /api/v1/models/{id}/classes
func (h *ModelHandler) UpdateModelClasses(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	var req struct {
		Classes []string `json:"classes"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.writeError(w, http.StatusBadRequest, fmt.Sprintf("Invalid request body: %v", err))
		return
	}

	if len(req.Classes) == 0 {
		h.writeError(w, http.StatusBadRequest, "classes array cannot be empty")
		return
	}

	ctx := r.Context()
	metadataPath := fmt.Sprintf("models/%s/metadata.json", modelID)
	classesPath := fmt.Sprintf("models/%s/classes.json", modelID)

	// Get existing metadata
	obj, err := h.minioClient.GetObject(ctx, h.config.MinIO.BucketModels, metadataPath)
	if err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Model not found: %s", modelID))
		return
	}
	metadataBytes, _ := io.ReadAll(obj)
	obj.Close()

	var modelInfo ModelInfo
	if err := json.Unmarshal(metadataBytes, &modelInfo); err != nil {
		h.writeError(w, http.StatusInternalServerError, "Failed to parse model metadata")
		return
	}

	// Update classes
	modelInfo.Classes = req.Classes
	modelInfo.UpdatedAt = time.Now().UTC().Format(time.RFC3339)

	// Store updated classes
	classesJSON, _ := json.Marshal(req.Classes)
	classesReader := strings.NewReader(string(classesJSON))
	if err := h.minioClient.PutObject(ctx, h.config.MinIO.BucketModels, classesPath, classesReader, int64(len(classesJSON)), "application/json"); err != nil {
		h.writeError(w, http.StatusInternalServerError, "Failed to update classes")
		return
	}

	// Store updated metadata
	metadataJSON, _ := json.Marshal(modelInfo)
	metadataReader := strings.NewReader(string(metadataJSON))
	if err := h.minioClient.PutObject(ctx, h.config.MinIO.BucketModels, metadataPath, metadataReader, int64(len(metadataJSON)), "application/json"); err != nil {
		h.writeError(w, http.StatusInternalServerError, "Failed to update metadata")
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"success":       true,
		"message":       "Classes updated successfully",
		"classes_count": len(req.Classes),
	})
	h.logger.Info("Model classes updated", "model_id", modelID, "classes_count", len(req.Classes))
}

// UpdateModelWeights generates a new presigned URL for uploading weights
// POST /api/v1/models/{id}/weights
func (h *ModelHandler) UpdateModelWeights(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	ctx := r.Context()
	metadataPath := fmt.Sprintf("models/%s/metadata.json", modelID)

	// Verify model exists
	obj, err := h.minioClient.GetObject(ctx, h.config.MinIO.BucketModels, metadataPath)
	if err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Model not found: %s", modelID))
		return
	}
	metadataBytes, _ := io.ReadAll(obj)
	obj.Close()

	var modelInfo ModelInfo
	if err := json.Unmarshal(metadataBytes, &modelInfo); err != nil {
		h.writeError(w, http.StatusInternalServerError, "Failed to parse model metadata")
		return
	}

	// Update timestamp
	modelInfo.UpdatedAt = time.Now().UTC().Format(time.RFC3339)

	// Store updated metadata
	metadataJSON, _ := json.Marshal(modelInfo)
	metadataReader := strings.NewReader(string(metadataJSON))
	if err := h.minioClient.PutObject(ctx, h.config.MinIO.BucketModels, metadataPath, metadataReader, int64(len(metadataJSON)), "application/json"); err != nil {
		h.logger.Error("Failed to update metadata", "error", err)
	}

	// Generate presigned URL for weights upload
	expiry := time.Duration(h.config.MinIO.PresignedURLExpiry) * time.Second
	if expiry == 0 {
		expiry = 15 * time.Minute
	}

	uploadURL, err := h.minioClient.GeneratePresignedPutURL(ctx, h.config.MinIO.BucketModels, modelInfo.WeightsPath, expiry)
	if err != nil {
		h.logger.Error("Failed to generate upload URL", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to generate upload URL")
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"upload_url":         uploadURL,
		"weights_path":       modelInfo.WeightsPath,
		"expires_in_seconds": int(expiry.Seconds()),
	})
	h.logger.Info("Model weights upload URL generated", "model_id", modelID)
}

// DeleteModel removes a model and all associated files
// DELETE /api/v1/models/{id}
func (h *ModelHandler) DeleteModel(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	ctx := r.Context()
	prefix := fmt.Sprintf("models/%s/", modelID)

	// Remove all objects with the model prefix
	if err := h.minioClient.RemoveObjectsWithPrefix(ctx, h.config.MinIO.BucketModels, prefix); err != nil {
		h.logger.Error("Failed to delete model files", "error", err, "model_id", modelID)
		h.writeError(w, http.StatusInternalServerError, "Failed to delete model files")
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"success": true,
		"message": fmt.Sprintf("Model %s deleted successfully", modelID),
	})
	h.logger.Info("Model deleted", "model_id", modelID)
}

// ListModels lists all available models
// GET /api/v1/models
func (h *ModelHandler) ListModels(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()

	// List all metadata files
	prefix := "models/"
	metadataFiles := []string{}

	objectsCh := h.minioClient.ListObjectsWithPrefix(ctx, h.config.MinIO.BucketModels, prefix)
	for objInfo := range objectsCh {
		if objInfo.Err != nil {
			h.logger.Error("Error listing objects", "error", objInfo.Err)
			continue
		}
		if strings.HasSuffix(objInfo.Key, "/metadata.json") {
			metadataFiles = append(metadataFiles, objInfo.Key)
		}
	}

	models := []ModelInfo{}
	for _, metadataPath := range metadataFiles {
		obj, err := h.minioClient.GetObject(ctx, h.config.MinIO.BucketModels, metadataPath)
		if err != nil {
			continue
		}
		metadataBytes, _ := io.ReadAll(obj)
		obj.Close()

		var modelInfo ModelInfo
		if err := json.Unmarshal(metadataBytes, &modelInfo); err != nil {
			continue
		}

		// Try to get weights size
		weightsInfo, err := h.minioClient.StatObject(ctx, h.config.MinIO.BucketModels, modelInfo.WeightsPath)
		if err == nil {
			modelInfo.Size = weightsInfo.Size
		}

		models = append(models, modelInfo)
	}

	h.writeJSON(w, http.StatusOK, ListModelsResponse{
		Models: models,
		Count:  len(models),
	})
}

// GetModelDownloadURL generates a presigned URL for downloading model weights
// GET /api/v1/models/{id}/download
func (h *ModelHandler) GetModelDownloadURL(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	ctx := r.Context()
	metadataPath := fmt.Sprintf("models/%s/metadata.json", modelID)

	// Get model info
	obj, err := h.minioClient.GetObject(ctx, h.config.MinIO.BucketModels, metadataPath)
	if err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Model not found: %s", modelID))
		return
	}
	metadataBytes, _ := io.ReadAll(obj)
	obj.Close()

	var modelInfo ModelInfo
	if err := json.Unmarshal(metadataBytes, &modelInfo); err != nil {
		h.writeError(w, http.StatusInternalServerError, "Failed to parse model metadata")
		return
	}

	// Generate presigned URL for download
	expiry := time.Duration(h.config.MinIO.PresignedURLExpiry) * time.Second
	if expiry == 0 {
		expiry = 15 * time.Minute
	}

	downloadURL, err := h.minioClient.GeneratePresignedGetURL(ctx, h.config.MinIO.BucketModels, modelInfo.WeightsPath, expiry)
	if err != nil {
		h.logger.Error("Failed to generate download URL", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to generate download URL")
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"download_url":       downloadURL,
		"weights_path":       modelInfo.WeightsPath,
		"expires_in_seconds": int(expiry.Seconds()),
	})
}

func (h *ModelHandler) writeJSON(w http.ResponseWriter, status int, v interface{}) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(v)
}

func (h *ModelHandler) writeError(w http.ResponseWriter, status int, message string) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(ErrorResponse{
		Error:   http.StatusText(status),
		Message: message,
	})
}

// LoadModelCommand sends a command to load a model in the engine
// POST /api/v1/models/{id}/load
func (h *ModelHandler) LoadModelCommand(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	var req struct {
		ForceReload bool `json:"force_reload,omitempty"`
	}
	if r.Body != nil && r.ContentLength > 0 {
		json.NewDecoder(r.Body).Decode(&req)
	}

	correlationID := uuid.New().String()

	command := map[string]interface{}{
		"command":        "load_model",
		"correlation_id": correlationID,
		"payload": map[string]interface{}{
			"model_id":     modelID,
			"force_reload": req.ForceReload,
		},
	}

	if err := h.kafkaProducer.PublishModelCommand(r.Context(), command); err != nil {
		h.logger.Error("Failed to publish load model command", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to send load command")
		return
	}

	h.writeJSON(w, http.StatusAccepted, map[string]interface{}{
		"success":        true,
		"message":        "Load model command sent",
		"model_id":       modelID,
		"correlation_id": correlationID,
	})
	h.logger.Info("Load model command sent", "model_id", modelID, "correlation_id", correlationID)
}

// ReloadModelCommand sends a command to reload a model in the engine
// POST /api/v1/models/{id}/reload
func (h *ModelHandler) ReloadModelCommand(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	correlationID := uuid.New().String()

	command := map[string]interface{}{
		"command":        "reload_model",
		"correlation_id": correlationID,
		"payload": map[string]interface{}{
			"model_id": modelID,
		},
	}

	if err := h.kafkaProducer.PublishModelCommand(r.Context(), command); err != nil {
		h.logger.Error("Failed to publish reload model command", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to send reload command")
		return
	}

	h.writeJSON(w, http.StatusAccepted, map[string]interface{}{
		"success":        true,
		"message":        "Reload model command sent",
		"model_id":       modelID,
		"correlation_id": correlationID,
	})
	h.logger.Info("Reload model command sent", "model_id", modelID, "correlation_id", correlationID)
}

// UpdateModelClassesRuntime sends a command to update model classes at runtime
// POST /api/v1/models/{id}/classes/apply
func (h *ModelHandler) UpdateModelClassesRuntime(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	var req struct {
		Classes []string `json:"classes"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.writeError(w, http.StatusBadRequest, fmt.Sprintf("Invalid request body: %v", err))
		return
	}

	if len(req.Classes) == 0 {
		h.writeError(w, http.StatusBadRequest, "classes array cannot be empty")
		return
	}

	correlationID := uuid.New().String()

	command := map[string]interface{}{
		"command":        "update_classes",
		"correlation_id": correlationID,
		"payload": map[string]interface{}{
			"model_id": modelID,
			"classes":  req.Classes,
		},
	}

	if err := h.kafkaProducer.PublishModelCommand(r.Context(), command); err != nil {
		h.logger.Error("Failed to publish update classes command", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to send update classes command")
		return
	}

	h.writeJSON(w, http.StatusAccepted, map[string]interface{}{
		"success":        true,
		"message":        "Update classes command sent",
		"model_id":       modelID,
		"classes_count":  len(req.Classes),
		"correlation_id": correlationID,
	})
	h.logger.Info("Update classes command sent", "model_id", modelID, "classes_count", len(req.Classes), "correlation_id", correlationID)
}

// UnloadModelCommand sends a command to unload a model from engine cache
// POST /api/v1/models/{id}/unload
func (h *ModelHandler) UnloadModelCommand(w http.ResponseWriter, r *http.Request) {
	modelID := r.PathValue("id")
	if modelID == "" {
		h.writeError(w, http.StatusBadRequest, "Model ID is required")
		return
	}

	correlationID := uuid.New().String()

	command := map[string]interface{}{
		"command":        "unload_model",
		"correlation_id": correlationID,
		"payload": map[string]interface{}{
			"model_id": modelID,
		},
	}

	if err := h.kafkaProducer.PublishModelCommand(r.Context(), command); err != nil {
		h.logger.Error("Failed to publish unload model command", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to send unload command")
		return
	}

	h.writeJSON(w, http.StatusAccepted, map[string]interface{}{
		"success":        true,
		"message":        "Unload model command sent",
		"model_id":       modelID,
		"correlation_id": correlationID,
	})
	h.logger.Info("Unload model command sent", "model_id", modelID, "correlation_id", correlationID)
}

// ListCachedModelsCommand sends a command to list cached models in engine
// POST /api/v1/models/cached/list
func (h *ModelHandler) ListCachedModelsCommand(w http.ResponseWriter, r *http.Request) {
	correlationID := uuid.New().String()

	command := map[string]interface{}{
		"command":        "list_models",
		"correlation_id": correlationID,
		"payload": map[string]interface{}{
			"include_storage": false,
		},
	}

	if err := h.kafkaProducer.PublishModelCommand(r.Context(), command); err != nil {
		h.logger.Error("Failed to publish list models command", "error", err)
		h.writeError(w, http.StatusInternalServerError, "Failed to send list command")
		return
	}

	h.writeJSON(w, http.StatusAccepted, map[string]interface{}{
		"success":        true,
		"message":        "List cached models command sent",
		"correlation_id": correlationID,
	})
	h.logger.Info("List cached models command sent", "correlation_id", correlationID)
}
