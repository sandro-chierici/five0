package handlers

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strings"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/five0/ore/gateway/internal/session"
	"github.com/five0/ore/gateway/pkg/logger"
)

type SessionHandler struct {
	sessionMgr *session.Manager
	config     *config.Config
	logger     *logger.Logger
}

func NewSessionHandler(sessionMgr *session.Manager, cfg *config.Config, log *logger.Logger) *SessionHandler {
	return &SessionHandler{
		sessionMgr: sessionMgr,
		config:     cfg,
		logger:     log,
	}
}

type CreateSessionRequest struct {
	ClientType          string   `json:"client_type"`
	ModelID             string   `json:"model_id,omitempty"`
	ConfidenceThreshold float32  `json:"confidence_threshold,omitempty"`
	TargetClasses       []string `json:"target_classes,omitempty"`
	EnableTracking      bool     `json:"enable_tracking,omitempty"`
	MaxDetections       int32    `json:"max_detections,omitempty"`
}

type CreateSessionResponse struct {
	SessionID      string `json:"session_id"`
	UploadEndpoint string `json:"upload_endpoint"`
	WebSocketURL   string `json:"websocket_url"`
	SessionTTL     int    `json:"session_ttl_seconds"`
}

type ErrorResponse struct {
	Error   string `json:"error"`
	Message string `json:"message,omitempty"`
}

func (h *SessionHandler) CreateSession(w http.ResponseWriter, r *http.Request) {

	var req CreateSessionRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.writeError(w, http.StatusBadRequest, fmt.Sprintf("Invalid request body: %v", err))
		return
	}

	if req.ClientType == "" {
		h.writeError(w, http.StatusBadRequest, "client_type is required")
		return
	}

	modelID := req.ModelID
	if modelID == "" {
		modelID = "yolov8n"
	}

	sess, err := h.sessionMgr.CreateSession(r.Context(), req.ClientType, modelID)
	if err != nil {
		h.logger.Error("Failed to create session", "error", err)
		h.writeError(w, http.StatusInternalServerError, fmt.Sprintf("Failed to create session: %v", err))
		return
	}

	if req.ConfidenceThreshold > 0 {
		sess.ConfThreshold = req.ConfidenceThreshold
	}
	if len(req.TargetClasses) > 0 {
		sess.TargetClasses = req.TargetClasses
	}
	sess.EnableTracking = req.EnableTracking
	if req.MaxDetections > 0 {
		sess.MaxDetections = req.MaxDetections
	}

	if err := h.sessionMgr.UpdateSession(r.Context(), sess); err != nil {
		h.logger.Error("Failed to update session config", "error", err)
	}

	wsURL := fmt.Sprintf("ws://%s/ws/%s", r.Host, sess.ID)
	uploadEndpoint := fmt.Sprintf("http://%s/api/v1/sessions/%s/frames", r.Host, sess.ID)

	resp := CreateSessionResponse{
		SessionID:      sess.ID,
		UploadEndpoint: uploadEndpoint,
		WebSocketURL:   wsURL,
		SessionTTL:     h.config.Redis.SessionTTL,
	}

	h.writeJSON(w, http.StatusCreated, resp)
	h.logger.Info("Session created", "session_id", sess.ID, "client_type", req.ClientType)
}

func (h *SessionHandler) GetSessionStatus(w http.ResponseWriter, r *http.Request) {

	sessionID := r.PathValue("id")
	if sessionID == "" {
		h.writeError(w, http.StatusBadRequest, "Invalid session ID")
		return
	}

	sess, err := h.sessionMgr.GetSession(r.Context(), sessionID)
	if err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Session not found: %v", err))
		return
	}

	resp := map[string]interface{}{
		"session_id":       sess.ID,
		"status":           sess.Status,
		"frames_processed": sess.FramesProcessed,
		"created_at":       sess.CreatedAt,
		"last_activity_at": sess.LastActivity,
		"model_id":         sess.ModelID,
	}

	h.writeJSON(w, http.StatusOK, resp)
}

func (h *SessionHandler) UpdateSession(w http.ResponseWriter, r *http.Request) {

	sessionID := r.PathValue("id")
	if sessionID == "" {
		h.writeError(w, http.StatusBadRequest, "Invalid session ID")
		return
	}

	var req map[string]interface{}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.writeError(w, http.StatusBadRequest, fmt.Sprintf("Invalid request body: %v", err))
		return
	}

	sess, err := h.sessionMgr.GetSession(r.Context(), sessionID)
	if err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Session not found: %v", err))
		return
	}

	if modelID, ok := req["model_id"].(string); ok && modelID != "" {
		sess.ModelID = modelID
	}

	if err := h.sessionMgr.UpdateSession(r.Context(), sess); err != nil {
		h.writeError(w, http.StatusInternalServerError, fmt.Sprintf("Failed to update session: %v", err))
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"success": true,
		"message": "Session updated successfully",
	})
}

func (h *SessionHandler) CloseSession(w http.ResponseWriter, r *http.Request) {

	sessionID := r.PathValue("id")
	if sessionID == "" {
		h.writeError(w, http.StatusBadRequest, "Invalid session ID")
		return
	}

	if err := h.sessionMgr.CloseSession(r.Context(), sessionID); err != nil {
		h.writeError(w, http.StatusInternalServerError, fmt.Sprintf("Failed to close session: %v", err))
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"success": true,
		"message": "Session closed successfully",
	})
	h.logger.Info("Session closed", "session_id", sessionID)
}

func (h *SessionHandler) writeJSON(w http.ResponseWriter, status int, data interface{}) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(data)
}

func (h *SessionHandler) writeError(w http.ResponseWriter, status int, message string) {
	h.writeJSON(w, status, ErrorResponse{
		Error:   http.StatusText(status),
		Message: message,
	})
}

func extractSessionID(path string) string {
	parts := strings.Split(path, "/")
	for i, part := range parts {
		if part == "sessions" && i+1 < len(parts) {
			return parts[i+1]
		}
	}
	return ""
}
