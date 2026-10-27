#!/bin/sh
set -eu

# Run this ON THE PRODUCTION SERVER, inside the cloned repo, to deploy the
# latest main branch. Not meant to run from a local machine or in CI —
# see DEPLOY.md for the one-time server setup this assumes.

cd "$(dirname "$0")/.."

echo "==> Fetching latest main"
git fetch origin main
git reset --hard origin/main

echo "==> Building and starting containers"
docker compose -f docker-compose.prod.yml up -d --build

echo "==> Waiting for backend health check"
healthy=0
for _ in $(seq 1 30); do
  if docker compose -f docker-compose.prod.yml exec -T backend \
       curl -sf http://localhost:8080/api/health >/dev/null 2>&1; then
    healthy=1
    break
  fi
  sleep 2
done

if [ "$healthy" -eq 0 ]; then
  echo "==> Backend did not become healthy within 60s — check logs:"
  echo "    docker compose -f docker-compose.prod.yml logs backend"
  exit 1
fi

echo "==> Pruning old images"
docker image prune -f

echo "==> Deploy complete"
docker compose -f docker-compose.prod.yml ps
