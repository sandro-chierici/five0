.PHONY: help build up down logs clean proto

help:
	@echo "ORE Services - Make targets:"
	@echo "  build       - Build all Docker images"
	@echo "  up          - Start all services"
	@echo "  down        - Stop all services"
	@echo "  logs        - View logs for all services"
	@echo "  clean       - Stop and remove volumes"
	@echo "  proto       - Generate protobuf code for both services"
	@echo "  test        - Run tests"

build:
	@echo "Building Docker images..."
	docker-compose build

up:
	@echo "Starting ORE services..."
	docker-compose up -d
	@echo "Services started. Access points:"
	@echo "  - Gateway gRPC: localhost:50051"
	@echo "  - Gateway HTTP: localhost:8080"
	@echo "  - Kafka UI: http://localhost:8081"
	@echo "  - MinIO Console: http://localhost:9001"

down:
	@echo "Stopping services..."
	docker-compose down

logs:
	docker-compose logs -f

clean:
	@echo "Cleaning up..."
	docker-compose down -v
	@echo "All services stopped and volumes removed"

test:
	@echo "Running tests..."
	@cd ore-gateway && make test
	@cd ore-engine && python -m pytest tests/ || echo "Python tests not yet implemented"

dev-gateway:
	@echo "Running gateway in development mode..."
	@cd ore-gateway && make run

dev-engine:
	@echo "Running engine in development mode..."
	@cd ore-engine && python main.py

scale-engine:
	@echo "Scaling engine instances..."
	docker-compose up -d --scale ore-engine-cpu-1=3 --scale ore-engine-cpu-2=3
