# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Deepr.sln", "./"]
COPY ["src/Deepr.Domain/Deepr.Domain.csproj", "src/Deepr.Domain/"]
COPY ["src/Deepr.Application/Deepr.Application.csproj", "src/Deepr.Application/"]
COPY ["src/Deepr.Infrastructure/Deepr.Infrastructure.csproj", "src/Deepr.Infrastructure/"]
COPY ["src/Deepr.API/Deepr.API.csproj", "src/Deepr.API/"]

# Restore dependencies
RUN dotnet restore "Deepr.sln"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/Deepr.API"
RUN dotnet build "Deepr.API.csproj" -c Release -o /app/build
RUN dotnet publish "Deepr.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r deepr && useradd -r -g deepr deepr

# Copy published application
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R deepr:deepr /app

# Switch to non-root user
USER deepr

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "Deepr.API.dll"]
