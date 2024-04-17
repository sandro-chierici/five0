package main

import (
	"flag"
	"fmt"
	"os"
	"os/signal"
	"syscall"
)

func main() {

	parseFlags()

	// handling stops
	sig := make(chan os.Signal, 10)
	signal.Notify(sig, os.Interrupt, syscall.SIGTERM)
	go func() {
		<-sig
		fmt.Println("\nReceived signal, program exit. Bye")
		os.Exit(0)
	}()

	fmt.Println("Waiting...")

	done := make(chan bool, 1)
	<-done
}

// parseFlags parses the flags passed on reexec to create the TCP/UDP/SCTP
// net.Addrs to map the host and container ports.
func parseFlags() (topic string, rabbitQueue string) {
	var (
		tp = flag.String("topic", "five0.signals", "kafka topic to write into")
		rq = flag.String("queue", "five0.signals", "rabbitmq queue to read from")
	)
	flag.Parse()
	fmt.Printf("program settings - topic: %s, queue: %s\n", *tp, *rq)
	return *tp, *rq
}
