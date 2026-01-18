package websocket

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strings"
	"sync"
	"time"

	"github.com/five0/ore/gateway/internal/config"
	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true // Allow all origins (configure for production)
	},
}

// Client represents a WebSocket client connection
type Client struct {
	conn      *websocket.Conn
	send      chan []byte
	sessionID string
	hub       *Hub
}

// Hub maintains active WebSocket connections
type Hub struct {
	clients    map[string]*Client // sessionID -> client
	register   chan *Client
	unregister chan *Client
	broadcast  chan *BroadcastMessage
	mu         sync.RWMutex
	config     config.WebSocketConfig
}

// BroadcastMessage contains session ID and message data
type BroadcastMessage struct {
	SessionID string
	Data      []byte
}

// NewHub creates a new WebSocket hub
func NewHub(cfg config.WebSocketConfig) *Hub {
	return &Hub{
		clients:    make(map[string]*Client),
		register:   make(chan *Client),
		unregister: make(chan *Client),
		broadcast:  make(chan *BroadcastMessage, 256),
		config:     cfg,
	}
}

// Run starts the hub's main loop
func (h *Hub) Run(ctx context.Context) {
	for {
		select {
		case <-ctx.Done():
			log.Println("WebSocket hub shutting down...")
			h.closeAllConnections()
			return

		case client := <-h.register:
			h.mu.Lock()
			h.clients[client.sessionID] = client
			h.mu.Unlock()
			log.Printf("Client registered for session: %s\n", client.sessionID)

		case client := <-h.unregister:
			h.mu.Lock()
			if _, ok := h.clients[client.sessionID]; ok {
				delete(h.clients, client.sessionID)
				close(client.send)
				log.Printf("Client unregistered for session: %s\n", client.sessionID)
			}
			h.mu.Unlock()

		case msg := <-h.broadcast:
			h.mu.RLock()
			client, ok := h.clients[msg.SessionID]
			h.mu.RUnlock()

			if ok {
				select {
				case client.send <- msg.Data:
				default:
					// Channel full, close connection
					h.unregister <- client
				}
			}
		}
	}
}

// HandleWebSocket handles WebSocket upgrade requests
func (h *Hub) HandleWebSocket(w http.ResponseWriter, r *http.Request) {
	// Extract session ID from path: /ws/{session_id}
	path := strings.TrimPrefix(r.URL.Path, "/ws/")
	sessionID := strings.TrimSuffix(path, "/")

	if sessionID == "" {
		http.Error(w, "Missing session ID", http.StatusBadRequest)
		return
	}

	// Upgrade connection
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("WebSocket upgrade error: %v\n", err)
		return
	}

	// Create client
	client := &Client{
		conn:      conn,
		send:      make(chan []byte, 256),
		sessionID: sessionID,
		hub:       h,
	}

	// Register client
	h.register <- client

	// Start read and write pumps
	go client.writePump()
	go client.readPump()
}

// BroadcastToSession sends a message to a specific session
func (h *Hub) BroadcastToSession(sessionID string, data []byte) error {
	select {
	case h.broadcast <- &BroadcastMessage{SessionID: sessionID, Data: data}:
		return nil
	case <-time.After(1 * time.Second):
		return fmt.Errorf("broadcast timeout for session %s", sessionID)
	}
}

// readPump reads messages from the WebSocket connection
func (c *Client) readPump() {
	defer func() {
		c.hub.unregister <- c
		c.conn.Close()
	}()

	c.conn.SetReadDeadline(time.Now().Add(c.hub.config.PongWait))
	c.conn.SetPongHandler(func(string) error {
		c.conn.SetReadDeadline(time.Now().Add(c.hub.config.PongWait))
		return nil
	})

	for {
		_, message, err := c.conn.ReadMessage()
		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				log.Printf("WebSocket error: %v\n", err)
			}
			break
		}

		// Handle client messages (e.g., ping, config updates)
		c.handleMessage(message)
	}
}

// writePump writes messages to the WebSocket connection
func (c *Client) writePump() {
	ticker := time.NewTicker(c.hub.config.PingPeriod)
	defer func() {
		ticker.Stop()
		c.conn.Close()
	}()

	for {
		select {
		case message, ok := <-c.send:
			c.conn.SetWriteDeadline(time.Now().Add(c.hub.config.WriteWait))
			if !ok {
				c.conn.WriteMessage(websocket.CloseMessage, []byte{})
				return
			}

			if err := c.conn.WriteMessage(websocket.TextMessage, message); err != nil {
				return
			}

		case <-ticker.C:
			c.conn.SetWriteDeadline(time.Now().Add(c.hub.config.WriteWait))
			if err := c.conn.WriteMessage(websocket.PingMessage, nil); err != nil {
				return
			}
		}
	}
}

// handleMessage processes incoming client messages
func (c *Client) handleMessage(data []byte) {
	var msg map[string]interface{}
	if err := json.Unmarshal(data, &msg); err != nil {
		log.Printf("Error unmarshaling client message: %v\n", err)
		return
	}

	// Handle different message types
	msgType, ok := msg["type"].(string)
	if !ok {
		return
	}

	switch msgType {
	case "ping":
		// Respond with pong
		c.send <- []byte(`{"type":"pong"}`)
	default:
		log.Printf("Unknown message type: %s\n", msgType)
	}
}

// closeAllConnections closes all active WebSocket connections
func (h *Hub) closeAllConnections() {
	h.mu.Lock()
	defer h.mu.Unlock()

	for _, client := range h.clients {
		close(client.send)
		client.conn.Close()
	}
}
