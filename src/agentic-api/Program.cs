using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

var agent = CreateAgent(endpoint, deploymentName);

builder.AddAIAgent("AGUIAssistant", (_,_) => agent);

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// builder.AddSequentialWorkflow("my-workflow", [agent1Builder, agent2Builder])
//     .AddAsAIAgent();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.MapAGUI("/", agent);

if (builder.Environment.IsDevelopment())
{
    // Map DevUI endpoint to /devui
    app.MapDevUI();
}

static AIAgent CreateAgent(string endpoint, string deploymentName)
{
    var chatClient = new AzureOpenAIClient(
        new Uri(endpoint),
        new DefaultAzureCredential())
    .GetChatClient(deploymentName);

    AIAgent agent = chatClient.AsIChatClient().CreateAIAgent(
        name: "AGUIAssistant",
        instructions: "You are a helpful assistant.");

        return agent;
}

await app.RunAsync();