package main

import (
	"flag"
	"fmt"
	"os"
	"os/signal"
	"syscall"
)

// Program management
type PM struct {
	done chan bool
	sig  chan os.Signal
	waitingGroup 
}

func NewPM() *PM {

	var pm = &PM{
		done: make(chan bool, 1),
		sig:  make(chan os.Signal, 10),
		wai
	}

	signal.Notify(pm.sig, os.Interrupt, syscall.SIGTERM)

	go func() {
		// waitin for kill signal
		<-pm.sig
		// signal all to close 
		pm.done <- true

	}()

	return pm
}

func main() {

	parseFlags()

	var pm = NewPM()

	fmt.Println("Starting")

	// start connections
	go connectRabbit()

	<-pm.done
}

// parseFlags parses the flags passed on reexec to create the TCP/UDP/SCTP
// net.Addrs to map the host and container ports.
func parseFlags() (tenant string) {

	var tenantId = *flag.String("tenant", "five0_test", "rabbitmq tenant name")

	flag.Parse()
	fmt.Printf("program settings - rabbitmq tenant name: %s\n", tenantId)
	return tenantId
}

func connectRabbit() {

}
