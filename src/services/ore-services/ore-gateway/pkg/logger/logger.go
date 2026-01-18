package logger

import (
	"log/slog"
	"os"
)

type Logger struct {
	*slog.Logger
}

// New creates a new logger instance
func New(service string) *Logger {
	handler := slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	})

	logger := slog.New(handler).With("service", service)

	return &Logger{Logger: logger}
}

// WithFields adds fields to the logger
func (l *Logger) WithFields(attrs ...any) *Logger {
	return &Logger{Logger: l.Logger.With(attrs...)}
}

// Fatal logs an error message and exits the program
func (l *Logger) Fatal(msg string, args ...any) {
	l.Error(msg, args...)
	os.Exit(1)
}
