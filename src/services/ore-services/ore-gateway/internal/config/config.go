package config

import (
	"os"
	"strconv"
	"strings"
	"time"

	"gopkg.in/yaml.v3"
)

type Config struct {
	Server    ServerConfig    `yaml:"server"`
	Kafka     KafkaConfig     `yaml:"kafka"`
	Redis     RedisConfig     `yaml:"redis"`
	MinIO     MinIOConfig     `yaml:"minio"`
	WebSocket WebSocketConfig `yaml:"websocket"`
	Logging   LoggingConfig   `yaml:"logging"`
}

type ServerConfig struct {
	HTTPPort        int           `yaml:"http_port"`
	ShutdownTimeout time.Duration `yaml:"shutdown_timeout"`
}

type KafkaConfig struct {
	Brokers       []string      `yaml:"brokers"`
	Topics        TopicsConfig  `yaml:"topics"`
	ConsumerGroup string        `yaml:"consumer_group"`
	BatchSize     int           `yaml:"batch_size"`
	BatchTimeout  time.Duration `yaml:"batch_timeout"`
}

type TopicsConfig struct {
	FrameInput           string `yaml:"frame_input"`
	DetectionResults     string `yaml:"detection_results"`
	SessionControl       string `yaml:"session_control"`
	ModelControl         string `yaml:"model_control"`
	ModelControlResponse string `yaml:"model_control_response"`
}

type RedisConfig struct {
	URL        string `yaml:"url"`
	Password   string `yaml:"password"`
	DB         int    `yaml:"db"`
	PoolSize   int    `yaml:"pool_size"`
	SessionTTL int    `yaml:"session_ttl"` // seconds
}

type MinIOConfig struct {
	Endpoint           string `yaml:"endpoint"`
	AccessKey          string `yaml:"access_key"`
	SecretKey          string `yaml:"secret_key"`
	UseSSL             bool   `yaml:"use_ssl"`
	BucketFrames       string `yaml:"bucket_frames"`
	BucketModels       string `yaml:"bucket_models"`
	PresignedURLExpiry int    `yaml:"presigned_url_expiry"` // seconds
}

type WebSocketConfig struct {
	ReadBufferSize  int           `yaml:"read_buffer_size"`
	WriteBufferSize int           `yaml:"write_buffer_size"`
	PingPeriod      time.Duration `yaml:"ping_period"`
	PongWait        time.Duration `yaml:"pong_wait"`
	WriteWait       time.Duration `yaml:"write_wait"`
	MaxMessageSize  int64         `yaml:"max_message_size"`
}

type LoggingConfig struct {
	Level  string `yaml:"level"`
	Format string `yaml:"format"`
}

// Load reads configuration from a YAML file
func Load(filename string) (*Config, error) {
	data, err := os.ReadFile(filename)
	if err != nil {
		return nil, err
	}

	var cfg Config
	if err := yaml.Unmarshal(data, &cfg); err != nil {
		return nil, err
	}

	return &cfg, nil
}

// OverrideFromEnv overrides configuration with environment variables
func (c *Config) OverrideFromEnv() {
	// Kafka
	if brokers := os.Getenv("KAFKA_BROKERS"); brokers != "" {
		c.Kafka.Brokers = strings.Split(brokers, ",")
	}

	// Redis
	if url := os.Getenv("REDIS_URL"); url != "" {
		c.Redis.URL = url
	}
	if pwd := os.Getenv("REDIS_PASSWORD"); pwd != "" {
		c.Redis.Password = pwd
	}

	// MinIO
	if endpoint := os.Getenv("MINIO_ENDPOINT"); endpoint != "" {
		c.MinIO.Endpoint = endpoint
	}
	if accessKey := os.Getenv("MINIO_ACCESS_KEY"); accessKey != "" {
		c.MinIO.AccessKey = accessKey
	}
	if secretKey := os.Getenv("MINIO_SECRET_KEY"); secretKey != "" {
		c.MinIO.SecretKey = secretKey
	}
	if useSSL := os.Getenv("MINIO_USE_SSL"); useSSL != "" {
		c.MinIO.UseSSL = useSSL == "true"
	}

	// Logging
	if level := os.Getenv("LOG_LEVEL"); level != "" {
		c.Logging.Level = level
	}

	// Ports
	if port := os.Getenv("HTTP_PORT"); port != "" {
		if p, err := strconv.Atoi(port); err == nil {
			c.Server.HTTPPort = p
		}
	}
}
