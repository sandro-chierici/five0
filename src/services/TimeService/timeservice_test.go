package main

import (
	"fmt"
	"sync"
	"testing"
	"time"
)

func TestGetCurrentUnixTime_Sequential(t *testing.T) {
	service := NewTimeService()

	var previousTime int64 = 0

	// Test 1000 sequential calls
	for i := 0; i < 1000; i++ {
		currentTime := service.GetCurrentUnixTime()

		if currentTime <= previousTime {
			t.Errorf("Time should be strictly increasing. Previous: %d, Current: %d", previousTime, currentTime)
		}

		previousTime = currentTime
	}
}

func TestGetCurrentUnixTime_Concurrent(t *testing.T) {
	service := NewTimeService()
	numGoroutines := 100
	callsPerGoroutine := 100

	var wg sync.WaitGroup
	results := make(chan int64, numGoroutines*callsPerGoroutine)

	// Launch multiple goroutines to call GetCurrentUnixTime concurrently
	for i := 0; i < numGoroutines; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for j := 0; j < callsPerGoroutine; j++ {
				timestamp := service.GetCurrentUnixTime()
				results <- timestamp
			}
		}()
	}

	wg.Wait()
	close(results)

	// Collect all results
	var timestamps []int64
	for timestamp := range results {
		timestamps = append(timestamps, timestamp)
	}

	// Verify all timestamps are unique and in order
	timestampMap := make(map[int64]bool)
	for _, timestamp := range timestamps {
		if timestampMap[timestamp] {
			t.Errorf("Duplicate timestamp found: %d", timestamp)
		}
		timestampMap[timestamp] = true
	}

	// Verify we got the expected number of results
	expectedCount := numGoroutines * callsPerGoroutine
	if len(timestamps) != expectedCount {
		t.Errorf("Expected %d timestamps, got %d", expectedCount, len(timestamps))
	}
}

func TestGetCurrentUnixTime_MassiveConcurrent(t *testing.T) {
	service := NewTimeService()
	numGoroutines := 1000
	callsPerGoroutine := 50

	var wg sync.WaitGroup
	results := make(chan int64, numGoroutines*callsPerGoroutine)
	errors := make(chan error, numGoroutines*callsPerGoroutine)

	start := time.Now()

	// Launch massive concurrent test
	for i := 0; i < numGoroutines; i++ {
		wg.Add(1)
		go func(goroutineID int) {
			defer wg.Done()

			var lastTime int64 = 0
			for j := 0; j < callsPerGoroutine; j++ {
				timestamp := service.GetCurrentUnixTime()

				// Each goroutine checks its own sequence
				if timestamp <= lastTime {
					errors <- fmt.Errorf("Non-monotonic time in goroutine %d: %d <= %d", goroutineID, timestamp, lastTime)
					return
				}

				lastTime = timestamp
				results <- timestamp
			}
		}(i)
	}

	wg.Wait()
	close(results)
	close(errors)

	duration := time.Since(start)

	// Check for errors
	for err := range errors {
		t.Error(err)
	}

	// Collect and verify results
	var timestamps []int64
	for timestamp := range results {
		timestamps = append(timestamps, timestamp)
	}

	expectedCount := numGoroutines * callsPerGoroutine
	if len(timestamps) != expectedCount {
		t.Errorf("Expected %d timestamps, got %d", expectedCount, len(timestamps))
	}

	// Verify all timestamps are unique
	timestampMap := make(map[int64]bool)
	duplicates := 0
	for _, timestamp := range timestamps {
		if timestampMap[timestamp] {
			duplicates++
		}
		timestampMap[timestamp] = true
	}

	if duplicates > 0 {
		t.Errorf("Found %d duplicate timestamps in massive concurrent test", duplicates)
	}

	t.Logf("Massive concurrent test completed in %v with %d goroutines, %d calls each",
		duration, numGoroutines, callsPerGoroutine)
}

func TestGetCurrentUnixTime_TimeProgression(t *testing.T) {
	service := NewTimeService()

	// Test that time progresses reasonably
	firstTime := service.GetCurrentUnixTime()
	time.Sleep(1 * time.Millisecond)
	secondTime := service.GetCurrentUnixTime()

	if secondTime <= firstTime {
		t.Errorf("Time should progress: first=%d, second=%d", firstTime, secondTime)
	}

	// The difference should be reasonable (at least 1 nanosecond, but probably much more)
	diff := secondTime - firstTime
	if diff <= 0 {
		t.Errorf("Time difference should be positive, got %d", diff)
	}
}

func TestGetCurrentUnixTime_StressTest(t *testing.T) {
	if testing.Short() {
		t.Skip("Skipping stress test in short mode")
	}

	service := NewTimeService()
	numGoroutines := 500
	duration := 5 * time.Second

	var wg sync.WaitGroup
	results := make(chan int64, 1000000) // Large buffer
	done := make(chan bool)

	// Start timer
	timer := time.NewTimer(duration)

	// Launch goroutines
	for i := 0; i < numGoroutines; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for {
				select {
				case <-done:
					return
				default:
					timestamp := service.GetCurrentUnixTime()
					select {
					case results <- timestamp:
					case <-done:
						return
					}
				}
			}
		}()
	}

	// Wait for timer or completion
	<-timer.C
	close(done)
	wg.Wait()
	close(results)

	// Analyze results
	var timestamps []int64
	for timestamp := range results {
		timestamps = append(timestamps, timestamp)
	}

	t.Logf("Stress test generated %d timestamps in %v with %d goroutines",
		len(timestamps), duration, numGoroutines)

	// Check for duplicates
	timestampMap := make(map[int64]bool)
	duplicates := 0
	for _, timestamp := range timestamps {
		if timestampMap[timestamp] {
			duplicates++
		}
		timestampMap[timestamp] = true
	}

	if duplicates > 0 {
		t.Errorf("Found %d duplicates in stress test", duplicates)
	}

	if len(timestamps) == 0 {
		t.Error("No timestamps generated in stress test")
	}
}

// Benchmark tests
func BenchmarkGetCurrentUnixTime_Sequential(b *testing.B) {
	service := NewTimeService()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		service.GetCurrentUnixTime()
	}
}

func BenchmarkGetCurrentUnixTime_Parallel(b *testing.B) {
	service := NewTimeService()

	b.ResetTimer()
	b.RunParallel(func(pb *testing.PB) {
		for pb.Next() {
			service.GetCurrentUnixTime()
		}
	})
}
