# ORE Gateway - REST API Documentation

## Overview

The ORE Gateway provides a REST API for managing object recognition sessions, submitting frames for processing, and dynamically managing YOLO models with weights and classes. Model runtime control is handled asynchronously via Kafka messaging.

## Base URL

```
http://localhost:8080/api/v1
```

## Endpoints

---

## Session Endpoints

### 1. Create Session

Create a new recognition session.

**POST** `/sessions`

**Request Body:**
```json
{
  "client_type": "web|mobile|desktop",
  "model_id": "yolov8n",
  "confidence_threshold": 0.5,
  "target_classes": ["person", "car"],
  "enable_tracking": true,
  "max_detections": 100
}
```

**Response** (201 Created):
```json
{
  "session_id": "uuid-here",
  "upload_endpoint": "http://localhost:8080/api/v1/sessions/{id}/frames",
  "websocket_url": "ws://localhost:8080/ws/{id}",
  "session_ttl_seconds": 3600
}
```

### 2. Get Session Status

Retrieve current session status.

**GET** `/sessions/{id}`

**Response** (200 OK):
```json
{
  "session_id": "uuid-here",
  "status": "active",
  "frames_processed": 42,
  "created_at": 1234567890,
  "last_activity_at": 1234567900,
  "model_id": "yolov8n"
}
```

### 3. Update Session

Update session configuration.

**PATCH** `/sessions/{id}`

**Request Body:**
```json
{
  "model_id": "yolov8s",
  "confidence_threshold": 0.6
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Session updated successfully"
}
```

### 4. Close Session

Close and cleanup a session.

**DELETE** `/sessions/{id}`

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Session closed successfully"
}
```

### 5. Submit Frame

Submit a frame for processing (JSON metadata with pre-uploaded frame).

**POST** `/sessions/{id}/frames`

**Request Body:**
```json
{
  "frame_id": 1,
  "storage_url": "minio://ore-frames/session-id/frame-000001.jpg",
  "metadata": {
    "source": "camera-1",
    "timestamp": "2026-01-18T10:30:00Z"
  }
}
```

**Response** (202 Accepted):
```json
{
  "accepted": true,
  "message": "Frame accepted for processing",
  "estimated_processing_ms": 100
}
```

### 6. Generate Upload URL

Get a presigned URL for direct frame upload to MinIO.

**GET** `/sessions/{id}/frames/upload-url?frame_id=1`

**Response** (200 OK):
```json
{
  "upload_url": "https://minio:9000/ore-frames/...",
  "expires_in": 3600,
  "method": "PUT"
}
```

## WebSocket Connection

Connect to receive real-time detection results:

```
ws://localhost:8080/ws/{session_id}
```

**Message Format (from server):**
```json
{
  "session_id": "uuid-here",
  "frame_id": 1,
  "timestamp_ms": 1234567890,
  "detections": [
    {
      "bbox": {"x1": 100, "y1": 200, "x2": 300, "y2": 400},
      "class_id": 0,
      "class_name": "person",
      "confidence": 0.95,
      "track_id": 1
    }
  ],
  "processed_ms": 45.2
}
```

## Health Check

**GET** `/health`

Returns `200 OK` with body `"OK"` when service is healthy.

---

## Model Management Endpoints

### 7. Create Model

Create a new model definition with classes and get a presigned URL for uploading weights.

**POST** `/models`

**Request Body:**
```json
{
  "model_id": "custom-detector",
  "name": "Custom Object Detector",
  "description": "Trained model for industrial parts detection",
  "framework": "yolov8",
  "classes": ["bolt", "nut", "washer", "screw", "gear"]
}
```

**Response** (201 Created):
```json
{
  "model_id": "custom-detector",
  "upload_url": "https://minio:9000/ore-models/...",
  "weights_path": "models/custom-detector/weights.pt",
  "expires_in_seconds": 900,
  "classes_path": "models/custom-detector/classes.json",
  "classes_stored": true
}
```

### 8. List Models

List all available models in storage.

**GET** `/models`

**Response** (200 OK):
```json
{
  "models": [
    {
      "model_id": "custom-detector",
      "name": "Custom Object Detector",
      "framework": "yolov8",
      "classes": ["bolt", "nut", "washer", "screw", "gear"],
      "weights_path": "models/custom-detector/weights.pt",
      "created_at": "2026-02-04T10:30:00Z",
      "updated_at": "2026-02-04T10:30:00Z",
      "size_bytes": 12345678
    }
  ],
  "count": 1
}
```

### 9. Get Model

Get details of a specific model.

**GET** `/models/{id}`

**Response** (200 OK):
```json
{
  "model_id": "custom-detector",
  "name": "Custom Object Detector",
  "description": "Trained model for industrial parts detection",
  "framework": "yolov8",
  "classes": ["bolt", "nut", "washer", "screw", "gear"],
  "weights_path": "models/custom-detector/weights.pt",
  "classes_path": "models/custom-detector/classes.json",
  "created_at": "2026-02-04T10:30:00Z",
  "updated_at": "2026-02-04T10:30:00Z",
  "size_bytes": 12345678
}
```

### 10. Update Model Classes (Storage)

Update classes for a model in storage.

**PUT** `/models/{id}/classes`

**Request Body:**
```json
{
  "classes": ["bolt", "nut", "washer", "screw", "gear", "spring"]
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Classes updated successfully",
  "classes_count": 6
}
```

### 11. Update Model Weights

Get a presigned URL to upload new weights for an existing model.

**POST** `/models/{id}/weights`

**Response** (200 OK):
```json
{
  "upload_url": "https://minio:9000/ore-models/...",
  "weights_path": "models/custom-detector/weights.pt",
  "expires_in_seconds": 900
}
```

### 12. Delete Model

Delete a model and all associated files from storage.

**DELETE** `/models/{id}`

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Model custom-detector deleted successfully"
}
```

### 13. Get Model Download URL

Get a presigned URL to download model weights.

**GET** `/models/{id}/download`

**Response** (200 OK):
```json
{
  "download_url": "https://minio:9000/ore-models/...",
  "weights_path": "models/custom-detector/weights.pt",
  "expires_in_seconds": 900
}
```

---

## Model Runtime Control Endpoints (Async via Kafka)

These endpoints send commands to the ore-engine via Kafka for runtime model management. Responses are asynchronous - the API returns a `correlation_id` that can be used to track the command result.

### 14. Load Model

Load a model from storage into the engine's memory.

**POST** `/models/{id}/load`

**Request Body (optional):**
```json
{
  "force_reload": false
}
```

**Response** (202 Accepted):
```json
{
  "success": true,
  "message": "Load model command sent",
  "model_id": "custom-detector",
  "correlation_id": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 15. Reload Model

Reload a model from storage (fetch latest weights/classes).

**POST** `/models/{id}/reload`

**Response** (202 Accepted):
```json
{
  "success": true,
  "message": "Reload model command sent",
  "model_id": "custom-detector",
  "correlation_id": "550e8400-e29b-41d4-a716-446655440001"
}
```

### 16. Unload Model

Unload a model from engine's memory cache.

**POST** `/models/{id}/unload`

**Response** (202 Accepted):
```json
{
  "success": true,
  "message": "Unload model command sent",
  "model_id": "custom-detector",
  "correlation_id": "550e8400-e29b-41d4-a716-446655440002"
}
```

### 17. Apply Classes at Runtime

Update classes for a loaded model at runtime (without reloading weights).

**POST** `/models/{id}/classes/apply`

**Request Body:**
```json
{
  "classes": ["bolt", "nut", "washer", "screw", "gear", "spring"]
}
```

**Response** (202 Accepted):
```json
{
  "success": true,
  "message": "Update classes command sent",
  "model_id": "custom-detector",
  "classes_count": 6,
  "correlation_id": "550e8400-e29b-41d4-a716-446655440003"
}
```

### 18. List Cached Models

Request list of models currently loaded in engine's cache.

**POST** `/models/cached/list`

**Response** (202 Accepted):
```json
{
  "success": true,
  "message": "List cached models command sent",
  "correlation_id": "550e8400-e29b-41d4-a716-446655440004"
}
```

---

## Kafka Message Formats

### Model Control Command (model-control topic)

```json
{
  "command": "load_model|reload_model|unload_model|update_classes|list_models|get_model_info",
  "correlation_id": "uuid",
  "payload": {
    "model_id": "custom-detector",
    "classes": ["optional", "for", "update_classes"],
    "force_reload": false
  }
}
```

### Model Control Response (model-control-response topic)

```json
{
  "correlation_id": "uuid",
  "result": {
    "success": true,
    "model_id": "custom-detector",
    "message": "Model loaded successfully",
    "classes_count": 5,
    "classes": ["bolt", "nut", "washer", "screw", "gear"]
  }
}
```

---

## Error Responses

All errors follow this format:

```json
{
  "error": "Not Found",
  "message": "Session not found: session does not exist"
}
```

## Example Usage

### Python Example

```python
import requests
import json

# Create session
response = requests.post('http://localhost:8080/api/v1/sessions', json={
    'client_type': 'python-client',
    'model_id': 'yolov8n',
    'confidence_threshold': 0.5
})
session = response.json()
session_id = session['session_id']
print(f"Session created: {session_id}")

# Submit frame
frame_data = {
    'frame_id': 1,
    'storage_url': 'minio://ore-frames/test/frame-001.jpg',
    'metadata': {'source': 'test'}
}
response = requests.post(
    f'http://localhost:8080/api/v1/sessions/{session_id}/frames',
    json=frame_data
)
print(f"Frame submitted: {response.json()}")

# Get session status
response = requests.get(f'http://localhost:8080/api/v1/sessions/{session_id}')
print(f"Session status: {response.json()}")

# Close session
response = requests.delete(f'http://localhost:8080/api/v1/sessions/{session_id}')
print(f"Session closed: {response.json()}")
```

### curl Example

```bash
# Create session
curl -X POST http://localhost:8080/api/v1/sessions \
  -H "Content-Type: application/json" \
  -d '{"client_type":"curl-test","model_id":"yolov8n"}'

# Submit frame
curl -X POST http://localhost:8080/api/v1/sessions/{SESSION_ID}/frames \
  -H "Content-Type: application/json" \
  -d '{"frame_id":1,"storage_url":"minio://ore-frames/test/frame-001.jpg"}'

# Get status
curl http://localhost:8080/api/v1/sessions/{SESSION_ID}

# Close session
curl -X DELETE http://localhost:8080/api/v1/sessions/{SESSION_ID}
```

## Configuration

See `config.yaml` for service configuration options:
- Server ports (HTTP, no gRPC needed)
- Kafka brokers and topics
- Redis connection
- MinIO endpoints
- WebSocket timeouts

## Building

```bash
# Local build
make build

# Docker build
docker build -t ore-gateway:latest .

# Run locally
./bin/gateway

# Run with Docker Compose
cd .. && docker-compose up ore-gateway
```

## Dependencies

- Redis (session storage)
- Kafka (message queue)
- MinIO (object storage)
- No protobuf/gRPC dependencies

## Development

The gateway is now fully REST-based:
- No protobuf compilation needed
- Standard HTTP/JSON APIs
- WebSocket for real-time results
- Simple curl/Postman testing
