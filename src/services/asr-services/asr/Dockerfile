FROM python:3.10

# Install system dependencies
RUN apt-get update && apt-get install -y \
    ffmpeg \
    libsndfile1 \
    gcc \
    g++ \
    make \
    autoconf \
    automake \
    libtool \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install RNNoise
RUN git clone https://github.com/xiph/rnnoise.git && \
    cd rnnoise && \
    ./autogen.sh && \
    ./configure && \
    make && \
    make install && \
    ldconfig && \
    gcc -o /usr/local/bin/rnnoise_demo examples/rnnoise_demo.c -lrnnoise -lm && \
    cd .. && rm -rf rnnoise

WORKDIR /app

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy configuration and application code
COPY config.yaml .
COPY asr.py .

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/')" || exit 1

# Run the application
CMD ["uvicorn", "asr:app", "--host", "0.0.0.0", "--port", "8000"]