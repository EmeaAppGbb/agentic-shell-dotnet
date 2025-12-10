#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace agentic_api.Workflows;

/// <summary>
/// Dummy workflow factory that creates a simple echo workflow for testing.
/// </summary>
public class DummyWorkflowFactory
{
    private readonly ILogger<DummyChatInputExecutor> _inputLogger;
    private readonly ILogger<GreetingExecutor> _greetingLogger;
    private readonly ILogger<DesignerExecutor> _designerLogger;
    private readonly ILogger<SummaryExecutor> _summaryLogger;
    private readonly IChatClient _chatClient;
    private readonly IImageGenerator _imageGenerator;

    public DummyWorkflowFactory(
        ILogger<DummyChatInputExecutor> chatInputLogger,
        ILogger<GreetingExecutor> greetingLogger,
        ILogger<DesignerExecutor> designerLogger,
        ILogger<SummaryExecutor> summaryLogger,
        IChatClient chatClient,
        IImageGenerator imageGenerator)
    {
        _inputLogger = chatInputLogger;
        _greetingLogger = greetingLogger;
        _designerLogger = designerLogger;
        _summaryLogger = summaryLogger;
        _chatClient = chatClient;
        _imageGenerator = imageGenerator;
    }

    public Workflow BuildWorkflow(string name)
    {
        // Create executors
        var chatInput = new DummyChatInputExecutor(_inputLogger);
        var greeting = new GreetingExecutor(_greetingLogger, _chatClient);
        var designer = new DesignerExecutor(_designerLogger,_chatClient, _imageGenerator);
        var summary = new SummaryExecutor(_summaryLogger);

        // Build simple workflow: ChatInput -> Greeting
        var workflowBuilder = new WorkflowBuilder(chatInput)
            .WithName(name)
            .AddSwitch(chatInput, switchBuilder =>
                switchBuilder.AddCase(GenerateCopy(), greeting)
                             .AddCase(GenerateDesign(), designer)
                             .AddCase(Summarize(), summary)
                             .WithDefault(greeting)
            )
            .WithOutputFrom(greeting)
            .WithOutputFrom(designer)
            .WithOutputFrom(summary);

        return workflowBuilder.Build();
    }

    public static Func<UserInputEvent?, bool> GenerateCopy() => (input) =>
    {
        return input?.NextStep == DummyWorkflowSteps.GenerateCopy;
    };

    public static Func<UserInputEvent?, bool> GenerateDesign() => (input) =>
    {
        return input?.NextStep == DummyWorkflowSteps.GenerateDesign;
    };

    public static Func<UserInputEvent?, bool> Summarize() => (input) =>
    {
        return input?.NextStep == DummyWorkflowSteps.Summary;
    };

}

/// <summary>
/// ChatInput executor that accepts List<ChatMessage> and TurnToken for dummy workflow.
/// </summary>
public sealed class DummyChatInputExecutor : Executor
{
    private readonly ILogger<DummyChatInputExecutor> _logger;

    public DummyChatInputExecutor(ILogger<DummyChatInputExecutor> logger) : base("DummyChatInput")
    {
        _logger = logger;
    }

    protected override Microsoft.Agents.AI.Workflows.RouteBuilder ConfigureRoutes(Microsoft.Agents.AI.Workflows.RouteBuilder routeBuilder) =>
        routeBuilder
            .AddHandler<List<ChatMessage>, UserInputEvent>(HandleChatMessagesAsync)
            .AddHandler<TurnToken, string>(HandleTurnTokenAsync);

    private async ValueTask<UserInputEvent> HandleChatMessagesAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        var approvalMessage = messages.LastOrDefault(m => m.Role == ChatRole.Tool);
        var functionResult = approvalMessage?.Contents.OfType<FunctionResultContent>().FirstOrDefault();

        var copyApproved = functionResult?.Result?.ToString()?.Contains("copy-approved");
        var designApproved = functionResult?.Result?.ToString()?.Contains("design-approved");

        if (copyApproved == true)
        {
            _logger.LogInformation("Copy approved by user.");
            return new UserInputEvent { Input = lastUserMessage?.Text ?? "Hello", NextStep = DummyWorkflowSteps.GenerateDesign };
        }

        else if (designApproved == true)
        {
            _logger.LogInformation("Design approved by user.");
            return new UserInputEvent { Input = lastUserMessage?.Text ?? "Hello", NextStep = DummyWorkflowSteps.Summary };
        }

        else
        {
            _logger.LogInformation("No approvals detected, proceeding to generate copy.");
            return new UserInputEvent { Input = lastUserMessage?.Text ?? "Hello", NextStep = DummyWorkflowSteps.GenerateCopy };
        }
    }

    private async ValueTask<string> HandleTurnTokenAsync(
        TurnToken turnToken,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var userInput = "Hello from TurnToken";

        _logger.LogInformation("Dummy Workflow started with TurnToken");

        return userInput;
    }
}

/// <summary>
/// Greeting executor that uses IChatClient to generate friendly AI greetings.
/// </summary>
public sealed class GreetingExecutor : Executor<UserInputEvent, AIContent>
{
    private readonly ILogger<GreetingExecutor> _logger;
    private readonly AIAgent _agent;
    public GreetingExecutor(ILogger<GreetingExecutor> logger, IChatClient chatClient) : base("Greeting")
    {
        _logger = logger;

        _agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = "GreetingAgent",
            Instructions = "You are a friendly AI marketing assistant. Create a short and funny marketing copy for the user input."
        });
    }

    public override async ValueTask<AIContent> HandleAsync(
        UserInputEvent input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Greeting executor received: {Input}", input.Input);
            _logger.LogInformation("Calling AI agent to generate greeting response");

            var agentResponse = await _agent.RunAsync(new ChatMessage(ChatRole.User, input.Input), cancellationToken: cancellationToken);

            var responseText = agentResponse.Text ?? "Hi there!";
            _logger.LogInformation($"AI agent responded with: {responseText}");
            
            return new FunctionApprovalRequestContent(Guid.NewGuid().ToString(), new FunctionCallContent("approve_copyright_command", "approve_copyright_command", arguments: new Dictionary<string, object?>
            {
                { "copyright", responseText }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in greeting executor: {Message}. Type: {Type}. StackTrace: {StackTrace}",
                ex.Message, ex.GetType().Name, ex.StackTrace);
            return new TextContent("Hi! I had trouble processing your message, but I'm here to help!");
        }
    }
}

/// <summary>
/// Greeting executor that uses IChatClient to generate friendly AI greetings.
/// </summary>
public sealed class SummaryExecutor : Executor<UserInputEvent, AIContent>
{
    private readonly ILogger<SummaryExecutor> _logger;
    public SummaryExecutor(ILogger<SummaryExecutor> logger) : base("Summary")
    {
        _logger = logger;
    }

    public override async ValueTask<AIContent> HandleAsync(
        UserInputEvent input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Summary executor received: {Input}", input.Input);
            _logger.LogInformation("Calling AI agent to generate summary response");


            var responseText = "All Good!";
            _logger.LogInformation($"AI agent responded with: {responseText}");

            return new TextContent(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in summary executor: {Message}. Type: {Type}. StackTrace: {StackTrace}",
                ex.Message, ex.GetType().Name, ex.StackTrace);
            return new TextContent("Hi! I had trouble processing your message, but I'm here to help!");
        }
    }
}

/// <summary>
/// Greeting executor that uses IChatClient to generate friendly AI greetings.
/// </summary>
public sealed class DesignerExecutor : Executor<UserInputEvent, AIContent>
{
    private readonly ILogger<DesignerExecutor> _logger;
    private readonly IImageGenerator _imageGenerator;

    private readonly AIAgent _agent;
    public DesignerExecutor(ILogger<DesignerExecutor> logger,IChatClient chatClient, IImageGenerator imageGenerator) : base("Designer")
    {
        _logger = logger;
        _agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
                {
                    Name = "GreetingAgent",
                    Instructions = "You are a expert in generating prompts for image models. Take the user input for a campaign design and generate safe and detailed image generation prompt"
                });
        _imageGenerator = imageGenerator;
    }

    public override async ValueTask<AIContent> HandleAsync(
        UserInputEvent input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Designer executor received: {Input}", input.Input);
            _logger.LogInformation("Calling AI agent to generate image");

            var agentResponse = await _agent.RunAsync(new ChatMessage(ChatRole.User, input.Input), cancellationToken: cancellationToken);

            var imagePrompt = agentResponse.Text ?? "A tennis court in a jungle";

            // Generate an image from a text prompt
            var options = new ImageGenerationOptions
            {
                MediaType = "image/png",
                ResponseFormat = ImageGenerationResponseFormat.Hosted
            };


            var response = await _imageGenerator.GenerateImagesAsync(imagePrompt, options);
            var dataContent = response.Contents.OfType<DataContent>().First();

            _logger.LogInformation($"image was created at {dataContent.Uri}");
            return new FunctionApprovalRequestContent(Guid.NewGuid().ToString(), new FunctionCallContent("approve_design_command", "approve_design_command", arguments: new Dictionary<string, object?>
            {
                { "design", dataContent.Uri }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in designer executor: {Message}. Type: {Type}. StackTrace: {StackTrace}",
                ex.Message, ex.GetType().Name, ex.StackTrace);
            return new TextContent("Hi! I had trouble processing your message, but I'm here to help!");
        }
    }
}

public class WorkflowState
{
    public bool CampaignApproved { get; set; }
    public bool DesignApproved { get; set; }

    public string? Campaign { get; set; }
    public string? Design { get; set; }
}

public class UserInputEvent
{
    public required string Input { get; set; }
    public DummyWorkflowSteps NextStep { get; set; }
}

public class AgentRunResponse
{
    public required string Text { get; set; }
    public string? ImageUrl { get; set; }
}

public enum DummyWorkflowSteps
{
    GenerateCopy,
    GenerateDesign,
    Summary
}