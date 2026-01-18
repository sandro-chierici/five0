# Base stage - common dependencies
FROM python:3.11-slim AS base

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender-dev \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements
COPY requirements.txt .

# ============================================================================
# CPU Stage
# ============================================================================
FROM base AS cpu

# Install CPU-only dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . .

# Generate protobuf files
RUN mkdir -p proto && \
    python -m grpc_tools.protoc -I../proto --python_out=./proto --pyi_out=./proto ../proto/recognition.proto || true

CMD ["python", "main.py"]

# ============================================================================
# GPU Stage
# ============================================================================
FROM nvidia/cuda:12.1.0-runtime-ubuntu22.04 AS gpu

WORKDIR /app

# Install Python and system dependencies
RUN apt-get update && apt-get install -y \
    python3.11 \
    python3-pip \
    libgl1-mesa-glx \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender-dev \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Create symbolic link for python
RUN ln -s /usr/bin/python3.11 /usr/bin/python

# Copy requirements and install
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . .

# Generate protobuf files
RUN mkdir -p proto && \
    python -m grpc_tools.protoc -I../proto --python_out=./proto --pyi_out=./proto ../proto/recognition.proto || true

CMD ["python", "main.py"]
