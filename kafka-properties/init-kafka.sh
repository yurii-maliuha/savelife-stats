#!/bin/bash

kafka-topics --create --topic transactions-raw --partitions 3 --replication-factor 1 --bootstrap-server 127.0.0.1:9092
curl -X POST http://localhost:8083/connectors -H "Content-Type: application/json" -d @props/file-stream-standalone.json