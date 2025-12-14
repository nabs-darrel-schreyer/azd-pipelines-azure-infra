using CommunityToolkit.Aspire.Hosting.PowerShell;

var builder = DistributedApplication.CreateBuilder(args);

var aca = builder
    .AddAzureContainerAppEnvironment("aca-env");

var sqlServer = builder
    .AddAzureSqlServer("sql-server")
    .RunAsContainer();

var testDb = sqlServer
    .AddDatabase("test-db");

var sqlMigration = builder
    .AddProject<Projects.AzdPipelinesAzureInfra_SqlMigrations>("sqlmigrations")
    .WithReference(testDb).WaitFor(testDb);

var apiService = builder
    .AddProject<Projects.AzdPipelinesAzureInfra_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(testDb).WaitFor(testDb)
    .WithReference(sqlMigration)
    .WaitForCompletion(sqlMigration);

builder
    .AddProject<Projects.AzdPipelinesAzureInfra_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService).WaitFor(apiService);


builder.Build().Run();
