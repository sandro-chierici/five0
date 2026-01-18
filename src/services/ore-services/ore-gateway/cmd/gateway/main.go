package main

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/five0/ore/gateway/internal/handlers"
	"github.com/five0/ore/gateway/internal/kafka"
	"github.com/five0/ore/gateway/internal/session"
	"github.com/five0/ore/gateway/internal/storage"
	"github.com/five0/ore/gateway/internal/websocket"
	"github.com/five0/ore/gateway/pkg/logger"
)

func main() {
	// Initialize logger
	log := logger.New("gateway")
	log.Info("Starting Object Recognition Gateway...")

	// Load configuration
	cfg, err := config.Load("config.yaml")
	if err != nil {
		log.Fatal("Failed to load configuration", "error", err)
	}

	// Override with environment variables
	cfg.OverrideFromEnv()

	// Initialize context
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Initialize Redis store
	redisStore, err := session.NewRedisStore(ctx, cfg.Redis)
	if err != nil {
		log.Fatal("Failed to initialize Redis", "error", err)
	}
	defer redisStore.Close()

	// Initialize MinIO client
	minioClient, err := storage.NewMinIOClient(cfg.MinIO)
	if err != nil {
		log.Fatal("Failed to initialize MinIO", "error", err)
	}

	// Initialize Kafka producer
	kafkaProducer, err := kafka.NewProducer(cfg.Kafka)
	if err != nil {
		log.Fatal("Failed to initialize Kafka producer", "error", err)
	}
	defer kafkaProducer.Close()

	// Initialize session manager
	sessionManager := session.NewManager(redisStore, minioClient, kafkaProducer, cfg)

	// Initialize WebSocket hub
	wsHub := websocket.NewHub(cfg.WebSocket)
	go wsHub.Run(ctx)

	// Initialize Kafka consumer for results
	kafkaConsumer, err := kafka.NewConsumer(cfg.Kafka, wsHub)
	if err != nil {
		log.Fatal("Failed to initialize Kafka consumer", "error", err)
	}
	go kafkaConsumer.Start(ctx)

	// Initialize HTTP handlers
	sessionHandler := handlers.NewSessionHandler(sessionManager, cfg, log)
	frameHandler := handlers.NewFrameHandler(sessionManager, log)

	// Setup HTTP routes
	httpMux := http.NewServeMux()

	// Health check
	httpMux.HandleFunc("GET /health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("OK"))
	})

	// WebSocket endpoint
	httpMux.HandleFunc("GET /ws/", wsHub.HandleWebSocket)

	// REST API endpoints
	httpMux.HandleFunc("POST /api/v1/sessions", sessionHandler.CreateSession)
	httpMux.HandleFunc("GET /api/v1/sessions/{id}", sessionHandler.GetSessionStatus)
	httpMux.HandleFunc("PATCH /api/v1/sessions/{id}", sessionHandler.UpdateSession)
	httpMux.HandleFunc("DELETE /api/v1/sessions/{id}", sessionHandler.CloseSession)

	httpMux.HandleFunc("POST /api/v1/sessions/{id}/frames", frameHandler.SubmitFrame)
	httpMux.HandleFunc("GET /api/v1/sessions/{id}/frames/upload-url", frameHandler.GenerateUploadURL)

	httpServer := &http.Server{
		Addr:    fmt.Sprintf(":%d", cfg.Server.HTTPPort),
		Handler: httpMux,
	}

	// Start HTTP server in goroutine
	go func() {
		log.Info("HTTP REST API server listening", "port", cfg.Server.HTTPPort)
		if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Error("HTTP server error", "error", err)
		}
	}()

	// Wait for interrupt signal
	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, os.Interrupt, syscall.SIGTERM)
	<-sigChan

	log.Info("Shutting down gracefully...")

	// Graceful shutdown
	shutdownCtx, shutdownCancel := context.WithTimeout(context.Background(), cfg.Server.ShutdownTimeout)
	defer shutdownCancel()

	// Stop HTTP server
	if err := httpServer.Shutdown(shutdownCtx); err != nil {
		log.Error("HTTP server shutdown error", "error", err)
	}

	// Cancel context to stop goroutines
	cancel()

	// Wait a bit for cleanup
	time.Sleep(1 * time.Second)

	log.Info("Gateway stopped")
}
