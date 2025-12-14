var builder = DistributedApplication.CreateBuilder(args);

var aca = builder
    .AddAzureContainerAppEnvironment("aca-env");

var sqlServer = builder
    .AddAzureSqlServer("sql-server");

var testDb = sqlServer
    .AddDatabase("test-db");

var apiService = builder
    .AddProject<Projects.AzdPipelinesAzureInfra_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(testDb).WaitFor(testDb);

builder
    .AddProject<Projects.AzdPipelinesAzureInfra_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
