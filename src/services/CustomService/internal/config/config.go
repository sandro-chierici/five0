// Package config provides configuration management for the CustomService
package config

import (
	"encoding/json"
	"fmt"
	"os"
)

// Config represents the application configuration
type Config struct {
	Server   ServerConfig   `json:"server"`
	Database DatabaseConfig `json:"database"`
	Logging  LoggingConfig  `json:"logging"`
}

// ServerConfig contains server-related configuration
type ServerConfig struct {
	Port string `json:"port"`
	Host string `json:"host"`
}

// DatabaseConfig contains database-related configuration
type DatabaseConfig struct {
	MongoDBURI     string `json:"mongodb_uri"`
	DatabaseName   string `json:"database_name"`
	CollectionName string `json:"collection_name"`
}

// LoggingConfig contains logging-related configuration
type LoggingConfig struct {
	Level string `json:"level"`
}

// Load loads configuration from a JSON file
func Load(configPath string) (*Config, error) {
	config := &Config{}
	
	configFile, err := os.Open(configPath)
	if err != nil {
		return nil, fmt.Errorf("failed to open config file: %v", err)
	}
	defer configFile.Close()

	jsonParser := json.NewDecoder(configFile)
	if err := jsonParser.Decode(config); err != nil {
		return nil, fmt.Errorf("failed to parse config file: %v", err)
	}

	// Validate required fields
	if err := config.validate(); err != nil {
		return nil, fmt.Errorf("invalid configuration: %v", err)
	}

	return config, nil
}

// validate performs basic validation on the configuration
func (c *Config) validate() error {
	if c.Server.Port == "" {
		return fmt.Errorf("server port is required")
	}
	if c.Database.MongoDBURI == "" {
		return fmt.Errorf("mongodb URI is required")
	}
	if c.Database.DatabaseName == "" {
		return fmt.Errorf("database name is required")
	}
	if c.Database.CollectionName == "" {
		return fmt.Errorf("collection name is required")
	}
	return nil
}

// GetServerAddr returns the full server address
func (c *Config) GetServerAddr() string {
	return c.Server.Host + ":" + c.Server.Port
}