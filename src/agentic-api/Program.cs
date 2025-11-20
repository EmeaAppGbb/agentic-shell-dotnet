using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

var app = builder.Build();

var agent = CreateAgent(endpoint, deploymentName);

app.MapAGUI("/", agent);

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