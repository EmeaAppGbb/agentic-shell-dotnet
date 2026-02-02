---
name: human-in-the-loop
description: Implement human-in-the-loop approval patterns where agents request user approval before proceeding. Use when adding approval workflows, user confirmations, or HITL features.
---

# Implementing Human-in-the-Loop Approval

**HITL** allows agents to request user approval before proceeding. Requires backend-frontend coordination.

## Backend: Create Approval Requests

### 1. Use ApprovalRequestHelper

Located at `src/agentic-api/ApprovalRequestHelper.cs`:

```csharp
public static class ApprovalRequestHelper
{
    public static FunctionApprovalRequestContent CreateApprovalRequest(
        string functionName,
        Dictionary<string, object?> arguments)
    {
        return new FunctionApprovalRequestContent(
            Guid.NewGuid().ToString(),
            new FunctionCallContent(functionName, functionName, arguments: arguments)
        );
    }
}
```

### 2. Return Approval Requests from Executors

Instead of returning a direct response, return an approval request:

```csharp
public override async ValueTask<AIContent> HandleAsync(UserInputEvent input, IWorkflowContext context, CancellationToken ct)
{
    var responseText = await GenerateContent(input);

    // Return approval request instead of direct response
    return ApprovalRequestHelper.CreateApprovalRequest(
        functionName: "approve_copyright_command",  // Must match frontend hook name
        arguments: new Dictionary<string, object?> { { "copyright", responseText } }
    );
}
```

### 3. Handle Approval Responses in Input Executor

```csharp
private async ValueTask<UserInputEvent> HandleChatMessagesAsync(List<ChatMessage> messages, IWorkflowContext context, CancellationToken ct)
{
    // Check for approval response (comes back as ChatRole.Tool with FunctionResultContent)
    var approvalMessage = messages.LastOrDefault(m => m.Role == ChatRole.Tool);
    var functionResult = approvalMessage?.Contents.OfType<FunctionResultContent>().FirstOrDefault();

    var textApproved = functionResult?.Result?.ToString()?.Contains("text-approved");
    var textRejected = functionResult?.Result?.ToString()?.Contains("text-rejected");

    // Route based on approval status
    if (textApproved == true) return new UserInputEvent { NextStep = WorkflowSteps.GenerateImage };
    if (textRejected == true) return new UserInputEvent { NextStep = WorkflowSteps.RegenerateText };
    return new UserInputEvent { NextStep = WorkflowSteps.GenerateText };  // Start workflow
}
```

**Note**: `AGUIWorkflowAgent` automatically converts `FunctionApprovalRequestContent` to `FunctionCallContent` for the frontend (auto-registered via `.AddAsAIAgent()`).

## Frontend: Add useHumanInTheLoop Hook

```typescript
import { useHumanInTheLoop } from "@copilotkit/react-core";

export default function Page() {
  const [approvedContent, setApprovedContent] = useState<string | null>(null);

  useHumanInTheLoop({
    name: "approve_copyright_command",  // Must match backend functionName
    description: "Ask the user to approve the generated text content",
    parameters: [
      { name: "copyright", type: "string", description: "The text to approve", required: true },
    ],
    render: ({ args, respond }) => {
      if (!respond) return <></>;
      return (
        <div className="approval-container">
          <pre>{args.copyright}</pre>
          <button onClick={() => { setApprovedContent(args.copyright); respond("text-approved"); }}>Approve</button>
          <button onClick={() => { respond("text-rejected"); }}>Reject</button>
        </div>
      );
    },
  });
  // ...
}
```

## Key Matching Requirements

| Frontend | Backend | Must Match |
|----------|---------|------------|
| `name` | `functionName` | Exact string match |
| `parameters[].name` | `arguments` dictionary keys | Key names |
| `respond()` value | `FunctionResultContent.Result` check | Response strings |

## Workflow with Conditional Routing

```csharp
public Workflow BuildWorkflow(string name)
{
    var chatInput = new DummyChatInputExecutor(_inputLogger);
    var textGenerator = new TextGeneratorExecutor(_logger, _chatClient);
    var imageGenerator = new ImageGeneratorExecutor(_logger, _chatClient, _imageGenerator);

    return new WorkflowBuilder(chatInput)
        .WithName(name)
        .AddSwitch(chatInput, switchBuilder =>
            switchBuilder
                .AddCase(input => input?.NextStep == WorkflowSteps.GenerateText, textGenerator)
                .AddCase(input => input?.NextStep == WorkflowSteps.GenerateImage, imageGenerator)
                .WithDefault(textGenerator))
        .WithOutputFrom(textGenerator)
        .WithOutputFrom(imageGenerator)
        .Build();
}
```

## Execution Flow

```
User → InputExecutor (checks approvals) → Executor → Returns FunctionApprovalRequestContent
    → Frontend shows approval UI → User responds → InputExecutor routes to next step
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Approval UI not appearing | Check `functionName` matches exactly, executor registered with `.WithOutputFrom()` |
| Response not reaching backend | Ensure `respond()` called with string, check `ChatRole.Tool` messages in input executor |

## Best Practices

- Use descriptive names: `approve_copyright_command`, `validate_data_command`
- Use clear response values: `"text-approved"`, `"image-rejected"`
- Always provide both approve and reject options
