# deepr

Ultimate decision making assistant

## Docker artifacts

The CI workflow now produces downloadable artifacts instead of pushing images to a remote registry:

- `deepr-api` — published .NET output
- `deepr-api-image` — Docker image saved as a `tar.gz` file (tagged `deepr-api:local`)
- `deepr-compose-bundle` — tarball with the ready-to-run `docker-compose.yml`

Download the artifacts from the latest successful GitHub Actions run, then:

```bash
# Load the Docker image
docker load -i deepr-api-image.tar.gz

# (Optional) refresh docker-compose.yml from the bundle
tar -xzf docker-compose-bundle.tar.gz

# Start the stack (API + Postgres)
docker compose up -d

# Follow logs or stop
docker compose logs -f deepr-api
docker compose down
```

The API will be available at `http://localhost:8080` with a Postgres database (`postgres/postgres`) preconfigured via the compose file.

### Build locally instead

If you prefer to build the image yourself:

```bash
docker build -t deepr-api:local .
docker compose up -d
```
