// Package errors provides common error handling utilities for the CustomService
package errors

import (
	"fmt"
	"net/http"

	"github.com/gin-gonic/gin"
)

// AppError represents an application-specific error
type AppError struct {
	Code    int    `json:"code"`
	Message string `json:"message"`
	Err     error  `json:"-"`
}

// Error implements the error interface
func (e *AppError) Error() string {
	if e.Err != nil {
		return fmt.Sprintf("%s: %v", e.Message, e.Err)
	}
	return e.Message
}

// Unwrap returns the underlying error
func (e *AppError) Unwrap() error {
	return e.Err
}

// Common error constructors
func NewBadRequestError(message string, err error) *AppError {
	return &AppError{
		Code:    http.StatusBadRequest,
		Message: message,
		Err:     err,
	}
}

func NewNotFoundError(message string) *AppError {
	return &AppError{
		Code:    http.StatusNotFound,
		Message: message,
	}
}

func NewInternalServerError(message string, err error) *AppError {
	return &AppError{
		Code:    http.StatusInternalServerError,
		Message: message,
		Err:     err,
	}
}

func NewServiceUnavailableError(message string, err error) *AppError {
	return &AppError{
		Code:    http.StatusServiceUnavailable,
		Message: message,
		Err:     err,
	}
}

// HandleError is a middleware for handling application errors
func HandleError(c *gin.Context, err error) {
	if appErr, ok := err.(*AppError); ok {
		c.JSON(appErr.Code, gin.H{"error": appErr.Message})
		return
	}
	
	// For unknown errors, return internal server error
	c.JSON(http.StatusInternalServerError, gin.H{"error": "Internal server error"})
}

// ErrorResponse represents a standard error response
type ErrorResponse struct {
	Error   string `json:"error"`
	Code    int    `json:"code,omitempty"`
	Details string `json:"details,omitempty"`
}