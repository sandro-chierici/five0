# Object Recognition Engine (ORE) Services

A microservices-based real-time object detection system with Kafka, Redis, and MinIO.

## Services

### Gateway Service (Go)
- **Port**: 50051 (gRPC), 8080 (HTTP/WebSocket)
- **Purpose**: Session management, client communication, WebSocket hub
- **Location**: `object-recognition-gateway/`

### Engine Service (Python)
- **Purpose**: ML inference using YOLOv8
- **Location**: `object-recognition-engine/`
- **Variants**: CPU and GPU versions

### Infrastructure
- **Kafka**: 3-broker cluster (KRaft mode, no Zookeeper)
- **Redis**: Session cache
- **MinIO**: Object storage for frames
- **Kafka UI**: Monitoring dashboard (port 8081)

## Quick Start

### Prerequisites
- Docker & Docker Compose
- Go 1.21+ (for local development)
- Python 3.11+ (for local development)
- protoc compiler (for regenerating protobufs)

### Run with Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Access Services

- **Gateway gRPC**: `localhost:50051`
- **Gateway HTTP**: `http://localhost:8080`
- **Kafka UI**: `http://localhost:8081`
- **MinIO Console**: `http://localhost:9001` (minioadmin/minioadmin)
- **Redis**: `localhost:6379`

## Development

### Gateway Service (Go)

```bash
cd object-recognition-gateway

# Install dependencies
go mod download

# Generate protobuf code
make proto

# Build
make build

# Run locally
make run

# Run tests
make test
```

### Engine Service (Python)

```bash
cd object-recognition-engine

# Create virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Run locally
python main.py
```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed system design, data flows, and deployment strategies.

### Key Components

```
Client → Gateway (gRPC) → Kafka → Engine → Kafka → Gateway → Client (WebSocket)
                ↓                    ↓
              Redis               MinIO
```

## Testing

### Create a Session

```bash
# Using grpcurl
grpcurl -plaintext -d '{"client_type":"web","config":{"model_id":"yolov8n","confidence_threshold":0.5}}' \
  localhost:50051 recognition.v1.RecognitionGateway/CreateSession
```

### Submit a Frame

```bash
# Upload image to MinIO first, then:
grpcurl -plaintext -d '{"session_id":"your-session-id","frame_id":1,"storage_url":"minio://ore-frames/session/frame.jpg"}' \
  localhost:50051 recognition.v1.RecognitionGateway/SubmitFrame
```

### Connect WebSocket

```javascript
const ws = new WebSocket('ws://localhost:8080/ws/your-session-id');
ws.onmessage = (event) => {
  const result = JSON.parse(event.data);
  console.log('Detections:', result.detections);
};
```

## Configuration

### Gateway
Edit `object-recognition-gateway/config.yaml` or use environment variables:
- `KAFKA_BROKERS`
- `REDIS_URL`
- `MINIO_ENDPOINT`
- `LOG_LEVEL`

### Engine
Edit `object-recognition-engine/config.yaml` or use environment variables:
- `KAFKA_BROKERS`
- `MINIO_ENDPOINT`
- `DEVICE` (cpu/cuda)
- `LOG_LEVEL`

## Scaling

### Horizontal Scaling

```bash
# Scale engine instances
docker-compose up -d --scale ore-engine-cpu-1=5

# Or in Kubernetes
kubectl scale deployment ore-engine-cpu --replicas=5
```

### Performance Tuning

- Adjust Kafka partitions for better load distribution
- Increase engine replicas based on throughput needs
- Use GPU instances for faster inference
- Tune batch sizes and confidence thresholds

## Monitoring

- **Kafka UI**: View topics, messages, consumer lag
- **Gateway logs**: Session activity, frame submissions
- **Engine logs**: Inference performance, detection results

## Troubleshooting

### Kafka Connection Issues
```bash
# Check Kafka health
docker exec ore-kafka-1 kafka-broker-api-versions.sh --bootstrap-server localhost:9092

# View Kafka logs
docker logs ore-kafka-1
```

### MinIO Access Issues
```bash
# Check MinIO status
docker exec ore-minio mc admin info myminio

# List buckets
docker exec ore-minio-init mc ls myminio
```

### Engine Not Processing
```bash
# Check engine logs
docker logs ore-engine-cpu-1

# Verify Kafka consumer lag
# Access Kafka UI at localhost:8081
```

## Production Deployment

For Kubernetes deployment:
1. Review [ARCHITECTURE.md](ARCHITECTURE.md) for K8s manifests
2. Configure persistent volumes for Kafka and Redis
3. Set up ingress for gRPC and WebSocket
4. Configure horizontal pod autoscaling
5. Enable monitoring with Prometheus/Grafana

## License

See LICENSE file in repository root.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Support

For issues and questions, please open a GitHub issue in the main repository.
