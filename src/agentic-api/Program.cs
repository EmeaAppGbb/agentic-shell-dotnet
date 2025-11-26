using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using agentic_api.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

// Register IChatClient
builder.Services.AddSingleton(_ =>
    new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
        .GetChatClient(deploymentName)
        .AsIChatClient());

// Register the dummy workflow factory
builder.Services.AddSingleton<DummyWorkflowFactory>();

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// Get the dummy workflow and convert it to an agent
var dummyWorkflowFactory = app.Services.GetRequiredService<DummyWorkflowFactory>();
var dummyWorkflow = dummyWorkflowFactory.BuildWorkflow("DummyWorkflow");
var dummyAgent = dummyWorkflow.AsAgent(name: "DummyWorkflow");

app.MapOpenAIResponses();
app.MapOpenAIConversations();

// Map the dummy workflow agent to the default AGUI endpoint
app.MapAGUI("/", dummyAgent);

if (builder.Environment.IsDevelopment())
{
    // Map DevUI endpoint to /devui
    app.MapDevUI();
}


await app.RunAsync();