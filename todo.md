# Deepr Development Task List

## Work Breakdown Structure (WBS)

### ✅ Phase 1: Foundation (Completed)
- [x] Clean Architecture scaffolding
- [x] Domain model with DDD principles
- [x] Strategy interfaces (Cartridge System)
- [x] EF Core configuration with JSONB support
- [x] Documentation (ERD, Sequence Diagrams)

### 🚧 Phase 2: Core Implementation (In Progress)

#### 2.1 Decision Method Implementations
- [ ] Implement Delphi method
  - [ ] Anonymous voting logic
  - [ ] Convergence detection
  - [ ] Round-based consensus building
- [ ] Implement NGT (Nominal Group Technique)
  - [ ] Structured voting
  - [ ] Ranking algorithm
  - [ ] Priority calculation
- [ ] Implement Brainstorming method
  - [ ] Free-form collection
  - [ ] No-criticism enforcement
  - [ ] Idea consolidation
- [ ] Implement NominalGroupTechnique method
  - [ ] Silent generation
  - [ ] Round-robin sharing
  - [ ] Voting mechanism
- [ ] Implement ConsensusBuilding method
  - [ ] Discussion facilitation
  - [ ] Agreement tracking
  - [ ] Final consensus validation

#### 2.2 Tool Adapter Implementations
- [ ] Implement SWOT adapter
  - [ ] Schema: {strengths, weaknesses, opportunities, threats}
  - [ ] Parser for SWOT responses
  - [ ] Validation rules
- [ ] Implement AHP (Analytic Hierarchy Process) adapter
  - [ ] Schema: {criteria, weights, comparisons}
  - [ ] Pairwise comparison parsing
  - [ ] Consistency ratio validation
- [ ] Implement WeightedScoring adapter
  - [ ] Schema: {options, criteria, scores}
  - [ ] Score normalization
  - [ ] Weight distribution validation
- [ ] Implement CostBenefitAnalysis adapter
  - [ ] Schema: {costs, benefits, roi}
  - [ ] Financial calculation parsing
  - [ ] NPV/IRR validation
- [ ] Implement DecisionMatrix adapter
  - [ ] Schema: {alternatives, criteria, ratings}
  - [ ] Matrix calculation
  - [ ] Optimal choice identification

#### 2.3 Agent Driver Implementations
- [ ] Implement AI Agent Driver (SemanticKernel)
  - [ ] OpenAI integration
  - [ ] System prompt handling
  - [ ] Response parsing
  - [ ] Retry logic
- [ ] Implement Human Agent Driver
  - [ ] WebSocket notifications
  - [ ] Response collection API
  - [ ] Timeout handling
  - [ ] Availability tracking

#### 2.4 Session Orchestrator Implementation
- [ ] Implement SessionOrchestrator service
  - [ ] StartSessionAsync logic
  - [ ] ExecuteNextRoundAsync coordination
  - [ ] ShouldContinueSessionAsync evaluation
  - [ ] FinalizeSessionAsync aggregation
- [ ] Add orchestrator unit tests
- [ ] Add integration tests for full flow

### 📋 Phase 3: API & Application Layer

#### 3.1 CQRS Commands & Queries (MediatR)
- [ ] CreateIssueCommand
- [ ] CreateCouncilCommand
- [ ] StartSessionCommand
- [ ] ExecuteRoundCommand
- [ ] FinalizeSessionCommand
- [ ] GetIssueQuery
- [ ] GetCouncilQuery
- [ ] GetSessionQuery
- [ ] GetSessionHistoryQuery

#### 3.2 API Controllers
- [ ] IssuesController
  - [ ] POST /api/issues
  - [ ] GET /api/issues/{id}
  - [ ] PUT /api/issues/{id}
  - [ ] DELETE /api/issues/{id}
- [ ] CouncilsController
  - [ ] POST /api/councils
  - [ ] GET /api/councils/{id}
  - [ ] PUT /api/councils/{id}
  - [ ] POST /api/councils/{id}/members
- [ ] SessionsController
  - [ ] POST /api/sessions/start
  - [ ] POST /api/sessions/{id}/execute-round
  - [ ] GET /api/sessions/{id}
  - [ ] POST /api/sessions/{id}/pause
  - [ ] POST /api/sessions/{id}/resume
  - [ ] GET /api/sessions/{id}/finalize

#### 3.3 Validators (FluentValidation)
- [ ] CreateIssueCommandValidator
- [ ] CreateCouncilCommandValidator
- [ ] StartSessionCommandValidator
- [ ] ExecuteRoundCommandValidator

### 🗄️ Phase 4: Database

#### 4.1 EF Core Migrations
- [ ] Create initial migration
- [ ] Test migration on PostgreSQL
- [ ] Add seed data for development
- [ ] Document migration process

#### 4.2 Repository Implementations
- [ ] Ensure Repository<T> handles all scenarios
- [ ] Add specialized methods if needed
- [ ] Add query optimizations

### 🧪 Phase 5: Testing

#### 5.1 Unit Tests
- [ ] Domain entity tests
- [ ] Value object tests
- [ ] Decision method tests
- [ ] Tool adapter tests
- [ ] Agent driver tests
- [ ] Orchestrator tests

#### 5.2 Integration Tests
- [ ] API endpoint tests
- [ ] Database integration tests
- [ ] Full session flow tests

### 📚 Phase 6: Documentation & DevOps

#### 6.1 Documentation
- [ ] API documentation (Swagger enhanced)
- [ ] Architecture decision records (ADR)
- [ ] Development setup guide
- [ ] Deployment guide

#### 6.2 DevOps
- [ ] Docker containerization
- [ ] Docker Compose for local dev
- [ ] CI/CD pipeline
- [ ] Environment configuration

## Notes

- All tasks should maintain Clean Architecture principles
- Follow DDD for domain logic
- Keep UML diagrams (erd.puml, sequence-diagram.puml) updated
- No `todo:` comments in code - use this file instead
- Minimum changes per commit
- Review uncommitted changes before new work
