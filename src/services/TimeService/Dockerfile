# Build stage
FROM golang:1.24-alpine AS builder

# Set working directory
WORKDIR /app

# Copy go mod and sum files
COPY go.mod ./

# Download dependencies
RUN go mod download

# Copy source code
COPY . .

# Build the application
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o timeservice .

# Final stage
FROM alpine:latest

# Set the environment variable for the timezone
ENV TZ=Europe/Rome

# Install ca-certificates for HTTPS requests if needed
# RUN apk --no-cache add ca-certificates

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

WORKDIR /root/

# Copy the binary from builder stage
COPY --from=builder /app/timeservice .

# Copy config file
COPY --from=builder /app/config.json .

# Change ownership to non-root user
RUN chown -R appuser:appgroup /root/

# Switch to non-root user
USER appuser

# Expose port (default from config)
EXPOSE 6080

# Run the binary
ENTRYPOINT ["./timeservice"]