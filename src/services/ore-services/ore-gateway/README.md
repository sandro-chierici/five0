# Object Recognition Gateway - Go Service

High-performance gateway service for managing client sessions and real-time communication.

## Features

- gRPC API for session management
- WebSocket support for real-time results
- Redis-backed session storage
- Kafka integration for event streaming
- MinIO presigned URL generation
- Concurrent client handling

## Installation

### Prerequisites

- Go 1.21+
- protoc compiler
- protoc-gen-go and protoc-gen-go-grpc plugins

```bash
# Install protoc plugins
go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest
```

### Build

```bash
# Download dependencies
go mod download

# Generate protobuf code
make proto

# Build binary
make build
```

## Usage

### Standalone

```bash
# Edit config.yaml
./bin/ore-gateway
```

### Docker

```bash
docker build -t ore-gateway:latest .
docker run -it --rm -p 50051:50051 -p 8080:8080 ore-gateway:latest
```

## API

### gRPC Endpoints

#### CreateSession

```protobuf
rpc CreateSession(CreateSessionRequest) returns (CreateSessionResponse);
```

Creates a new recognition session.

**Request:**
```json
{
  "client_type": "web",
  "config": {
    "model_id": "yolov8n",
    "confidence_threshold": 0.7,
    "target_classes": ["person", "car"],
    "enable_tracking": false,
    "max_detections_per_frame": 100
  }
}
```

**Response:**
```json
{
  "session_id": "uuid-123",
  "upload_endpoint": "http://localhost:8080/upload/uuid-123",
  "websocket_url": "ws://localhost:8080/ws/uuid-123",
  "session_ttl_seconds": 3600
}
```

#### SubmitFrame

```protobuf
rpc SubmitFrame(SubmitFrameRequest) returns (SubmitFrameResponse);
```

Submit a frame for processing.

**Request:**
```json
{
  "session_id": "uuid-123",
  "frame_id": 1,
  "storage_url": "minio://ore-frames/uuid-123/frame-000001.jpg",
  "metadata": {
    "resolution": "1920x1080",
    "fps": "30"
  }
}
```

### WebSocket

Connect to `ws://localhost:8080/ws/{session_id}` to receive real-time detection results.

**Message Format:**
```json
{
  "type": "detection_result",
  "session_id": "uuid-123",
  "frame_id": 1,
  "detections": [
    {
      "class": "person",
      "confidence": 0.95,
      "bbox": {"x": 0.3, "y": 0.4, "width": 0.1, "height": 0.15}
    }
  ],
  "processing_time_ms": 50
}
```

## Configuration

Edit `config.yaml`:

```yaml
server:
  grpc_port: 50051
  http_port: 8080
  shutdown_timeout: 30s

kafka:
  brokers:
    - kafka-1:9092
    - kafka-2:9092
    - kafka-3:9092

redis:
  url: redis:6379
  session_ttl: 3600

minio:
  endpoint: minio:9000
  access_key: minioadmin
  secret_key: minioadmin
```

### Environment Variables

- `KAFKA_BROKERS` - Override Kafka brokers
- `REDIS_URL` - Override Redis URL
- `MINIO_ENDPOINT` - Override MinIO endpoint
- `LOG_LEVEL` - Set logging level

## Development

### Running Tests

```bash
make test
```

### Code Generation

```bash
# Regenerate protobuf code
make proto
```

### Linting

```bash
make lint
```

## Performance

- Handles 1000+ concurrent WebSocket connections
- <10ms latency for session operations
- Kafka batch publishing for efficiency

## Monitoring

Metrics are exposed for Prometheus scraping (planned).

## License

See main repository LICENSE.
