.PHONY: proto build run test clean docker-build docker-push

# Variables
SERVICE_NAME=ore-gateway
PROTO_DIR=../proto
PROTO_OUT_DIR=./proto/recognition/v1
DOCKER_REGISTRY=localhost:5000
VERSION?=latest

# Generate protobuf code
proto:
	@echo "Generating protobuf code..."
	@mkdir -p $(PROTO_OUT_DIR)
	protoc --go_out=$(PROTO_OUT_DIR) --go_opt=paths=source_relative \
		--go-grpc_out=$(PROTO_OUT_DIR) --go-grpc_opt=paths=source_relative \
		-I$(PROTO_DIR) $(PROTO_DIR)/recognition.proto
	@echo "Protobuf code generated successfully"

# Build the service
build: proto
	@echo "Building $(SERVICE_NAME)..."
	go build -o bin/$(SERVICE_NAME) ./cmd/gateway
	@echo "Build complete: bin/$(SERVICE_NAME)"

# Run the service locally
run: build
	@echo "Running $(SERVICE_NAME)..."
	./bin/$(SERVICE_NAME)

# Run tests
test:
	@echo "Running tests..."
	go test -v -race -cover ./...

# Clean build artifacts
clean:
	@echo "Cleaning..."
	rm -rf bin/
	rm -rf $(PROTO_OUT_DIR)
	go clean

# Format code
fmt:
	@echo "Formatting code..."
	go fmt ./...
	goimports -w .

# Lint code
lint:
	@echo "Linting code..."
	golangci-lint run

# Download dependencies
deps:
	@echo "Downloading dependencies..."
	go mod download
	go mod tidy

# Docker build
docker-build:
	@echo "Building Docker image..."
	docker build -t $(DOCKER_REGISTRY)/$(SERVICE_NAME):$(VERSION) .
	@echo "Docker image built: $(DOCKER_REGISTRY)/$(SERVICE_NAME):$(VERSION)"

# Docker push
docker-push: docker-build
	@echo "Pushing Docker image..."
	docker push $(DOCKER_REGISTRY)/$(SERVICE_NAME):$(VERSION)

# Development setup
dev-setup:
	@echo "Setting up development environment..."
	go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
	go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest
	go install golang.org/x/tools/cmd/goimports@latest
	go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest

help:
	@echo "Available targets:"
	@echo "  proto        - Generate protobuf code"
	@echo "  build        - Build the service"
	@echo "  run          - Run the service locally"
	@echo "  test         - Run tests"
	@echo "  clean        - Clean build artifacts"
	@echo "  fmt          - Format code"
	@echo "  lint         - Lint code"
	@echo "  deps         - Download dependencies"
	@echo "  docker-build - Build Docker image"
	@echo "  docker-push  - Push Docker image"
	@echo "  dev-setup    - Setup development environment"
