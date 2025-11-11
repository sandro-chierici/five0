# CustomService

A well-structured Go microservice that provides a REST API for managing resources in MongoDB, following Go best practices and standard project layout.

## Features

- **Clean Architecture**: Follows standard Go project layout with proper separation of concerns
- **MongoDB Integration**: Connects to MongoDB for data persistence with repository pattern
- **Health Check**: `/health` endpoint for service monitoring
- **CRUD API**: Complete Create, Read, Update, Delete operations for resources
- **JSON Configuration**: All settings managed through `config.json`
- **Error Handling**: Structured error handling with custom error types
- **Containerized**: Docker support for easy deployment
- **Make Support**: Makefile for common development tasks

## API Endpoints

### Health Check
- `GET /health` - Service health status and database connectivity

### Resources API (v1)
- `POST /api/v1/resources` - Create a new resource
- `GET /api/v1/resources` - List all resources
- `GET /api/v1/resources/{id}` - Get a specific resource by ID
- `PUT /api/v1/resources/{id}` - Update a resource by ID  
- `DELETE /api/v1/resources/{id}` - Delete a resource by ID

## Resource Schema

Resources are stored as documents with the following structure:
```json
{
  "_id": "ObjectId",
  "specs": {
    // TBD - flexible key-value pairs for resource specifications
  }
}
```

## Configuration

Edit `config.json` to customize:

```json
{
  "server": {
    "port": "8080",
    "host": "0.0.0.0"
  },
  "database": {
    "mongodb_uri": "mongodb://localhost:27017",
    "database_name": "customservice_db",
    "collection_name": "resources"
  },
  "logging": {
    "level": "info"
  }
}
```

## Prerequisites

- Go 1.21 or higher
- MongoDB instance
- Docker (optional)

## Project Structure

```
CustomService/
├── cmd/
│   └── customservice/          # Application entry points
│       └── main.go
├── internal/                   # Private application code
│   ├── config/                 # Configuration management
│   ├── database/               # Database clients and repositories
│   ├── handlers/               # HTTP handlers
│   ├── models/                 # Data models and DTOs
│   └── server/                 # HTTP server setup
├── pkg/                        # Public library code
│   └── errors/                 # Error handling utilities
├── config.json                 # Configuration file
├── Dockerfile                  # Container build file
├── Makefile                    # Build automation
└── go.mod                      # Go module definition
```

## Local Development

1. **Start MongoDB:**
   ```bash
   # Using Docker
   docker run -d -p 27017:27017 --name mongodb mongo:latest
   
   # Or use local MongoDB installation
   ```

2. **Install dependencies:**
   ```bash
   make deps
   ```

3. **Run the service:**
   ```bash
   make run
   # Or directly: go run ./cmd/customservice
   ```

4. **Test the service:**
   ```bash
   curl http://localhost:8080/health
   ```

## Build Commands

```bash
make build          # Build the application
make run            # Run the application
make test           # Run tests
make clean          # Clean build artifacts
make fmt            # Format code
make vet            # Vet code
make lint           # Lint code (requires golangci-lint)
make docker-build   # Build Docker image
make docker-run     # Run Docker container
make dev-setup      # Setup development environment
make help           # Show available commands
```

## Docker Deployment

1. **Build image:**
   ```bash
   docker build -t customservice .
   ```

2. **Run container:**
   ```bash
   docker run -p 8080:8080 customservice
   ```

## Example Usage

### Create a Resource
```bash
curl -X POST http://localhost:8080/api/v1/resources \
  -H "Content-Type: application/json" \
  -d '{
    "specs": {
      "name": "server-01",
      "type": "virtual-machine",
      "cpu_cores": 4,
      "memory_gb": 16,
      "region": "us-east-1"
    }
  }'
```

### List Resources
```bash
curl http://localhost:8080/api/v1/resources
```

### Get a Resource
```bash
curl http://localhost:8080/api/v1/resources/{resource-id}
```

### Update a Resource
```bash
curl -X PUT http://localhost:8080/api/v1/resources/{resource-id} \
  -H "Content-Type: application/json" \
  -d '{
    "specs": {
      "name": "server-01-updated",
      "type": "virtual-machine",
      "cpu_cores": 8,
      "memory_gb": 32,
      "region": "us-east-1"
    }
  }'
```

### Delete a Resource
```bash
curl -X DELETE http://localhost:8080/api/v1/resources/{resource-id}
```

## Development Notes

- The service uses Gin framework for HTTP routing
- MongoDB driver handles database operations with proper timeouts
- Graceful shutdown is implemented with signal handling
- All configuration is externalized to `config.json`
- Error handling includes proper HTTP status codes and messages