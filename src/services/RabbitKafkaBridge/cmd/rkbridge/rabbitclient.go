package main

import (
	"fmt"
	"log"
	"net/url"

	amqp "github.com/rabbitmq/amqp091-go"
)

type RabbitClient struct {
	connection    *amqp.Connection
	channel       *amqp.Channel
	tag           string
	done          chan error
	isConnected   bool
	notifyCloseCh chan *amqp.Error
}

func (r *RabbitClient) NewConsumer(tenant string) {

	var client, err = clientConnect(tenant)
	if err != nil {
		return
	}

	client.handleReconnect()

	// if err = c.channel.ExchangeDeclare(
	// 	exchange,     // name of the exchange
	// 	exchangeType, // type
	// 	true,         // durable
	// 	false,        // delete when complete
	// 	false,        // internal
	// 	false,        // noWait
	// 	nil,          // arguments
	// ); err != nil {
	// 	return fmt.Errorf("Exchange Declare: %s", err)
	// }

	// queue, err := c.channel.QueueDeclare(
	// 	queueName, // name of the queue
	// 	true,      // durable
	// 	false,     // delete when unused
	// 	false,     // exclusive
	// 	false,     // noWait
	// 	nil,       // arguments
	// )
	// if err != nil {
	// 	return fmt.Errorf("Queue Declare: %s", err)
	// }

	// if err = c.channel.QueueBind(
	// 	queue.Name, // name of the queue
	// 	key,        // bindingKey
	// 	exchange,   // sourceExchange
	// 	false,      // noWait
	// 	nil,        // arguments
	// ); err != nil {
	// 	return fmt.Errorf("Queue Bind: %s", err)
	// }

	// c.channel.Consume(
	// 	queue.Name, // name
	// 	c.tag,      // consumerTag,
	// 	true,       // autoAck
	// 	false,      // exclusive
	// 	false,      // noLocal
	// 	false,      // noWait
	// 	nil,        // arguments
	// )
}

func clientConnect(tenant string) (*RabbitClient, error) {

	c := &RabbitClient{
		connection:    nil,
		channel:       nil,
		tag:           tenant,
		done:          make(chan error),
		isConnected:   false,
		notifyCloseCh: make(chan *amqp.Error, 1),
	}

	config := amqp.Config{Properties: amqp.NewConnectionProperties()}
	config.Properties.SetClientConnectionName(fmt.Sprintf("%s-consumer", tenant))

	// connection string
	var escapedTenant = url.QueryEscape(tenant)
	var amqpURI = fmt.Sprintf("amqp://%[1]s:%[1]s@localhost:5672/%[1]s", escapedTenant)

	log.Printf("Rabbit amqpURI %s", amqpURI)

	var err error
	c.connection, err = amqp.DialConfig(amqpURI, config)
	if err != nil {
		return nil, fmt.Errorf("error dial, %s", err)
	}

	c.channel, err = c.connection.Channel()
	if err != nil {
		return nil, fmt.Errorf("error connecting RabbitMQ, channel: %s", err)
	}

	log.Printf("Rabbit successful connected to %s", amqpURI)

	return c, nil
}

func (r *RabbitClient) handleReconnect() {

	r.connection.NotifyClose(r.notifyCloseCh)

	go func() {

	}()
}
