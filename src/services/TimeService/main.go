package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"sync"
	"time"
)

// a simple http server with a 1 rest api: GET /api/v1/now
// that retun current UnixNano timestamp in json format
// example: {"utcUnixTime": 1633072800}
// use the standard library only

type TimeService struct {
	lock     sync.Mutex
	lastTime int64
}

type Config struct {
	Port int    `json:"port"`
	Host string `json:"host"`
}

func NewTimeService() *TimeService {
	return &TimeService{
		lock:     sync.Mutex{},
		lastTime: 0,
	}
}

func LoadConfig(filename string) (*Config, error) {
	file, err := os.Open(filename)
	if err != nil {
		return nil, fmt.Errorf("error opening config file: %v", err)
	}
	defer file.Close()

	var config Config
	decoder := json.NewDecoder(file)
	if err := decoder.Decode(&config); err != nil {
		return nil, fmt.Errorf("error decoding config file: %v", err)
	}

	return &config, nil
}

func (s *TimeService) GetCurrentUnixTime() int64 {
	s.lock.Lock()
	defer s.lock.Unlock()

	// ensure time is always increasing
	now := time.Now().UnixNano()
	if now <= s.lastTime {

		log.Printf("Warning, multiple response with same nanotime. now: %v - lastTime: %v. Add 1 to lastTime", now, s.lastTime)
		now = s.lastTime + 1
	}
	s.lastTime = now
	return now
}

func SetupServer() {

	timeService := NewTimeService()

	http.HandleFunc("/api/v1/now", func(writer http.ResponseWriter, reader *http.Request) {

		if reader.Method != http.MethodGet {
			http.Error(writer, "Method not allowed", http.StatusMethodNotAllowed)
			return
		}

		payload := map[string]int64{"utcUnixTime": timeService.GetCurrentUnixTime()}

		// send response as Json
		writer.Header().Set("Content-Type", "application/json")
		if err := json.NewEncoder(writer).Encode(payload); err != nil {
			http.Error(writer, err.Error(), http.StatusInternalServerError)
			log.Printf("Error encoding response in Json. %v", err)
			return
		}
	})
}

func main() {
	// Load configuration
	config, err := LoadConfig("config.json")
	if err != nil {
		log.Fatalf("Error loading config: %v", err)
	}

	SetupServer()

	address := fmt.Sprintf("%s:%d", config.Host, config.Port)
	log.Printf("Starting server on %s", address)

	if err := http.ListenAndServe(address, nil); err != nil {
		log.Fatalf("Error starting server: %v", err)
	}
}
