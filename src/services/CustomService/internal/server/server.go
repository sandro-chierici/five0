// Package server provides HTTP server setup and routing
package server

import (
	"log"

	"github.com/gin-gonic/gin"

	"customservice/internal/config"
	"customservice/internal/database"
	"customservice/internal/handlers"
)

// Server represents the HTTP server
type Server struct {
	config   *config.Config
	dbClient *database.Client
	router   *gin.Engine
}

// New creates a new HTTP server
func New(cfg *config.Config, dbClient *database.Client) *Server {
	return &Server{
		config:   cfg,
		dbClient: dbClient,
	}
}

// SetupRoutes configures all the API routes
func (s *Server) SetupRoutes() {
	s.router = gin.Default()

	// Initialize handlers
	healthHandler := handlers.NewHealthHandler(s.dbClient)
	resourceRepository := database.NewResourceRepository(s.dbClient)
	resourceHandler := handlers.NewResourceHandler(resourceRepository)

	// Health check endpoint
	s.router.GET("/api/probes/ready", healthHandler.HealthCheck)

	// API v1 routes
	v1 := s.router.Group("/api/v1")
	{
		// Resource CRUD endpoints
		resources := v1.Group("/resources")
		{
			resources.POST("", resourceHandler.CreateResource)       // Create resource
			resources.GET("", resourceHandler.ListResources)         // List all resources
			resources.GET("/:id", resourceHandler.GetResource)       // Get resource by ID
			resources.PUT("/:id", resourceHandler.UpdateResource)    // Update resource by ID
			resources.DELETE("/:id", resourceHandler.DeleteResource) // Delete resource by ID
		}
	}
}

// Start starts the HTTP server
func (s *Server) Start() error {
	if s.router == nil {
		s.SetupRoutes()
	}

	serverAddr := s.config.GetServerAddr()
	log.Printf("Starting CustomService on %s", serverAddr)
	log.Println("Health check: http://" + serverAddr + "/api/probes/ready")
	log.Println("API endpoints: http://" + serverAddr + "/api/v1/resources")

	return s.router.Run(serverAddr)
}

// GetRouter returns the Gin router (useful for testing)
func (s *Server) GetRouter() *gin.Engine {
	if s.router == nil {
		s.SetupRoutes()
	}
	return s.router
}
