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
    private readonly IChatClient _chatClient;

    public DummyWorkflowFactory(
        ILogger<DummyChatInputExecutor> chatInputLogger,
        ILogger<GreetingExecutor> greetingLogger,
        IChatClient chatClient)
    {
        _inputLogger = chatInputLogger;
        _greetingLogger = greetingLogger;
        _chatClient = chatClient;
    }

    public Workflow BuildWorkflow(string name)
    {
        // Create executors
        var chatInput = new DummyChatInputExecutor(_inputLogger);
        var greeting = new GreetingExecutor(_greetingLogger, _chatClient);

        // Build simple workflow: ChatInput -> Greeting
        var workflowBuilder = new WorkflowBuilder(chatInput)
            .WithName(name)
            .AddEdge(chatInput, greeting)
            .WithOutputFrom(greeting);

        return workflowBuilder.Build();
    }
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
            .AddHandler<List<ChatMessage>, string>(HandleChatMessagesAsync)
            .AddHandler<TurnToken, string>(HandleTurnTokenAsync);

    private async ValueTask<string> HandleChatMessagesAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        var userInput = lastUserMessage?.Text ?? "Hello";

        _logger.LogInformation("Dummy Workflow started with input: {Input}", userInput);

        return userInput;
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
public sealed class GreetingExecutor : Executor<string, string>
{
    private readonly ILogger<GreetingExecutor> _logger;
    private readonly AIAgent _agent;

    public GreetingExecutor(ILogger<GreetingExecutor> logger, IChatClient chatClient) : base("Greeting")
    {
        _logger = logger;
        _agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = "GreetingAgent",
            Instructions = "You are a friendly AI assistant. Greet the user warmly and respond to their message with enthusiasm."
        });
    }

    public override async ValueTask<string> HandleAsync(
        string input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Greeting executor received: {Input}", input);
            _logger.LogInformation("Calling AI agent to generate greeting response");

            var response = await _agent.RunAsync(new ChatMessage(ChatRole.User, input), cancellationToken: cancellationToken);
            
            var responseText = response.Text ?? "Hi there!";
            _logger.LogInformation("AI agent responded with: {Response}", responseText);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in greeting executor: {Message}. Type: {Type}. StackTrace: {StackTrace}",
                ex.Message, ex.GetType().Name, ex.StackTrace);
            return "Hi! I had trouble processing your message, but I'm here to help!";
        }
    }
}
