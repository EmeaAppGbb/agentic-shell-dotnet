using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;



public class AGUIWorkflowAgent : DelegatingAIAgent
{
    public AGUIWorkflowAgent(AIAgent innerAgent) : base(innerAgent) { }

    public override Task<AgentRunResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this.RunStreamingAsync(messages, thread, options, cancellationToken).ToAgentRunResponseAsync(cancellationToken);
    }

    public override async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var allUpdates = new List<AgentRunResponseUpdate>();
        await foreach (var update in this.InnerAgent.RunStreamingAsync(messages, thread, options, cancellationToken).ConfigureAwait(false))
        {
            switch (update.RawRepresentation)
            {
                case WorkflowOutputEvent outputEvent:
                    allUpdates.Add(update);
                    yield return CreateUpdateFromEvent(update, outputEvent.Data);
                    break;
                    
                // case ExecutorCompletedEvent completedEvent:
                //     allUpdates.Add(update);
                //     yield return CreateUpdateFromEvent(update, completedEvent.Data);
                //     break;
                    
                // case SuperStepCompletedEvent superStepEvent:
                //     allUpdates.Add(update);
                //     yield return CreateUpdateFromEvent(update, superStepEvent.Data);
                //     break;
                    
                default:
                    yield return update;
                    break;
            }
        }
    }

    private static AgentRunResponseUpdate CreateUpdateFromEvent(AgentRunResponseUpdate update, object? data)
    {
        var textContent = SerializeData(data);
        
        return new AgentRunResponseUpdate
        {
            AdditionalProperties = update.AdditionalProperties,
            AgentId = update.AgentId,
            AuthorName = update.AuthorName,
            CreatedAt = update.CreatedAt,
            Contents = { new TextContent(textContent) },
            ContinuationToken = update.ContinuationToken,
            MessageId = update.MessageId,
            RawRepresentation = update.RawRepresentation,
            ResponseId = update.ResponseId,
            Role = update.Role
        };
    }

    private static string SerializeData(object? data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        // If it's already a string, return as-is
        if (data is string str)
        {
            return str;
        }

        // For primitive types, use ToString()
        if (data.GetType().IsPrimitive || data is DateTime || data is DateTimeOffset || data is Guid)
        {
            return data.ToString() ?? string.Empty;
        }

        // For complex types, serialize to JSON
        try
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            // Fallback to ToString() if serialization fails
            return data.ToString() ?? string.Empty;
        }
    }
}