using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI.Chat;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

var app = builder.Build();

var endpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"];
var credential = new DefaultAzureCredential();
AIProjectClient projectClient = new AIProjectClient(new Uri(endpoint),credential );

var connection = projectClient.GetConnection(typeof(AzureOpenAIClient).FullName!);

if (!connection.TryGetLocatorAsUri(out Uri uri) || uri is null)
{
    throw new InvalidOperationException("Invalid URI.");
}
uri = new Uri($"https://{uri.Host}");

AzureOpenAIClient azureOpenAIClient = new AzureOpenAIClient(uri, credential);
ChatClient chatClient = azureOpenAIClient.GetChatClient(deploymentName: "gpt5mini");

AIAgent agent = chatClient.AsIChatClient().CreateAIAgent(
    name: "AGUIAssistant",
    instructions: "You are a helpful assistant.");

// // Map the AG-UI agent endpoint
app.MapAGUI("/", agent);

await app.RunAsync();