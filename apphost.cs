

#:sdk Aspire.AppHost.Sdk@13.0.0
#:package Aspire.Hosting.JavaScript@13.0.0
#:package Aspire.Hosting.Azure.CognitiveServices@13.0.0
#:package Aspire.Hosting.Azure.AIFoundry@13.0.0-preview.1.25560.3

var builder = DistributedApplication.CreateBuilder(args);

var existingFoundryName = builder.AddParameter("existingFoundryName");
var existingFoundryResourceGroup = builder.AddParameter("existingFoundryResourceGroup");

var foundry = builder.AddAzureAIFoundry("foundry")
                     .AsExisting(existingFoundryName, existingFoundryResourceGroup);

var api = builder.AddCSharpApp("agentic-api", "./src/agentic-api")
    .WithReference(foundry);



var ui = builder.AddJavaScriptApp("agentic-ui", "./src/agentic-ui")
    .WithRunScript("dev")
    .WithNpm(installCommand: "ci")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
