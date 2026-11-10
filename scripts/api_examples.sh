#!/bin/bash
BASE=http://localhost
curl -s $BASE/workouts
curl -s -X POST $BASE/workouts -H "Content-Type: application/json; charset=utf-8" --data-binary "@api/body.json"
curl -s -X PUT $BASE/workouts/3 -H "Content-Type: application/json; charset=utf-8" --data-binary "@api/body.json"
curl -s -X DELETE $BASE/workouts/3
curl -s $BASE/workouts