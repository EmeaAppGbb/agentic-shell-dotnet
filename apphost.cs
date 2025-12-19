

#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.JavaScript@13.1.0
#:package Aspire.Hosting.Azure.CognitiveServices@13.1.0
#:package Aspire.Hosting.Azure.AIFoundry@13.1.0-preview.1.25616.3
#:package Aspire.Hosting.Azure.CosmosDB@13.1.0
#:package Aspire.Hosting.Python@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

var openAiEndpoint = builder.AddParameter("openAiEndpoint");
var openAiDeployment = builder.AddParameter("openAiDeployment");
var imageModelDeployment = builder.AddParameter("imageModelDeployment");

var cosmosName = builder.AddParameter("cosmosName");
var cosmosResourceGroup = builder.AddParameter("cosmosResourceGroup");

var cosmos = builder.AddAzureCosmosDB("cosmos-db")
    .AsExisting(cosmosName, cosmosResourceGroup);

var api = builder.AddCSharpApp("agentic-api", "./src/agentic-api")
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", openAiEndpoint)
    .WithEnvironment("AZURE_OPENAI_DEPLOYMENT_NAME", openAiDeployment)
    .WithEnvironment("AZURE_IMAGE_MODEL_DEPLOYMENT_NAME", imageModelDeployment)
    .WithReference(cosmos);

builder.AddJavaScriptApp("agentic-ui", "./src/agentic-ui")
    .WithRunScript("dev")
    .WithNpm(installCommand: "ci")
    .WithEnvironment("AGENT_API_URL", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Documentation site using MkDocs
builder.AddPythonModule("docs", "./specs", "mkdocs")
    .WithArgs("serve", "--dev-addr", "0.0.0.0:8100")
    .WithHttpEndpoint(targetPort: 8100, name: "http")
    .ExcludeFromManifest();

builder.Build().Run();
