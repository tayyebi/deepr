# Deepr CI/CD and Deployment Guide

## Overview

This project includes automated CI/CD pipeline using GitHub Actions that builds, tests, and deploys the application to a remote server via SSH.

## Prerequisites

### GitHub Secrets Configuration

You need to configure the following secrets in your GitHub repository settings (Settings → Secrets and variables → Actions):

1. **SSH_PRIVATE_KEY**: Your SSH private key for authentication
   ```bash
   # Generate SSH key pair if you don't have one
   ssh-keygen -t ed25519 -C "deploy@deepr"
   # Copy the private key content
   cat ~/.ssh/id_ed25519
   ```

2. **REMOTE_USER**: SSH username for the remote server
   ```
   Example: ubuntu, root, deploy
   ```

3. **REMOTE_HOST**: Hostname or IP address of the remote server
   ```
   Example: 192.168.1.100 or server.example.com
   ```

4. **REMOTE_PASSWORD** (Optional): Password for sudo operations if needed
   ```
   Only required if your deployment needs password authentication
   ```

### Remote Server Requirements

- SSH access enabled
- Port 22 open for SSH
- Port 8080 open for the application (or configure as needed)
- Sufficient disk space (recommended: 10GB+)

## CI/CD Pipeline

### Workflow Triggers

The pipeline runs on:
- **Push** to `main` or `develop` branches
- **Pull Request** to `main` or `develop` branches
- **Manual trigger** via GitHub Actions UI

### Pipeline Stages

#### 1. Build and Test
- Checkout code
- Setup .NET 10
- Restore dependencies
- Build solution
- Run tests with code coverage
- Publish artifacts

#### 2. Build Docker Image
- Build multi-stage Docker image
- Push to GitHub Container Registry (ghcr.io)
- Tag with branch name and commit SHA

#### 3. Deploy to Remote Server
- SSH to remote server
- Install Docker if not present
- Start Docker service if not running
- Pull latest Docker image
- Stop existing container
- Start new container
- Clean up old images

## Local Development

### Using Docker Compose

```bash
# Build and run all services
docker-compose up -d

# View logs
docker-compose logs -f deepr-api

# Stop services
docker-compose down

# Rebuild after code changes
docker-compose up -d --build
```

### Manual Docker Build

```bash
# Build image
docker build -t deepr-api:local .

# Run container
docker run -d \
  --name deepr-api \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deepr-api:local

# View logs
docker logs -f deepr-api

# Stop and remove
docker stop deepr-api && docker rm deepr-api
```

## Deployment

### Automatic Deployment

Deployment happens automatically when you push to `main` or `develop` branch:

```bash
git push origin main
```

### Manual Deployment

Trigger deployment manually from GitHub Actions UI:
1. Go to Actions tab
2. Select "CI/CD - Build and Deploy" workflow
3. Click "Run workflow"
4. Select branch and confirm

### Verify Deployment

After deployment, verify the application is running:

```bash
# SSH to remote server
ssh user@remote-host

# Check container status
docker ps | grep deepr-api

# View container logs
docker logs deepr-api

# Check application health
curl http://localhost:8080/health
```

## Troubleshooting

### SSH Connection Issues

```bash
# Test SSH connection
ssh -i ~/.ssh/deploy_key user@remote-host

# Check SSH key permissions
chmod 600 ~/.ssh/deploy_key

# Add host to known_hosts
ssh-keyscan -H remote-host >> ~/.ssh/known_hosts
```

### Docker Issues on Remote Server

```bash
# Check Docker status
sudo systemctl status docker

# Start Docker service
sudo systemctl start docker

# View Docker logs
sudo journalctl -u docker -f

# Clean up Docker resources
docker system prune -af
```

### Application Issues

```bash
# View application logs
docker logs deepr-api --tail 100 -f

# Check container health
docker inspect deepr-api | grep -A 10 Health

# Restart container
docker restart deepr-api
```

## Security Best Practices

1. **SSH Keys**: Use Ed25519 keys for better security
2. **Secrets**: Never commit secrets to the repository
3. **Non-root User**: Docker container runs as non-root user
4. **Network**: Configure firewall rules appropriately
5. **HTTPS**: Use reverse proxy (nginx/traefik) with SSL certificates
6. **Updates**: Keep Docker and base images updated

## Monitoring

### Container Health

The application includes a health check endpoint:
```bash
curl http://localhost:8080/health
```

### Docker Stats

Monitor resource usage:
```bash
docker stats deepr-api
```

### Logs

Centralized logging:
```bash
# Stream logs
docker logs deepr-api -f

# Export logs
docker logs deepr-api > app.log 2>&1
```

## Scaling

### Horizontal Scaling

Use Docker Swarm or Kubernetes for multi-instance deployment.

### Reverse Proxy

Example nginx configuration:
```nginx
upstream deepr {
    server localhost:8080;
}

server {
    listen 80;
    server_name deepr.example.com;

    location / {
        proxy_pass http://deepr;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## Rollback

If deployment fails or issues occur:

```bash
# SSH to remote server
ssh user@remote-host

# List available images
docker images | grep deepr

# Run previous version
docker run -d \
  --name deepr-api \
  -p 8080:8080 \
  ghcr.io/owner/repo:previous-tag
```

## Support

For issues related to:
- **CI/CD Pipeline**: Check GitHub Actions logs
- **Docker Build**: Review Dockerfile and build logs
- **Deployment**: Check remote server logs
- **Application**: Review application logs in container
