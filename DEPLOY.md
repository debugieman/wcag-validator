# Deploying to production

No CI/CD pipeline runs this automatically — deploy manually via `scripts/deploy.sh`,
run on the server itself over SSH.

## One-time server setup

1. Provision a VPS (Ubuntu 22.04/24.04 LTS recommended), point `wcag-analyzer.com`'s
   DNS A record (and `www.wcag-analyzer.com`, if used) at the server's IP.
2. Install Docker + Docker Compose plugin:
   ```sh
   curl -fsSL https://get.docker.com | sh
   ```
3. Clone the repo:
   ```sh
   git clone https://github.com/debugieman/wcag-validator.git /opt/wcag-analyzer
   cd /opt/wcag-analyzer
   ```
4. Create `.env` from the template and fill in real production values
   (Stripe live keys, Resend API key, `App__BaseUrl=https://wcag-analyzer.com`,
   `App__CorsOrigins=https://wcag-analyzer.com,https://www.wcag-analyzer.com`):
   ```sh
   cp .env.example .env
   nano .env
   ```
5. First deploy:
   ```sh
   ./scripts/deploy.sh
   ```
   This builds the backend, frontend, and Caddy images, starts everything, and Caddy
   will automatically request a Let's Encrypt certificate for the domain on first
   request — no manual certbot step needed. DNS must already be pointing at the
   server before this step, or the certificate request will fail.

## Repeatable deploy (every release)

SSH into the server and run:

```sh
cd /opt/wcag-analyzer
./scripts/deploy.sh
```

This fetches `origin/main`, rebuilds changed images, restarts containers, waits for
the backend health check, and prunes old images. It's safe to re-run — `git reset
--hard origin/main` means any local edits on the server are discarded, so never edit
files directly on the server (except `.env`, which is git-ignored and untouched by
this reset).

## Checking on things

```sh
# Logs
docker compose -f docker-compose.prod.yml logs -f backend
docker compose -f docker-compose.prod.yml logs -f caddy

# Status
docker compose -f docker-compose.prod.yml ps

# Latest backup
docker compose -f docker-compose.prod.yml exec backup ls -la /backups
```

## Rolling back

```sh
cd /opt/wcag-analyzer
git fetch origin
git reset --hard <previous-commit-or-tag>
docker compose -f docker-compose.prod.yml up -d --build
```
