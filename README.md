# deepr

Ultimate decision making assistant

## Docker artifacts and registry image

The CI workflow now publishes the image to GitHub Container Registry and also produces downloadable artifacts:

- `ghcr.io/tayyebi/deepr:latest` — pushed on main/develop
- `deepr-api` — published .NET output
- `deepr-api-image` — Docker image saved as a `tar.gz` file (tagged `ghcr.io/tayyebi/deepr:latest`)
- `deepr-compose-bundle` — tarball with the ready-to-run `docker-compose.yml`

Download the artifacts from the latest successful GitHub Actions run, then:

```bash
# Load the Docker image
docker load -i deepr-api-image.tar.gz

# (Optional) refresh docker-compose.yml from the bundle (uses ghcr.io/tayyebi/deepr:latest)
tar -xzf docker-compose-bundle.tar.gz

# Start the stack (API + Postgres)
docker compose up -d

# Follow logs or stop
docker compose logs -f deepr-api
docker compose down
```

The API will be available at `http://localhost:8080` with a Postgres database (`postgres/postgres`) preconfigured via the compose file.

### Pull from registry instead

```bash
docker pull ghcr.io/tayyebi/deepr:latest
docker compose up -d
```

### Build locally instead

If you prefer to build the image yourself (matching the compose tag):

```bash
docker build -t ghcr.io/tayyebi/deepr:latest .
docker compose up -d
```
