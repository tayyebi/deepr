# deepr — Ultimate Decision Making Assistant

**deepr** is a structured group decision-making API that orchestrates multi-agent councils using proven decision methods (Delphi, NGT, Brainstorming, Consensus Building, ADKAR, Weighted Deliberation) and analytical tools (SWOT, PESTLE, Weighted Scoring). It supports both AI agents (via [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)) and human participants.

## Screenshots

### Web Client — Template Gallery (with method, tool & agent badges)

![Template Gallery](https://github.com/user-attachments/assets/b6e3aa8a-3ac7-4e77-9eff-21017e23e37a)

### Web Client — New Issue pre-filled from Template (with recommendation banner)

![New Issue pre-filled from template](https://github.com/user-attachments/assets/5830812e-ac0b-4329-8de2-e469adde084a)

### Web Client — Issue Detail with template-suggested council form (method + tool pre-selected)

![Issue Detail — template council form pre-filled](https://github.com/user-attachments/assets/c5a711ab-6463-4127-81bb-9d5ae71a3d5a)

### Web Client — Council Detail with suggested agents (one-click add, shows ✓ when added)

![Council Detail — suggested agents added](https://github.com/user-attachments/assets/de4afa7c-9611-4bf3-8f46-03d5a48b2b78)

### Web Client — Dashboard

![Dashboard](https://github.com/user-attachments/assets/41b17dce-c0b9-4f3f-84c2-1ca1ff7c172b)

### Web Client — Create Issue

![Create Issue Form](https://github.com/user-attachments/assets/3a0d0ab1-2dc2-437c-ad43-cf9d2df1a083)

### Web Client — Create Council (all methods including Weighted Deliberation)

![Create Council — all methods](https://github.com/user-attachments/assets/ee4a0229-1212-436f-b2c5-7c72c1636016)

### Web Client — Add Member with Custom AI Persona Prompt

![Add Member — custom AI prompt per agent](https://github.com/user-attachments/assets/cdf8a324-980b-4416-af0e-23f7149f823c)

### Web Client — Council with Roles (Moderator, Expert, Critic, Observer)

![Council members with roles and custom prompts](https://github.com/user-attachments/assets/7361de87-9b31-460e-8b01-9bd22746a172)

### Web Client — Voting Round with Structured Scores

![Voting round — each member submits scores](https://github.com/user-attachments/assets/505521f0-e405-485a-8bb8-0408b92b1689)

### Web Client — Decision Matrix Results (inline after voting)

![Decision matrix table with weighted scores and ranking](https://github.com/user-attachments/assets/8e7a0b67-176d-4a1c-a0c2-465cc237dd0f)

### Web Client — Create Council with ADKAR & PESTLE

![Create Council — ADKAR and PESTLE selected](https://github.com/user-attachments/assets/7bea5274-9a2a-4afd-b450-2849f1da5ced)

### Web Client — ADKAR Phase 1 (Awareness) Round Result

![ADKAR Phase 1 Awareness round](https://github.com/user-attachments/assets/b6be7ad8-37d9-41cb-a8fb-e1b3cfd09bf2)

### Web Client — ADKAR Session Completed (all 5 phases)

![ADKAR all 5 phases completed](https://github.com/user-attachments/assets/f1effa43-9565-4a0d-8b8f-4144b38bb664)

### Web Client — Export Decision Sheet button

![Export Decision Sheet](https://github.com/user-attachments/assets/14cb4607-7a39-4250-8021-b76498d53c71)

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
| `NGT` (1) | Nominal Group Technique — silent generation, sharing, clarification, voting | 4 |
| `ADKAR` (5) | Change management — Awareness, Desire, Knowledge, Ability, Reinforcement | 5 |
| `WeightedDeliberation` (6) | Moderator frames → Experts discuss → Vote → Weighted scoring matrix | 4 |

### Analytical Tools
| Tool | Description |
|---|---|
| `SWOT` (0) | Strengths, Weaknesses, Opportunities, Threats |
| `WeightedScoring` (2) | Multi-criteria option scoring |
| `PESTLE` (5) | Political, Economic, Social, Technological, Legal, Environmental |

### Agent Roles
| Role | Value | Description | Participates in voting |
|---|---|---|---|
| `Chairman` / Moderator | 0 | Frames the problem and leads discussion | ✅ |
| `Expert` | 1 | Domain expert analysis and scoring | ✅ |
| `Critic` | 2 | Challenges assumptions and scores | ✅ |
| `Observer` | 3 | Watch-only — never contributes to rounds | ❌ |

> **Observers are automatically silenced** — the orchestrator skips them in every round regardless of the chosen method.

### Weighted Deliberation User Story

> *A team of experts is presented with a complex problem. One person acts as Moderator, several become Commenters, and some are Observers. Each is an AI model with a specific system prompt persona. After structured discussion rounds, a vote takes place. Each voter scores options against weighted criteria. The system computes a weighted scoring matrix and ranks the options. Results are exported as a decision sheet.*

| Round | Phase | Who participates |
|---|---|---|
| 1 | **Moderator Framing** — presents options and criteria | All non-Observer members |
| 2 | **Expert Discussion** — analyse strengths/weaknesses | All non-Observer members |
| 3 | **Expert Deliberation** — refine positions | All non-Observer members |
| 4 | **Voting** — score each option on each criterion (0–10) | All non-Observer members |

After Round 4 the system automatically computes the **weighted scoring matrix** and ranks options by `Σ(weight × average_score)`. The result is displayed inline in the session panel and included in the exported decision sheet.

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
Templates → Issue → Council → Session → Rounds → Finalize
```

1. **Choose a Template** — pick a pre-built starting point or start blank
2. **Create an Issue** — define the problem to be decided
3. **Create a Council** — choose a decision method and analytical tool
4. **Add Members** — assign AI or human agents to the council
5. **Start a Session** — initialise the session with context
6. **Execute Rounds** — agents deliberate and contribute
7. **Finalize** — aggregate results and get the final outcome

---

### Step 0 — Choose a Template

Navigate to **New Issue** in the sidebar (or click `+ New Issue` on the dashboard). You will be taken to the **Template Gallery** — a curated collection of 21 ready-to-use decision templates organised by category:

| Category | Templates |
|---|---|
| Blank | ✏️ Blank Issue — start fresh |
| Technology | 🏗️ Microservices vs Monolith, ☁️ Cloud Migration, ⚙️ Tech Stack Selection, 🔧 DevOps Toolchain, 🌐 Open-Source Strategy, 🤖 AI Adoption |
| HR & Organisation | 🏠 Remote/Hybrid Work Policy, 💰 Compensation Framework, 🏢 Team Restructuring, 📊 Performance Review |
| Product & Business | 📋 Feature Prioritisation, 🚀 Go-to-Market Strategy, 🏷️ Pricing Model Change, 🤝 Partnership Evaluation |
| Finance | 💼 Budget Allocation, 📈 Strategic Investment |
| Procurement | 🛒 Vendor Selection |
| Risk | ⚠️ Risk Assessment |
| Operations | 🏬 Office Space Decision |
| Sustainability | 🌱 ESG Initiatives |
| Compliance | 🔒 Data Privacy Policy |

Selecting a template pre-fills the **Title** and **Context** fields so you can start immediately, or refine them before creating the issue. Selecting **Blank Issue** opens an empty form.

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

> `selectedMethod` values: `0`=Delphi, `1`=NGT, `2`=Brainstorming, `4`=ConsensusBuilding, `5`=ADKAR  
> `selectedTool` values: `0`=SWOT, `2`=WeightedScoring, `5`=PESTLE

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
| `GET` | `/api/sessions/{id}/export` | Export classified decision sheet (`.md`) |
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
