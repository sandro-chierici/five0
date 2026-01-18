# Object Recognition Engine - Python Service

ML inference service for real-time object detection.

## Features

- YOLOv8-based object detection
- Kafka integration for streaming processing
- MinIO support for frame storage
- CPU and GPU inference support
- Dynamic model loading
- Configurable detection parameters

## Installation

### With pip

```bash
# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

### With Docker

```bash
# CPU version
docker build --target cpu -t ore-engine:cpu .

# GPU version (requires nvidia-docker)
docker build --target gpu -t ore-engine:gpu .
```

## Usage

### Standalone

```bash
# Edit config.yaml with your settings
python main.py
```

### Docker

```bash
# CPU
docker run -it --rm \
  -e KAFKA_BROKERS=kafka:9092 \
  -e MINIO_ENDPOINT=minio:9000 \
  -e DEVICE=cpu \
  ore-engine:cpu

# GPU
docker run -it --rm --gpus all \
  -e KAFKA_BROKERS=kafka:9092 \
  -e MINIO_ENDPOINT=minio:9000 \
  -e DEVICE=cuda \
  ore-engine:gpu
```

## Configuration

Edit `config.yaml` or use environment variables:

```yaml
inference:
  device: cuda  # cpu, cuda, or mps
  batch_size: 1
  confidence_threshold: 0.5
  iou_threshold: 0.45
  max_detections: 100
  default_model_id: yolov8n
```

### Environment Variables

- `KAFKA_BROKERS` - Kafka broker addresses
- `MINIO_ENDPOINT` - MinIO server endpoint
- `DEVICE` - Inference device (cpu/cuda)
- `LOG_LEVEL` - Logging level (DEBUG/INFO/WARNING/ERROR)

## Models

### Supported Models

- YOLOv8n (Nano) - Fast, lower accuracy
- YOLOv8s (Small) - Balanced
- YOLOv8m (Medium) - More accurate
- YOLOv8l (Large) - High accuracy
- YOLOv8x (XLarge) - Highest accuracy

### Custom Models

Place model weights in MinIO `ore-models` bucket or locally and configure in `config.yaml`.

## Performance

### CPU Performance

- YOLOv8n: ~500ms per frame
- YOLOv8s: ~800ms per frame

### GPU Performance (NVIDIA RTX 4090)

- YOLOv8n: ~30-50ms per frame
- YOLOv8s: ~50-80ms per frame

## Development

### Running Tests

```bash
pytest tests/
```

### Code Style

```bash
# Format code
black src/

# Lint
pylint src/
```

## Architecture

```
Kafka Consumer → Frame Fetcher → Inference Engine → Result Publisher
                      ↓
                   MinIO
```

## Troubleshooting

### CUDA Out of Memory

- Reduce batch size
- Use smaller model (yolov8n instead of yolov8l)
- Reduce input image resolution

### Slow Inference

- Use GPU if available
- Enable TensorRT optimization
- Reduce model size
- Lower confidence threshold

### Model Loading Errors

- Verify model file exists
- Check model format compatibility
- Ensure sufficient disk space

## License

See main repository LICENSE.
