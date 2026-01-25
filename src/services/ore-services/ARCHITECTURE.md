# Object Recognition Engine (ORE) - System Architecture

**Version:** 1.0  
**Date:** January 18, 2026  
**Status:** Design Phase

## Table of Contents
1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Service Components](#service-components)
4. [Communication Protocols](#communication-protocols)
5. [Infrastructure Components](#infrastructure-components)
6. [Data Flow](#data-flow)
7. [Deployment Strategy](#deployment-strategy)
8. [Scalability & Performance](#scalability--performance)
9. [Technology Stack](#technology-stack)
10. [Development Phases](#development-phases)

---

## Overview

### Purpose
The Object Recognition Engine (ORE) is a real-time object detection system designed for industrial applications. It processes video streams and uploaded images to detect and classify custom objects using trained ML models.

### Key Features
- Real-time object detection from video streams
- Support for uploaded image analysis
- Multi-client session management
- Custom model training for industrial objects (orthogonal views + 3D images)
- Dynamic object class management (tens to thousands of classes)
- Kafka-based event-driven architecture
- WebSocket real-time results delivery
- Containerized deployment (Docker Compose / Kubernetes)

### Design Principles
- **Microservices Architecture**: Separate concerns between management and inference
- **Language-Appropriate**: Go for I/O-intensive operations, Python for ML
- **Event-Driven**: Kafka as the central nervous system
- **Scalable**: Horizontal scaling for both services
- **Cloud-Native**: Container-first design for K8s/K3s deployment

---

## System Architecture

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                                │
│  ┌──────────────┐         ┌──────────────┐                         │
│  │  Web Client  │         │   Electron   │                         │
│  │  (Browser)   │         │   Desktop    │                         │
│  └───────┬──────┘         └───────┬──────┘                         │
└──────────┼────────────────────────┼─────────────────────────────────┘
           │                        │
           │ gRPC/REST              │ WebSocket (results)
           │                        │
┌──────────▼────────────────────────▼─────────────────────────────────┐
│              OBJECT RECOGNITION GATEWAY (Go)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐          │
│  │ gRPC Server  │  │  WebSocket   │  │  HTTP/REST API  │          │
│  │              │  │     Hub      │  │                 │          │
│  └──────┬───────┘  └───────▲──────┘  └─────────────────┘          │
│         │                   │                                       │
│  ┌──────▼───────────────────┴──────┐                               │
│  │    Session Manager (Redis)      │                                 │
│  └──────┬───────────────────▲──────┘                               │
│         │                   │                                       │
│  ┌──────▼──────┐    ┌──────┴──────┐                               │
│  │   Kafka     │    │   Kafka     │                               │
│  │  Producer   │    │  Consumer   │                               │
│  └──────┬──────┘    └──────▲──────┘                               │
└─────────┼──────────────────┼────────────────────────────────────────┘
          │                  │
          │  ┌───────────────┴─────────────────┐
          │  │                                  │
          │  │        MinIO Object Storage      │
          │  │    (Frame/Image Storage)         │
          │  │                                  │
          │  └───────────────┬─────────────────┘
          │                  │
┌─────────▼──────────────────▼─────────────────────────────────────────┐
│                       KAFKA CLUSTER (KRaft)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                 │
│  │  Broker 1   │  │  Broker 2   │  │  Broker 3   │                 │
│  │ Controller  │  │ Controller  │  │ Controller  │                 │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘                 │
│         │                │                │                          │
│  Topics: video-frames-input, detection-results, session-control     │
└─────────┼────────────────┼────────────────┼──────────────────────────┘
          │                │                │
┌─────────▼────────────────▼────────────────▼──────────────────────────┐
│           OBJECT RECOGNITION ENGINE (Python)                         │
│  ┌───────────────────────────────────────────────────────┐          │
│  │             Kafka Consumer (Frame Refs)               │          │
│  └───────────────────────┬───────────────────────────────┘          │
│                          │                                           │
│  ┌───────────────────────▼───────────────────────────────┐          │
│  │          MinIO Client (Fetch Frames)                  │          │
│  └───────────────────────┬───────────────────────────────┘          │
│                          │                                           │
│  ┌───────────────────────▼───────────────────────────────┐          │
│  │   Inference Engine (YOLOv8/Custom Models)             │          │
│  │   - Model Loader                                       │          │
│  │   - GPU Acceleration                                   │          │
│  │   - Object Tracking (Optional)                         │          │
│  └───────────────────────┬───────────────────────────────┘          │
│                          │                                           │
│  ┌───────────────────────▼───────────────────────────────┐          │
│  │         Kafka Producer (Detection Results)             │          │
│  └───────────────────────────────────────────────────────┘          │
└──────────────────────────────────────────────────────────────────────┘
```

### Component Breakdown

#### 1. **Object Recognition Gateway (Go Service)**
- **Purpose**: Client-facing service handling session management and real-time communication
- **Technology**: Go 1.21+
- **Responsibilities**:
  - Session lifecycle management (create, track, close)
  - gRPC API for client operations
  - WebSocket hub for real-time result delivery
  - Frame upload coordination with MinIO
  - Kafka producer (frame references)
  - Kafka consumer (detection results routing)
  - Redis integration for session state

#### 2. **Object Recognition Engine (Python Service)**
- **Purpose**: ML inference service for object detection
- **Technology**: Python 3.11+, PyTorch/YOLOv8
- **Responsibilities**:
  - Kafka consumer for frame references
  - Frame fetching from MinIO
  - ML model loading and inference
  - GPU utilization and optimization
  - Detection result serialization
  - Kafka producer for results

#### 3. **Kafka Cluster (KRaft Mode)**
- **Purpose**: Event streaming and message broker
- **Technology**: Apache Kafka 3.6+ (KRaft mode)
- **Configuration**: 3 broker cluster with combined controller mode
- **Topics**:
  - `video-frames-input`: Frame reference messages from gateway
  - `detection-results`: Detection outputs from engine
  - `session-control`: Session lifecycle events
  - `model-updates`: Model management commands
  - `config-updates`: Runtime configuration changes

#### 4. **Redis**
- **Purpose**: Session state cache
- **Technology**: Redis 7+
- **Data Structures**:
  - Session metadata (hash)
  - Session expiry (TTL)
  - Active connections tracking

#### 5. **MinIO**
- **Purpose**: Object storage for frames/images
- **Technology**: MinIO latest
- **Buckets**:
  - `ore-frames`: Temporary frame storage
  - `ore-models`: Model weights storage
  - `ore-results`: Optional result archiving

---

## Service Components

### Object Recognition Gateway (Go)

#### Directory Structure
```
object-recognition-gateway/
├── cmd/
│   └── gateway/
│       └── main.go                    # Entry point
├── internal/
│   ├── session/
│   │   ├── manager.go                 # Session lifecycle management
│   │   ├── store.go                   # Redis operations
│   │   └── models.go                  # Session data structures
│   ├── grpc/
│   │   ├── server.go                  # gRPC server setup
│   │   └── handlers.go                # Service implementation
│   ├── websocket/
│   │   ├── hub.go                     # WebSocket connection manager
│   │   ├── client.go                  # Individual client handler
│   │   └── handler.go                 # HTTP WebSocket upgrade
│   ├── kafka/
│   │   ├── producer.go                # Frame reference publisher
│   │   ├── consumer.go                # Result consumer & router
│   │   └── config.go                  # Kafka configuration
│   ├── storage/
│   │   ├── minio.go                   # MinIO client wrapper
│   │   └── upload.go                  # Upload handling
│   └── config/
│       └── config.go                  # Configuration loader
├── proto/
│   └── recognition.proto              # gRPC & Kafka message definitions
├── pkg/
│   ├── logger/
│   │   └── logger.go                  # Structured logging
│   └── metrics/
│       └── metrics.go                 # Prometheus metrics
├── config.yaml                         # Service configuration
├── Dockerfile                          # Container image
├── Makefile                            # Build automation
├── go.mod
├── go.sum
└── README.md
```

#### Key Interfaces

**Session Manager**
```go
type SessionManager interface {
    CreateSession(ctx context.Context, req *CreateSessionRequest) (*Session, error)
    GetSession(ctx context.Context, sessionID string) (*Session, error)
    UpdateLastActivity(ctx context.Context, sessionID string) error
    CloseSession(ctx context.Context, sessionID string) error
    ListActiveSessions(ctx context.Context) ([]*Session, error)
}
```

**WebSocket Hub**
```go
type Hub interface {
    RegisterClient(sessionID string, client *Client) error
    UnregisterClient(sessionID string) error
    BroadcastToSession(sessionID string, message []byte) error
    Run(ctx context.Context) error
}
```

### Object Recognition Engine (Python)

#### Directory Structure
```
object-recognition-engine/
├── main.py                            # Entry point
├── config.yaml                        # Service configuration
├── requirements.txt                   # Python dependencies
├── Dockerfile                         # Container image
├── proto/
│   ├── recognition_pb2.py            # Generated protobuf (Python)
│   └── recognition_pb2.pyi           # Type hints
├── src/
│   ├── __init__.py
│   ├── config/
│   │   └── settings.py               # Configuration management
│   ├── kafka/
│   │   ├── __init__.py
│   │   ├── consumer.py               # Frame reference consumer
│   │   ├── producer.py               # Result producer
│   │   └── serializer.py             # Protobuf serialization
│   ├── storage/
│   │   ├── __init__.py
│   │   └── minio_client.py           # MinIO operations
│   ├── models/
│   │   ├── __init__.py
│   │   ├── loader.py                 # Model loading & caching
│   │   ├── inference.py              # Detection logic
│   │   ├── registry.py               # Model versioning
│   │   └── yolo.py                   # YOLOv8 wrapper
│   ├── processors/
│   │   ├── __init__.py
│   │   ├── frame_processor.py        # Main processing pipeline
│   │   └── tracker.py                # Object tracking (optional)
│   └── utils/
│       ├── __init__.py
│       ├── image.py                  # Image preprocessing
│       ├── metrics.py                # Performance metrics
│       └── logger.py                 # Logging setup
├── tests/
│   ├── test_inference.py
│   ├── test_kafka.py
│   └── test_storage.py
└── README.md
```

#### Key Classes

**Frame Processor**
```python
class FrameProcessor:
    def __init__(self, model_loader, storage_client, kafka_producer):
        pass
    
    async def process_frame(self, frame_ref: FrameReference) -> DetectionResult:
        # Fetch frame from MinIO
        # Run inference
        # Format results
        # Publish to Kafka
        pass
```

**Model Loader**
```python
class ModelLoader:
    def load_model(self, model_id: str, device: str = 'cuda') -> Model:
        pass
    
    def get_cached_model(self, model_id: str) -> Optional[Model]:
        pass
    
    def switch_model(self, model_id: str) -> None:
        pass
```

---

## Communication Protocols

### 1. gRPC API (Client ↔ Gateway)

#### Service Definition (Protobuf)

```protobuf
syntax = "proto3";

package recognition.v1;

option go_package = "github.com/five0/ore/proto/recognition/v1";

// Main service interface
service RecognitionGateway {
  // Session management
  rpc CreateSession(CreateSessionRequest) returns (CreateSessionResponse);
  rpc GetSessionStatus(GetSessionStatusRequest) returns (GetSessionStatusResponse);
  rpc UpdateSessionConfig(UpdateSessionConfigRequest) returns (UpdateSessionConfigResponse);
  rpc CloseSession(CloseSessionRequest) returns (CloseSessionResponse);
  
  // Frame submission
  rpc SubmitFrame(SubmitFrameRequest) returns (SubmitFrameResponse);
  rpc SubmitFrameBatch(stream SubmitFrameRequest) returns (SubmitFrameBatchResponse);
  
  // Model management
  rpc ListModels(ListModelsRequest) returns (ListModelsResponse);
  rpc GetModelInfo(GetModelInfoRequest) returns (GetModelInfoResponse);
}

// Session Messages
message CreateSessionRequest {
  string client_type = 1;  // "web" | "electron"
  SessionConfig config = 2;
}

message CreateSessionResponse {
  string session_id = 1;
  string upload_endpoint = 2;  // MinIO presigned URL base
  string websocket_url = 3;
  int64 session_ttl_seconds = 4;
}

message SessionConfig {
  string model_id = 1;
  float confidence_threshold = 2;  // 0.0 - 1.0
  repeated string target_classes = 3;  // Filter specific classes
  bool enable_tracking = 4;
  int32 max_detections_per_frame = 5;
}

message GetSessionStatusRequest {
  string session_id = 1;
}

message GetSessionStatusResponse {
  string session_id = 1;
  string status = 2;  // "active" | "inactive" | "expired"
  int64 frames_processed = 3;
  int64 created_at = 4;
  int64 last_activity_at = 5;
  SessionConfig config = 6;
}

message CloseSessionRequest {
  string session_id = 1;
}

message CloseSessionResponse {
  bool success = 1;
  string message = 2;
}

// Frame Submission Messages
message SubmitFrameRequest {
  string session_id = 1;
  int64 frame_id = 2;
  oneof frame_data {
    bytes inline_image = 3;      // For small images (<1MB)
    string storage_url = 4;       // Pre-uploaded to MinIO
  }
  map<string, string> metadata = 5;  // resolution, fps, timestamp, etc.
}

message SubmitFrameResponse {
  bool accepted = 1;
  string message = 2;
  int64 estimated_processing_ms = 3;
}

message SubmitFrameBatchResponse {
  int32 frames_accepted = 1;
  int32 frames_rejected = 2;
}

// Model Management Messages
message ListModelsRequest {
  int32 page = 1;
  int32 page_size = 2;
}

message ListModelsResponse {
  repeated ModelInfo models = 1;
  int32 total_count = 2;
}

message ModelInfo {
  string model_id = 1;
  string name = 2;
  string version = 3;
  repeated string supported_classes = 4;
  string framework = 5;  // "yolov8", "detr", etc.
  ModelMetrics metrics = 6;
}

message ModelMetrics {
  float map50 = 1;  // Mean Average Precision @ IoU 0.5
  float map50_95 = 2;
  int32 avg_inference_ms = 3;
}
```

### 2. Kafka Messages (Internal Communication)

```protobuf
// Frame Reference (Gateway → Engine)
message FrameReference {
  string session_id = 1;
  int64 frame_id = 2;
  int64 timestamp_ms = 3;
  string storage_url = 4;  // minio://bucket/path/to/frame.jpg
  string model_id = 5;
  SessionConfig config = 6;
  map<string, string> metadata = 7;
}

// Detection Result (Engine → Gateway)
message DetectionResult {
  string session_id = 1;
  int64 frame_id = 2;
  int64 timestamp_ms = 3;
  int32 processing_time_ms = 4;
  repeated Detection detections = 5;
  string model_version = 6;
  ResultMetadata metadata = 7;
}

message Detection {
  string class_name = 1;
  float confidence = 2;
  BoundingBox bbox = 3;
  optional string tracking_id = 4;  // For multi-frame tracking
  map<string, float> attributes = 5;  // Additional properties
}

message BoundingBox {
  float x = 1;       // Top-left X (normalized 0-1)
  float y = 2;       // Top-left Y (normalized 0-1)
  float width = 3;   // Width (normalized 0-1)
  float height = 4;  // Height (normalized 0-1)
}

message ResultMetadata {
  int32 total_objects = 1;
  float avg_confidence = 2;
  string processing_node = 3;  // For debugging
}

// Session Control Messages
message SessionEvent {
  string session_id = 1;
  string event_type = 2;  // "created" | "updated" | "closed"
  int64 timestamp_ms = 3;
  map<string, string> details = 4;
}
```

### 3. WebSocket Protocol (Gateway → Client)

**Connection Establishment**
```
ws://gateway:8080/ws/{session_id}?token={auth_token}
```

**Message Format (JSON)**
```json
{
  "type": "detection_result",
  "session_id": "uuid-123",
  "frame_id": 42,
  "timestamp_ms": 1737235200000,
  "detections": [
    {
      "class": "bolt",
      "confidence": 0.95,
      "bbox": {"x": 0.3, "y": 0.4, "width": 0.1, "height": 0.15},
      "tracking_id": "track-001"
    }
  ],
  "processing_time_ms": 85
}
```

**Message Types**
- `detection_result`: Real-time detection output
- `session_status`: Session state updates
- `error`: Error notifications
- `heartbeat`: Keep-alive ping/pong

---

## Infrastructure Components

### Kafka Cluster (KRaft Mode)

#### Why KRaft?
- **Simplified Architecture**: No Zookeeper dependency
- **Better Performance**: Lower latency for metadata operations
- **Improved Scalability**: Supports millions of partitions
- **Operational Simplicity**: One less system to manage
- **Future-Proof**: Zookeeper deprecated in Kafka 4.0+

#### Configuration

**3-Node Cluster Setup**
```yaml
# Broker 1 (Controller + Broker)
KAFKA_NODE_ID: 1
KAFKA_PROCESS_ROLES: broker,controller
KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka-1:9093,2@kafka-2:9093,3@kafka-3:9093
KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
KAFKA_LOG_DIRS: /var/lib/kafka/data

# Similar for Broker 2 (NODE_ID=2) and Broker 3 (NODE_ID=3)
```

#### Topic Configuration
```bash
# video-frames-input
Partitions: 12 (for load balancing across engine instances)
Replication Factor: 3
Retention: 1 hour (short-lived)
Compression: lz4

# detection-results
Partitions: 12 (matches input for ordering)
Replication Factor: 3
Retention: 1 hour
Compression: lz4

# session-control
Partitions: 3
Replication Factor: 3
Retention: 7 days
Compaction: enabled (latest session state)
```

### Redis Configuration

```yaml
# Persistence
save: "900 1 300 10 60 10000"  # RDB snapshots
appendonly: yes                 # AOF for durability

# Memory
maxmemory: 2gb
maxmemory-policy: allkeys-lru  # Evict on memory pressure

# Performance
tcp-backlog: 511
timeout: 300
```

**Data Schemas**

```redis
# Session metadata
HSET sessions:{session_id} 
  client_type "web"
  model_id "yolov8-industrial-v2"
  confidence_threshold "0.7"
  created_at "1737235200"
  last_activity "1737235500"
  frames_processed "142"

EXPIRE sessions:{session_id} 3600  # 1 hour TTL

# Active WebSocket connections
SADD ws_connections:{session_id} "conn-uuid-1" "conn-uuid-2"

# Session index (for listing)
ZADD active_sessions 1737235500 "session-uuid-1"  # Score = last_activity
```

### MinIO Configuration

```yaml
# Buckets
ore-frames:
  versioning: false
  lifecycle:
    - expiration: 2h  # Auto-delete after 2 hours
  
ore-models:
  versioning: true
  lifecycle:
    - transition_to_glacier: 90d  # Archive old models

# Performance
MINIO_API_REQUESTS_MAX: 1000
MINIO_API_REQUESTS_DEADLINE: 10s
```

**Directory Structure**
```
ore-frames/
  ├── {session_id}/
  │   ├── frame-0001.jpg
  │   ├── frame-0002.jpg
  │   └── ...

ore-models/
  ├── yolov8-industrial-v1/
  │   ├── model.pt
  │   ├── config.yaml
  │   └── classes.txt
  └── yolov8-industrial-v2/
      ├── model.pt
      └── ...
```

---

## Data Flow

### End-to-End Flow: Video Frame Processing

```
┌─────────┐
│ Client  │
└────┬────┘
     │
     │ 1. CreateSession(gRPC)
     ▼
┌────────────────┐
│    Gateway     │──────► Redis: Store session metadata
└────┬───────────┘        Return: {session_id, upload_url, ws_url}
     │
     │ 2. WebSocket Connect(ws_url)
     │
┌────▼───────────┐
│   WS Hub       │──────► Register connection for session_id
└────────────────┘
     
┌─────────┐
│ Client  │
└────┬────┘
     │
     │ 3. Upload frame to MinIO (HTTP PUT)
     ▼
┌────────────────┐
│     MinIO      │──────► Store: ore-frames/{session_id}/frame-{id}.jpg
└────────────────┘        Return: storage_url
     
┌─────────┐
│ Client  │
└────┬────┘
     │
     │ 4. SubmitFrame(gRPC) with storage_url
     ▼
┌────────────────┐
│    Gateway     │
└────┬───────────┘
     │
     │ 5. Produce FrameReference to Kafka
     ▼
┌────────────────────────────────┐
│  Kafka: video-frames-input     │
│  {session_id, frame_id, url}   │
└────┬───────────────────────────┘
     │
     │ 6. Consume (partitioned by session_id)
     ▼
┌────────────────┐
│     Engine     │
│  (Instance 1)  │
└────┬───────────┘
     │
     │ 7. Fetch frame from MinIO
     ▼
┌────────────────┐
│     MinIO      │──────► Download: ore-frames/{session_id}/frame-{id}.jpg
└────────────────┘
     
┌────────────────┐
│     Engine     │
└────┬───────────┘
     │
     │ 8. Run inference (YOLOv8)
     │    - Load model (cached)
     │    - Preprocess image
     │    - Detect objects
     │    - Post-process results
     │
     │ 9. Produce DetectionResult to Kafka
     ▼
┌────────────────────────────────┐
│  Kafka: detection-results      │
│  {session_id, detections}      │
└────┬───────────────────────────┘
     │
     │ 10. Consume (all results)
     ▼
┌────────────────┐
│    Gateway     │
│  Result Router │
└────┬───────────┘
     │
     │ 11. Route by session_id → WebSocket
     ▼
┌────────────────┐
│   WS Hub       │──────► Find connection for session_id
└────┬───────────┘        Send message to WebSocket
     │
     │ 12. WebSocket message
     ▼
┌─────────┐
│ Client  │──────► Display detections in UI
└─────────┘

     │ (Optional)
     │ 13. Background cleanup
     ▼
Delete frame from MinIO after 2 hours
```

### Performance Metrics

**Target Latencies**
- Frame upload to MinIO: <100ms
- Kafka produce/consume: <10ms
- Inference (YOLOv8): 50-150ms (GPU) / 500ms-1s (CPU)
- Result delivery via WebSocket: <20ms
- **Total end-to-end**: <200ms (GPU) / <1.5s (CPU)

**Throughput Targets**
- Per-engine instance: 10-20 FPS (GPU) / 1-2 FPS (CPU)
- With 5 GPU instances: 50-100 FPS total
- Concurrent sessions: 100+ (tested)

---

## Deployment Strategy

### Docker Compose (Development & Testing)

```yaml
version: '3.8'

services:
  # Kafka Cluster (KRaft Mode)
  kafka-1:
    image: apache/kafka:3.7.0
    container_name: ore-kafka-1
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: 'broker,controller'
      KAFKA_CONTROLLER_QUORUM_VOTERS: '1@kafka-1:9093,2@kafka-2:9093,3@kafka-3:9093'
      KAFKA_LISTENERS: 'PLAINTEXT://:9092,CONTROLLER://:9093'
      KAFKA_ADVERTISED_LISTENERS: 'PLAINTEXT://kafka-1:9092'
      KAFKA_CONTROLLER_LISTENER_NAMES: 'CONTROLLER'
      KAFKA_LOG_DIRS: '/var/lib/kafka/data'
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 3
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 3
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 2
      CLUSTER_ID: 'ORE-Kafka-Cluster-2026'
    volumes:
      - kafka-1-data:/var/lib/kafka/data
    networks:
      - ore-network

  kafka-2:
    image: apache/kafka:3.7.0
    container_name: ore-kafka-2
    environment:
      KAFKA_NODE_ID: 2
      KAFKA_PROCESS_ROLES: 'broker,controller'
      KAFKA_CONTROLLER_QUORUM_VOTERS: '1@kafka-1:9093,2@kafka-2:9093,3@kafka-3:9093'
      KAFKA_LISTENERS: 'PLAINTEXT://:9092,CONTROLLER://:9093'
      KAFKA_ADVERTISED_LISTENERS: 'PLAINTEXT://kafka-2:9092'
      KAFKA_CONTROLLER_LISTENER_NAMES: 'CONTROLLER'
      KAFKA_LOG_DIRS: '/var/lib/kafka/data'
      CLUSTER_ID: 'ORE-Kafka-Cluster-2026'
    volumes:
      - kafka-2-data:/var/lib/kafka/data
    networks:
      - ore-network

  kafka-3:
    image: apache/kafka:3.7.0
    container_name: ore-kafka-3
    environment:
      KAFKA_NODE_ID: 3
      KAFKA_PROCESS_ROLES: 'broker,controller'
      KAFKA_CONTROLLER_QUORUM_VOTERS: '1@kafka-1:9093,2@kafka-2:9093,3@kafka-3:9093'
      KAFKA_LISTENERS: 'PLAINTEXT://:9092,CONTROLLER://:9093'
      KAFKA_ADVERTISED_LISTENERS: 'PLAINTEXT://kafka-3:9092'
      KAFKA_CONTROLLER_LISTENER_NAMES: 'CONTROLLER'
      KAFKA_LOG_DIRS: '/var/lib/kafka/data'
      CLUSTER_ID: 'ORE-Kafka-Cluster-2026'
    volumes:
      - kafka-3-data:/var/lib/kafka/data
    networks:
      - ore-network

  # Kafka UI (Optional for monitoring)
  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: ore-kafka-ui
    ports:
      - "8081:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: ore-cluster
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka-1:9092,kafka-2:9092,kafka-3:9092
    depends_on:
      - kafka-1
      - kafka-2
      - kafka-3
    networks:
      - ore-network

  # Redis
  redis:
    image: redis:7-alpine
    container_name: ore-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes --maxmemory 2gb --maxmemory-policy allkeys-lru
    networks:
      - ore-network

  # MinIO
  minio:
    image: minio/minio:latest
    container_name: ore-minio
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    volumes:
      - minio-data:/data
    command: server /data --console-address ":9001"
    networks:
      - ore-network

  # MinIO Client (Create buckets on startup)
  minio-init:
    image: minio/mc:latest
    container_name: ore-minio-init
    depends_on:
      - minio
    entrypoint: >
      /bin/sh -c "
      sleep 5;
      mc alias set myminio http://minio:9000 minioadmin minioadmin;
      mc mb myminio/ore-frames --ignore-existing;
      mc mb myminio/ore-models --ignore-existing;
      mc ilm add myminio/ore-frames --expiry-days 1;
      echo 'MinIO initialized successfully';
      "
    networks:
      - ore-network

  # Object Recognition Gateway (Go)
  ore-gateway:
    build:
      context: ./object-recognition-gateway
      dockerfile: Dockerfile
    container_name: ore-gateway
    ports:
      - "50051:50051"  # gRPC
      - "8080:8080"    # HTTP/WebSocket
    environment:
      KAFKA_BROKERS: "kafka-1:9092,kafka-2:9092,kafka-3:9092"
      REDIS_URL: "redis:6379"
      MINIO_ENDPOINT: "minio:9000"
      MINIO_ACCESS_KEY: "minioadmin"
      MINIO_SECRET_KEY: "minioadmin"
      MINIO_USE_SSL: "false"
      LOG_LEVEL: "info"
    depends_on:
      - kafka-1
      - kafka-2
      - kafka-3
      - redis
      - minio
    networks:
      - ore-network

  # Object Recognition Engine (Python) - CPU Version
  ore-engine-cpu:
    build:
      context: ./object-recognition-engine
      dockerfile: Dockerfile
      target: cpu
    container_name: ore-engine-cpu-1
    environment:
      KAFKA_BROKERS: "kafka-1:9092,kafka-2:9092,kafka-3:9092"
      MINIO_ENDPOINT: "minio:9000"
      MINIO_ACCESS_KEY: "minioadmin"
      MINIO_SECRET_KEY: "minioadmin"
      MINIO_USE_SSL: "false"
      DEVICE: "cpu"
      LOG_LEVEL: "info"
      CONSUMER_GROUP: "ore-engine-group"
    depends_on:
      - kafka-1
      - kafka-2
      - kafka-3
      - minio
    networks:
      - ore-network
    deploy:
      replicas: 2  # Scale horizontally

  # Object Recognition Engine (Python) - GPU Version (Commented out)
  # ore-engine-gpu:
  #   build:
  #     context: ./object-recognition-engine
  #     dockerfile: Dockerfile
  #     target: gpu
  #   container_name: ore-engine-gpu-1
  #   environment:
  #     KAFKA_BROKERS: "kafka-1:9092,kafka-2:9092,kafka-3:9092"
  #     MINIO_ENDPOINT: "minio:9000"
  #     MINIO_ACCESS_KEY: "minioadmin"
  #     MINIO_SECRET_KEY: "minioadmin"
  #     MINIO_USE_SSL: "false"
  #     DEVICE: "cuda"
  #     LOG_LEVEL: "info"
  #   runtime: nvidia  # Requires nvidia-container-toolkit
  #   deploy:
  #     resources:
  #       reservations:
  #         devices:
  #           - driver: nvidia
  #             count: 1
  #             capabilities: [gpu]
  #   depends_on:
  #     - kafka-1
  #     - minio
  #   networks:
  #     - ore-network

volumes:
  kafka-1-data:
  kafka-2-data:
  kafka-3-data:
  redis-data:
  minio-data:

networks:
  ore-network:
    driver: bridge
```

### Kubernetes Deployment (Production)

#### Namespace
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: ore-system
```

#### Gateway Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ore-gateway
  namespace: ore-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ore-gateway
  template:
    metadata:
      labels:
        app: ore-gateway
    spec:
      containers:
      - name: gateway
        image: ore-gateway:latest
        ports:
        - containerPort: 50051
          name: grpc
        - containerPort: 8080
          name: http
        env:
        - name: KAFKA_BROKERS
          valueFrom:
            configMapKeyRef:
              name: ore-config
              key: kafka.brokers
        - name: REDIS_URL
          valueFrom:
            configMapKeyRef:
              name: ore-config
              key: redis.url
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          grpc:
            port: 50051
          initialDelaySeconds: 10
          periodSeconds: 10
        readinessProbe:
          grpc:
            port: 50051
          initialDelaySeconds: 5
          periodSeconds: 5
```

#### Engine Deployment (GPU)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ore-engine-gpu
  namespace: ore-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ore-engine-gpu
  template:
    metadata:
      labels:
        app: ore-engine-gpu
    spec:
      nodeSelector:
        accelerator: nvidia-gpu  # Node with GPU
      containers:
      - name: engine
        image: ore-engine:latest-gpu
        env:
        - name: KAFKA_BROKERS
          valueFrom:
            configMapKeyRef:
              name: ore-config
              key: kafka.brokers
        - name: DEVICE
          value: "cuda"
        resources:
          requests:
            memory: "4Gi"
            cpu: "2"
            nvidia.com/gpu: 1
          limits:
            memory: "8Gi"
            cpu: "4"
            nvidia.com/gpu: 1
```

#### Horizontal Pod Autoscaler
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: ore-engine-hpa
  namespace: ore-system
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: ore-engine-gpu
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Pods
    pods:
      metric:
        name: kafka_consumer_lag
      target:
        type: AverageValue
        averageValue: "100"
```

---

## Scalability & Performance

### Horizontal Scaling

**Gateway Service (Stateless)**
- Scale based on: HTTP/gRPC request rate, WebSocket connections
- Load balancer: Kubernetes Service (ClusterIP) or Ingress
- Session affinity: Not required (Redis-backed sessions)
- **Scaling range**: 3-10 replicas

**Engine Service (Compute-Intensive)**
- Scale based on: Kafka consumer lag, CPU/GPU utilization
- Partitioning: Each instance consumes from multiple partitions
- **Scaling range**: 
  - CPU: 5-20 replicas
  - GPU: 3-10 replicas (depends on GPU availability)

### Performance Optimizations

**Gateway**
1. Connection pooling (Kafka, Redis, MinIO)
2. WebSocket message batching (combine multiple results)
3. Request rate limiting per client
4. gRPC connection multiplexing

**Engine**
1. Model preloading and caching
2. Batch inference (process multiple frames together)
3. GPU memory management (automatic batch sizing)
4. Frame prefetching from MinIO (async)
5. TensorRT optimization (for production models)

### Monitoring & Observability

**Metrics (Prometheus)**
```
# Gateway
ore_sessions_active
ore_sessions_created_total
ore_websocket_connections
ore_grpc_requests_total
ore_grpc_request_duration_seconds
ore_kafka_produce_latency_seconds

# Engine
ore_frames_processed_total
ore_inference_duration_seconds
ore_inference_batch_size
ore_model_load_duration_seconds
ore_kafka_consumer_lag
```

**Logging (Structured JSON)**
- Correlation ID across services (trace_id)
- Log level: DEBUG (dev), INFO (prod), ERROR (always)
- Log aggregation: ELK stack or Loki

**Tracing (OpenTelemetry)**
- End-to-end request tracing
- Span: gRPC call → Kafka → Inference → Kafka → WebSocket
- Tools: Jaeger or Tempo

---

## Technology Stack

### Languages & Frameworks

**Object Recognition Gateway (Go)**
- Go 1.21+
- gRPC: `google.golang.org/grpc`
- WebSocket: `github.com/gorilla/websocket`
- Kafka: `github.com/segmentio/kafka-go`
- Redis: `github.com/redis/go-redis/v9`
- MinIO: `github.com/minio/minio-go/v7`
- Protobuf: `google.golang.org/protobuf`

**Object Recognition Engine (Python)**
- Python 3.11+
- ML: `ultralytics` (YOLOv8), `torch`, `torchvision`
- Kafka: `confluent-kafka-python`
- Redis: `redis-py`
- MinIO: `minio`
- Image: `opencv-python`, `Pillow`
- Async: `asyncio`, `aiohttp`

### Infrastructure

- **Container Runtime**: Docker 24+, containerd
- **Orchestration**: Kubernetes 1.28+ / K3s
- **Message Broker**: Apache Kafka 3.7+ (KRaft mode)
- **Cache**: Redis 7+
- **Object Storage**: MinIO (S3-compatible)
- **Monitoring**: Prometheus + Grafana
- **Logging**: Fluent Bit / Loki
- **Tracing**: Jaeger / Tempo

---

## Development Phases

### Phase 1: Foundation (Week 1-2)
**Goal**: Basic infrastructure and service scaffolding

- [ ] Protobuf schema definition
- [ ] Gateway service skeleton (Go)
  - [ ] gRPC server setup
  - [ ] Session manager with Redis
  - [ ] Basic Kafka producer
- [ ] Engine service skeleton (Python)
  - [ ] Kafka consumer setup
  - [ ] Dummy inference (echo frame metadata)
- [ ] Docker Compose setup (Kafka KRaft, Redis, MinIO)
- [ ] End-to-end smoke test (manual)

**Deliverables**: Running services in Docker Compose

---

### Phase 2: Core Features (Week 3-4)
**Goal**: Implement full detection pipeline

- [ ] Gateway: MinIO integration
  - [ ] Presigned URL generation
  - [ ] Frame upload handling
- [ ] Gateway: WebSocket hub
  - [ ] Connection management
  - [ ] Result routing by session_id
- [ ] Gateway: Kafka consumer (results)
- [ ] Engine: MinIO client (fetch frames)
- [ ] Engine: YOLOv8 integration
  - [ ] Model loader
  - [ ] Inference pipeline
  - [ ] Result formatting
- [ ] Engine: Kafka producer (results)
- [ ] Integration testing

**Deliverables**: Working object detection pipeline

---

### Phase 3: Advanced Features (Week 5-6)
**Goal**: Model management and optimization

- [ ] Model registry
  - [ ] Multiple model support
  - [ ] Version management
  - [ ] Hot-swapping
- [ ] Object tracking (DeepSORT/ByteTrack)
- [ ] Batch inference optimization
- [ ] GPU memory management
- [ ] gRPC streaming API
- [ ] Performance tuning

**Deliverables**: Production-ready inference engine

---

### Phase 4: Production Readiness (Week 7-8)
**Goal**: Deployment, monitoring, and hardening

- [ ] Kubernetes manifests
  - [ ] Deployments, Services, ConfigMaps
  - [ ] HPA for auto-scaling
  - [ ] PersistentVolumes for Kafka/Redis
- [ ] Monitoring setup
  - [ ] Prometheus metrics
  - [ ] Grafana dashboards
  - [ ] Alerting rules
- [ ] Logging & tracing
- [ ] Security hardening
  - [ ] TLS for gRPC
  - [ ] Authentication/authorization
  - [ ] Network policies
- [ ] Load testing
- [ ] Documentation

**Deliverables**: Kubernetes deployment ready for production

---

## Security Considerations

### Authentication & Authorization
- **Client Auth**: JWT tokens for gRPC/WebSocket
- **Service-to-Service**: mTLS between gateway and engine
- **Kafka**: SASL/SCRAM for authentication
- **MinIO**: IAM policies for bucket access

### Data Security
- **Encryption in Transit**: TLS 1.3 for all external communication
- **Encryption at Rest**: MinIO encryption for sensitive frames
- **Data Retention**: Automatic cleanup of frames after processing
- **PII Handling**: No persistent storage of client data

### Network Security
- **Kubernetes**: NetworkPolicies to isolate services
- **Firewall**: Expose only necessary ports (gRPC, HTTP)
- **Rate Limiting**: Per-client request throttling
- **DDoS Protection**: Load balancer with rate limiting

---

## Cost Estimation

### Bare Metal Setup (Example)
- Server: 2x GPU nodes (NVIDIA RTX 4090) - ~$6,000
- CPUs: 3x Kafka brokers (16 cores) - ~$3,000
- Storage: 4TB SSD for Kafka/MinIO - ~$500
- **Total**: ~$9,500 (one-time)
- **Operational**: Power, cooling, maintenance

### Cloud Setup (AWS Example, Monthly)
- EKS Cluster: $72 (control plane)
- EC2 GPU (g5.xlarge x3): ~$2,400
- EC2 CPU (t3.large x5): ~$375
- MSK (Kafka): ~$500
- ElastiCache (Redis): ~$100
- S3 Storage: ~$50 (1TB)
- Data Transfer: ~$200
- **Total**: ~$3,700/month

### Hybrid (Recommended)
- Bare metal for GPU inference
- Cloud for Kafka, Redis, S3
- **Estimated**: ~$1,500/month + initial hardware

---

## Next Steps

1. **Review and Approve Architecture**
2. **Set up Development Environment**
3. **Begin Phase 1 Implementation**
4. **Establish CI/CD Pipeline**
5. **Define Testing Strategy**

---

## Appendix

### Glossary
- **ORE**: Object Recognition Engine
- **KRaft**: Kafka Raft metadata mode (no Zookeeper)
- **HPA**: Horizontal Pod Autoscaler
- **mTLS**: Mutual TLS authentication
- **YOLOv8**: You Only Look Once version 8 (object detection model)

### References
- Kafka KRaft Documentation: https://kafka.apache.org/documentation/#kraft
- YOLOv8: https://github.com/ultralytics/ultralytics
- MinIO: https://min.io/docs/
- gRPC Go: https://grpc.io/docs/languages/go/

---

**Document Version**: 1.0  
**Last Updated**: January 18, 2026  
**Maintained By**: Engineering Team
