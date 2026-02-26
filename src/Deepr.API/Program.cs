using Deepr.Application.Behaviors;
using Deepr.Application.Interfaces;
using Deepr.Infrastructure.AgentDrivers;
using Deepr.Infrastructure.DecisionMethods;
using Deepr.Infrastructure.Persistence;
using Deepr.Infrastructure.Repositories;
using Deepr.Infrastructure.Services;
using Deepr.Infrastructure.ToolAdapters;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS for the Blazor client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5012", "http://localhost:8081" })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Deepr API", Version = "v1", Description = "Ultimate decision making assistant API" });
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

// Add MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(IDecisionMethod).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Add FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(IDecisionMethod).Assembly);

// Add Repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Add Decision Methods
builder.Services.AddScoped<IDecisionMethod, BrainstormingMethod>();
builder.Services.AddScoped<IDecisionMethod, DelphiMethod>();
builder.Services.AddScoped<IDecisionMethod, ConsensusMethod>();
builder.Services.AddScoped<IDecisionMethod, NgtMethod>();
builder.Services.AddScoped<IDecisionMethod, AdkarMethod>();
builder.Services.AddScoped<IDecisionMethod, WeightedDeliberationMethod>();
builder.Services.AddScoped<IDecisionMethod, AhpMethod>();
builder.Services.AddScoped<IDecisionMethod, ElectreMethod>();
builder.Services.AddScoped<IDecisionMethod, TopsisMethod>();
builder.Services.AddScoped<IDecisionMethod, PrometheeMethod>();
builder.Services.AddScoped<IDecisionMethod, GreyTheoryMethod>();

// Add Tool Adapters
builder.Services.AddScoped<IToolAdapter, SwotToolAdapter>();
builder.Services.AddScoped<IToolAdapter, WeightedScoringAdapter>();
builder.Services.AddScoped<IToolAdapter, PestleToolAdapter>();

// Add Agent Driver (Echo driver as default; replace with SemanticKernelAgentDriver when OpenAI is configured)
builder.Services.AddScoped<IAgentDriver, EchoAgentDriver>();

// Add Session Orchestrator
builder.Services.AddScoped<ISessionOrchestrator, SessionOrchestratorService>();

// Add Session Export Service
builder.Services.AddScoped<ISessionExportService, SessionExportService>();

// Add health check
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Deepr API v1");
    c.RoutePrefix = string.Empty; // Serve at root
});

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
