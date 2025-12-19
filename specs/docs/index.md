# Agentic Shell .NET Documentation

Welcome to the technical documentation and feature specifications for the **agentic-shell-dotnet** project.

## Overview

**agentic-shell-dotnet** is a microservices-based AI agent application built using the Microsoft Agent Framework.

| Component | Stack |
|-----------|-------|
| **Frontend** | Next.js 16 + React 19 + TypeScript + CopilotKit |
| **Backend** | ASP.NET Core 10 + Microsoft Agent Framework |
| **Orchestration** | .NET Aspire |
| **Deployment** | Azure Container Apps + Azure AI services |

## Quick Start

```bash
# Setup
az login && azd auth login && azd provision

# Run locally
aspire run   # Dashboard: http://localhost:15888

# Build & Deploy
./build.sh
azd deploy
```

## Documentation Sections

### Technical Documentation

- **[Architecture](architecture/overview.md)** - System design and security
- **[Infrastructure](infrastructure/deployment.md)** - Deployment and operations
- **[Integration](integration/apis.md)** - API specifications
- **[Technology](technology/stack.md)** - Stack and dependencies

### Feature Requirements

- **[AI Chat Interface](features/ai-chat-interface.md)** - Chat interface specifications

## Key Resources

- [Project Repository](https://github.com/EmeaAppGbb/agentic-shell-dotnet)
- [AGENTS.md](https://github.com/EmeaAppGbb/agentic-shell-dotnet/blob/main/AGENTS.md) - AI Agent Instructions
- [README.md](https://github.com/EmeaAppGbb/agentic-shell-dotnet/blob/main/README.md) - Human Developer Guide
