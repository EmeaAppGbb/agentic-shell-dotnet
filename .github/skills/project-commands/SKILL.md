---
name: project-commands
description: Run common project commands for setup, local development, building, deploying, and testing. Use when setting up the project, running locally, building, deploying to Azure, or running tests.
---

# Project Commands

Common commands for working with the agentic-shell-dotnet project.

## Setup

Authenticate and provision Azure resources:

```bash
az login && azd auth login && azd provision
```

This command:
1. Logs into Azure CLI
2. Logs into Azure Developer CLI
3. Provisions all required Azure resources (AI services, Container Apps, etc.)

## Run Locally

Start the application with .NET Aspire orchestration:

```bash
aspire run
```

This starts all services with proper environment injection:
- **Aspire Dashboard**: http://localhost:15888
- **UI (Next.js)**: http://localhost:3000
- **API (ASP.NET Core)**: http://localhost:5149

**Note**: Always use `aspire run` for local development to ensure environment variables are properly injected.

## Build

Build all components:

```bash
./build.sh
```

## Deploy to Azure

Deploy the application to Azure Container Apps:

```bash
azd deploy
```

## Run Tests

### Backend Tests (.NET)

```bash
dotnet test tests/agentic-api-tests/agentic-api-tests.csproj
```

### Frontend Tests (Next.js)

```bash
cd src/agentic-ui && npm test
```

## Quick Reference

| Task | Command |
|------|---------|
| Setup | `az login && azd auth login && azd provision` |
| Run locally | `aspire run` |
| Build all | `./build.sh` |
| Deploy | `azd deploy` |
| Backend tests | `dotnet test tests/agentic-api-tests/agentic-api-tests.csproj` |
| Frontend tests | `cd src/agentic-ui && npm test` |

## Local Development URLs

| Service | URL |
|---------|-----|
| Aspire Dashboard | http://localhost:15888 |
| Frontend UI | http://localhost:3000 |
| Backend API | http://localhost:5149 |
