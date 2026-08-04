.PHONY: setup dev build start docker up down logs seed test-build

setup:
	bash scripts/setup.sh

dev:
	bash scripts/start.sh dev

build:
	bash scripts/start.sh build

start:
	bash scripts/start.sh start

docker up:
	bash scripts/start.sh docker

down:
	bash scripts/start.sh down

logs:
	bash scripts/start.sh logs

seed:
	npm run seed

test-build:
	npm run build
