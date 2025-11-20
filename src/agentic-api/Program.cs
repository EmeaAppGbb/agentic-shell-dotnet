using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

var app = builder.Build();

builder.AddAzureChatCompletionsClient(connectionName: "foundry")
       .AddChatClient("gpt5MiniDeployment");

var agent = CreateAgent(app.Services);

app.MapAGUI("/", agent);

static AIAgent CreateAgent(IServiceProvider serviceProvider)
{
    var chatClient = serviceProvider.GetRequiredService<IChatClient>();
    
    return chatClient.CreateAIAgent(
        name: "AGUIAssistant",
        instructions: "You are a helpful assistant.");
}

await app.RunAsync();