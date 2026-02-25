# deepr — Ultimate Decision Making Assistant

**deepr** is a structured group decision-making API that orchestrates multi-agent councils using proven decision methods (Delphi, Brainstorming, Consensus Building) and analytical tools (SWOT, Weighted Scoring). It supports both AI agents (via [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)) and human participants.

## Screenshots

### Web Client — Dashboard

![Dashboard](https://github.com/user-attachments/assets/41b17dce-c0b9-4f3f-84c2-1ca1ff7c172b)

### Web Client — Create Issue

![Create Issue Form](https://github.com/user-attachments/assets/3a0d0ab1-2dc2-437c-ad43-cf9d2df1a083)

### Web Client — Issue Detail (filled form)

![Issue Detail](https://github.com/user-attachments/assets/38c8d66a-a08a-4907-a594-ce9db02ac4f8)

### Web Client — Council with Members & Active Session

![Council Session](https://github.com/user-attachments/assets/ce1af6a1-a26d-4b57-9f22-0315920d329c)

### Web Client — Round Results

![Round Results](https://github.com/user-attachments/assets/d4c9b006-0b2b-4584-89e3-44b273b39895)

### Swagger UI — REST API

![Swagger UI Overview](https://github.com/user-attachments/assets/00517cc5-10f1-4db9-885a-da6e6a3a1ce3)

---

## Architecture

```
Deepr.Domain          → Entities, Value Objects, Enums (DDD)
Deepr.Application     → CQRS (MediatR), Interfaces, DTOs
Deepr.Infrastructure  → EF Core / PostgreSQL, Decision Methods,
                        Tool Adapters, Agent Drivers, Orchestrator
Deepr.API             → ASP.NET Core REST API + Swagger UI
Deepr.Web             → Blazor Server client app
```

### Decision Methods
| Method | Description | Rounds |
|---|---|---|
| `Brainstorming` (2) | Free-form idea collection | 1 |
| `Delphi` (0) | Anonymous iterative expert consensus | 3 |
| `ConsensusBuilding` (4) | Structured agreement tracking | 2 |

### Analytical Tools
| Tool | Description |
|---|---|
| `SWOT` (0) | Strengths, Weaknesses, Opportunities, Threats |
| `WeightedScoring` (2) | Multi-criteria option scoring |

### Agent Roles
| Role | Value | Description |
|---|---|---|
| `Chairman` | 0 | Facilitates and summarises |
| `Expert` | 1 | Domain expert analysis |
| `Critic` | 2 | Challenges assumptions |
| `Observer` | 3 | Silent observer |

---

## Quick Start

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose

### Run with Docker Compose

```bash
docker compose up
```

| Service | URL | Description |
|---|---|---|
| Web client | `http://localhost:8081` | Blazor Server UI |
| REST API | `http://localhost:8080` | Swagger UI at root |
| PostgreSQL | `localhost:5432` | Database |

### Run Locally (development)

```bash
# Start PostgreSQL
docker run -d -e POSTGRES_DB=deepr -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine

# Terminal 1 — API (http://localhost:5011)
cd src/Deepr.API
dotnet run

# Terminal 2 — Web client (http://localhost:5012)
cd src/Deepr.Web
dotnet run
```

The database is migrated automatically on API startup.

---

## User Manual

### The Decision-Making Workflow

```
Issue → Council → Session → Rounds → Finalize
```

1. **Create an Issue** — define the problem to be decided
2. **Create a Council** — choose a decision method and analytical tool
3. **Add Members** — assign AI or human agents to the council
4. **Start a Session** — initialise the session with context
5. **Execute Rounds** — agents deliberate and contribute
6. **Finalize** — aggregate results and get the final outcome

---

### Step 1 — Create an Issue

```http
POST /api/issues
Content-Type: application/json

{
  "title": "Should we adopt microservices architecture?",
  "contextVector": "Our monolithic app is becoming hard to scale. We need to decide whether to refactor into microservices or keep the monolith.",
  "ownerId": "00000000-0000-0000-0000-000000000001"
}
```

**Response:**
```json
{
  "id": "019c968d-c454-73ac-a7dc-2299f8b4e887",
  "title": "Should we adopt microservices architecture?",
  "contextVector": "Our monolithic app is becoming hard to scale...",
  "ownerId": "00000000-0000-0000-0000-000000000001",
  "isArchived": false,
  "createdAt": "2026-02-25T20:46:39.246Z"
}
```

---

### Step 2 — Create a Council

Choose your decision method and analytical tool:

```http
POST /api/councils
Content-Type: application/json

{
  "issueId": "019c968d-c454-73ac-a7dc-2299f8b4e887",
  "selectedMethod": 2,
  "selectedTool": 0
}
```

> `selectedMethod` values: `0`=Delphi, `2`=Brainstorming, `4`=ConsensusBuilding  
> `selectedTool` values: `0`=SWOT, `2`=WeightedScoring

---

### Step 3 — Add Members

```http
POST /api/councils/{councilId}/members
Content-Type: application/json

{
  "agentId": "00000000-0000-0000-0000-000000000010",
  "name": "Dr. Sarah Chen",
  "role": 0,
  "isAi": true,
  "systemPromptOverride": null
}
```

> `role` values: `0`=Chairman, `1`=Expert, `2`=Critic, `3`=Observer

Add as many members as needed. AI agents use the `EchoAgentDriver` by default (returns role-specific placeholder responses). To use real AI, configure an OpenAI key and the `SemanticKernelAgentDriver` will be used automatically.

---

### Step 4 — Start a Session

```http
POST /api/sessions/start
Content-Type: application/json

{
  "councilId": "019c968d-dcb6-7d70-a07c-69c4b6e633f4"
}
```

**Response:**
```json
{
  "id": "019c968e-1107-7343-ba53-536c8cd27666",
  "councilId": "019c968d-dcb6-7d70-a07c-69c4b6e633f4",
  "status": 0,
  "currentRoundNumber": 0,
  "statePayload": "{\"topic\":\"Should we adopt microservices architecture?\",\"roundsCompleted\":0}"
}
```

> `status` values: `0`=Active, `1`=Paused, `2`=Completed, `3`=Failed

---

### Step 5 — Execute a Round

```http
POST /api/sessions/{sessionId}/execute-round
```

This endpoint:
1. Gets the next prompt from the decision method
2. Sends it to every council member via the agent driver
3. Parses each response through the tool adapter
4. Aggregates contributions into a round summary

**Response:**
```json
{
  "id": "019c968e-25a5-7e43-ba83-8815819bfd83",
  "sessionId": "019c968e-1107-7343-ba53-536c8cd27666",
  "roundNumber": 1,
  "instructions": "Please brainstorm as many ideas as possible about: Should we adopt microservices architecture?...",
  "summary": "Round 1 Ideas:\n- [Dr. Sarah Chen - Chairman] ...\n- [Alex Kumar - Expert] ...\n- [Maria Torres - Critic] ...",
  "contributions": [
    {
      "id": "019c968e-25b2-7d9e-b94e-bf06da13e6aa",
      "agentId": "00000000-0000-0000-0000-000000000010",
      "rawContent": "[Dr. Sarah Chen - Chairman] I acknowledge the prompt...",
      "structuredData": "{\"strengths\":[],\"weaknesses\":[],\"opportunities\":[],\"threats\":[]}"
    }
  ]
}
```

Call this endpoint once per round until the session is complete (`status`=2). Each decision method defines how many rounds are needed.

---

### Step 6 — Finalize the Session

```http
POST /api/sessions/{sessionId}/finalize
```

Returns the aggregated final result text from all rounds.

---

### Check Session Status

```http
GET /api/sessions/{sessionId}
GET /api/issues/{issueId}
GET /api/councils/{councilId}
```

---

## AI Integration (OpenAI / Semantic Kernel)

By default, deepr uses the `EchoAgentDriver` which returns deterministic placeholder responses. To enable real AI agents:

1. Add your OpenAI key to `appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "ModelId": "gpt-4o"
  }
}
```

2. Update `Program.cs` to register `SemanticKernelAgentDriver`:
```csharp
builder.Services.AddOpenAIChatCompletion(
    modelId: builder.Configuration["OpenAI:ModelId"]!,
    apiKey: builder.Configuration["OpenAI:ApiKey"]!);
builder.Services.AddScoped<IAgentDriver, SemanticKernelAgentDriver>();
```

Each council member can have a custom `systemPromptOverride` to specialise their AI behaviour.

---

## API Reference Summary

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/issues` | List all issues |
| `POST` | `/api/issues` | Create a new issue |
| `GET` | `/api/issues/{id}` | Get issue by ID |
| `POST` | `/api/councils` | Create a council for an issue |
| `GET` | `/api/councils/{id}` | Get council by ID |
| `POST` | `/api/councils/{id}/members` | Add a member to a council |
| `POST` | `/api/sessions/start` | Start a new session |
| `GET` | `/api/sessions/{id}` | Get session status |
| `POST` | `/api/sessions/{id}/execute-round` | Execute the next round |
| `POST` | `/api/sessions/{id}/finalize` | Finalize and get result |
| `GET` | `/health` | Health check |

Full interactive docs available at the Swagger UI root (`/`).

---

## Development

```bash
# Build entire solution
dotnet build Deepr.sln

# Run API (http://localhost:5011 — Swagger at root)
cd src/Deepr.API && dotnet run

# Run Web client (http://localhost:5012)
cd src/Deepr.Web && dotnet run

# Create/update migrations
dotnet tool install --global dotnet-ef
cd src/Deepr.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../Deepr.API

# Run tests
dotnet test
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for production deployment instructions.

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
