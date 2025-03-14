#!/bin/bash

# run below command to init kafka connect
# docker exec -it $(docker ps -qf "name=kafka-cluster") bash -c "./properties/init-kafka.sh"

kafka-topics --create --topic transactions-raw --partitions 3 --replication-factor 1 --zookeeper 127.0.0.1:2181
cd properties
connect-standalone worker.properties file-stream-standalone.properties

# when connectors are initialized create ES sink
# curl -X POST http://localhost:8083/connectors -H "Content-Type: application/json" -d @properties/elasticsearch-sink.json

# to test kafka topic
# kafka-console-consumer --topic transactions-raw --bootstrap-server 127.0.0.1:9092
# kafka-console-consumer --topic transactions --bootstrap-server 127.0.0.1:9092