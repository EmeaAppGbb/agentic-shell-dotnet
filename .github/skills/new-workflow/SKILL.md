---
name: new-workflow
description: Create a new agent workflow with executors, registration, and AGUI protocol integration. Use when adding new AI agent capabilities, creating executors, or building workflow graphs.
---

# Adding a New Agent Workflow

Follow these steps to create a new workflow in the agentic-shell-dotnet project.

## 1. Create Workflow File

Create a new file at `src/agentic-api/Workflows/MyWorkflow.cs`:

```csharp
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace agentic_api.Workflows;

// Input Executor: Receives chat input, converts to internal event
public class MyChatInputExecutor(ILogger<MyChatInputExecutor> logger)
    : ExecutorBase<IConversationUpdate, UserInputEvent>(logger)
{
    protected override ValueTask ExecuteAsync(IConversationUpdate input, CancellationToken ct)
    {
        var userMessage = input switch
        {
            ChatMessage msg => msg.Text,
            TurnToken token => token.Text,
            _ => "Hello"
        };
        return ValueTask.FromResult(new ExecutionResult<UserInputEvent>(new UserInputEvent { Input = userMessage }));
    }
}

// Processing Executor: Handles business logic, calls AI models
public class MyProcessingExecutor(ILogger<MyProcessingExecutor> logger, IChatClient chatClient)
    : ExecutorBase<UserInputEvent, WorkflowOutputEvent>(logger)
{
    protected override async ValueTask ExecuteAsync(UserInputEvent input, CancellationToken ct)
    {
        var response = await chatClient.CompleteAsync($"User message: {input.Input}", cancellationToken: ct);
        return new ExecutionResult<WorkflowOutputEvent>(new WorkflowOutputEvent(response.Message.Text ?? "Hello!"));
    }
}

// Factory: Builds the workflow graph
public class MyWorkflowFactory(
    ILogger<MyChatInputExecutor> inputLogger,
    ILogger<MyProcessingExecutor> processingLogger,
    IChatClient chatClient)
{
    public Workflow BuildWorkflow(string name)
    {
        var inputExecutor = new MyChatInputExecutor(inputLogger);
        var processingExecutor = new MyProcessingExecutor(processingLogger, chatClient);

        return new WorkflowBuilder(inputExecutor)
            .WithName(name)
            .AddEdge(inputExecutor, processingExecutor)
            .WithOutputFrom(processingExecutor)  // Required for streaming to UI
            .Build();
    }
}
```

## 2. Register in Program.cs

Add the workflow registration in `src/agentic-api/Program.cs`:

```csharp
builder.Services.AddSingleton<MyWorkflowFactory>();
builder.AddWorkflow("MyWorkflow", (sp, name) =>
    sp.GetRequiredService<MyWorkflowFactory>().BuildWorkflow(name))
    .AddAsAIAgent();  // Wraps with AGUIWorkflowAgent for AGUI protocol compatibility
```

## What `.AddAsAIAgent()` Does

- Wraps workflow with `AGUIWorkflowAgent` for AGUI protocol
- Registers agent with AGUI endpoint (via `app.MapAGUI()`)
- Makes workflow accessible via `/api/copilotkit`

## Workflow Patterns

### Basic Linear Workflow
```
User Input → InputExecutor → ProcessingExecutor → Response
```

### Conditional Routing with Switch
```csharp
return new WorkflowBuilder(chatInput)
    .WithName(name)
    .AddSwitch(chatInput, switchBuilder =>
        switchBuilder
            .AddCase(input => input?.NextStep == WorkflowSteps.StepA, executorA)
            .AddCase(input => input?.NextStep == WorkflowSteps.StepB, executorB)
            .WithDefault(executorA))
    .WithOutputFrom(executorA)
    .WithOutputFrom(executorB)
    .Build();
```

## Streaming Messages to UI

Use `YieldOutputAsync` to stream intermediate results:

```csharp
await context.YieldOutputAsync(new AgentMessage { Text = "Processing..." });
```

**Critical**: Every executor calling `YieldOutputAsync` must be registered with `.WithOutputFrom()`:

```csharp
var workflow = new WorkflowBuilder(inputExecutor)
    .AddEdge(inputExecutor, processingExecutor)
    .WithOutputFrom(processingExecutor)  // Required!
    .Build();
```

## Key Files Reference

- `src/agentic-api/Program.cs` - Backend config and workflow registration
- `src/agentic-api/Workflows/DummyWorkflow.cs` - Example workflow
- `src/agentic-api/AGUIWorkflowAgent.cs` - AGUI adapter
