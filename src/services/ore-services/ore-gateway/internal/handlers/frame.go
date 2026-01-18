package handlers

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"
	"time"

	"github.com/five0/ore/gateway/internal/session"
	"github.com/five0/ore/gateway/pkg/logger"
)

type FrameHandler struct {
	sessionMgr *session.Manager
	logger     *logger.Logger
}

func NewFrameHandler(sessionMgr *session.Manager, log *logger.Logger) *FrameHandler {
	return &FrameHandler{
		sessionMgr: sessionMgr,
		logger:     log,
	}
}

func (h *FrameHandler) SubmitFrame(w http.ResponseWriter, r *http.Request) {

	sessionID := r.PathValue("id")
	if sessionID == "" {
		h.writeError(w, http.StatusBadRequest, "Invalid session ID")
		return
	}

	if _, err := h.sessionMgr.GetSession(r.Context(), sessionID); err != nil {
		h.writeError(w, http.StatusNotFound, fmt.Sprintf("Session not found: %v", err))
		return
	}

	var req map[string]interface{}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.writeError(w, http.StatusBadRequest, fmt.Sprintf("Invalid JSON: %v", err))
		return
	}

	frameID, ok := req["frame_id"].(float64)
	if !ok {
		h.writeError(w, http.StatusBadRequest, "frame_id is required")
		return
	}

	storageURL, ok := req["storage_url"].(string)
	if !ok || storageURL == "" {
		h.writeError(w, http.StatusBadRequest, "storage_url is required")
		return
	}

	metadata, _ := req["metadata"].(map[string]interface{})

	frameRef := map[string]interface{}{
		"session_id":   sessionID,
		"frame_id":     int64(frameID),
		"timestamp_ms": time.Now().UnixMilli(),
		"storage_url":  storageURL,
		"metadata":     metadata,
	}

	if err := h.sessionMgr.PublishFrameReference(r.Context(), frameRef); err != nil {
		h.writeError(w, http.StatusInternalServerError, fmt.Sprintf("Failed to publish frame: %v", err))
		return
	}

	if err := h.sessionMgr.UpdateLastActivity(r.Context(), sessionID); err != nil {
		h.logger.Error("Failed to update last activity", "error", err)
	}

	h.writeJSON(w, http.StatusAccepted, map[string]interface{}{
		"accepted":                true,
		"message":                 "Frame accepted for processing",
		"estimated_processing_ms": 100,
	})
}

func (h *FrameHandler) GenerateUploadURL(w http.ResponseWriter, r *http.Request) {

	sessionID := r.PathValue("id")
	if sessionID == "" {
		h.writeError(w, http.StatusBadRequest, "Invalid session ID")
		return
	}

	frameIDStr := r.URL.Query().Get("frame_id")
	if frameIDStr == "" {
		h.writeError(w, http.StatusBadRequest, "frame_id query parameter is required")
		return
	}

	frameID, err := strconv.ParseInt(frameIDStr, 10, 64)
	if err != nil {
		h.writeError(w, http.StatusBadRequest, "Invalid frame_id")
		return
	}

	url, err := h.sessionMgr.GenerateUploadURL(r.Context(), sessionID, frameID)
	if err != nil {
		h.writeError(w, http.StatusInternalServerError, fmt.Sprintf("Failed to generate URL: %v", err))
		return
	}

	h.writeJSON(w, http.StatusOK, map[string]interface{}{
		"upload_url": url,
		"expires_in": 3600,
		"method":     "PUT",
	})
}

func (h *FrameHandler) writeJSON(w http.ResponseWriter, status int, data interface{}) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(data)
}

func (h *FrameHandler) writeError(w http.ResponseWriter, status int, message string) {
	h.writeJSON(w, status, ErrorResponse{
		Error:   http.StatusText(status),
		Message: message,
	})
}
