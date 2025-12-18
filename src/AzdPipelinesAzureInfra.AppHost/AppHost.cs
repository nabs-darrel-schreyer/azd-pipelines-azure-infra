using Azure.Provisioning.AppConfiguration;
using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

var aca = builder
    .AddAzureContainerAppEnvironment("aca-env");

var appconfig = builder
    .AddAzureAppConfiguration("appconfig")
    .ConfigureInfrastructure(infra =>
    {
        var appConfigStore = infra
            .GetProvisionableResources()
            .OfType<AppConfigurationStore>()
            .Single();

        appConfigStore.SkuName = "Free";
        appConfigStore.EnablePurgeProtection = false;
    })
    .RunAsEmulator(config =>
    {
        config.WithContainerName("local-appconfig");
        config.WithLifetime(ContainerLifetime.Persistent);
        config.WithEnvironment("Tenant:AnonymousAuthEnabled", "true");
        config.WithEnvironment("Authentication:Anonymous:AnonymousUserRole", "Owner");
    });

var sqlServer = builder
    .AddAzureSqlServer("sql-server")
    .ConfigureInfrastructure(sqlConfig =>
    {
        var azureResources = sqlConfig.GetProvisionableResources();
        var azureDb = azureResources.OfType<SqlDatabase>().Single();
        azureDb.Sku = new SqlSku() { Name = "Basic", Tier = "Basic", Capacity = 5 };
        azureDb.UseFreeLimit = false;
    })
    .RunAsContainer(config =>
    {
        config.WithDataVolume("localSqlDbDataVolume");
        config.WithContainerName("local-sql-db");
        config.WithLifetime(ContainerLifetime.Persistent);
        config.WithImage("mssql/server:2025-latest");  
    });

var testDb = sqlServer
    .AddDatabase("test-db", "TestSqlDb");

var dataMigration = builder
    .AddProject<Projects.AzdPipelinesAzureInfra_DataMigrations>("datamigrations")
    .WithReference(testDb).WaitFor(testDb)
    .WithReference(appconfig).WaitFor(appconfig);

var apiService = builder
    .AddProject<Projects.AzdPipelinesAzureInfra_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(appconfig).WaitFor(appconfig)
    .WithReference(testDb).WaitFor(testDb)
    .WithReference(dataMigration)
    .WaitForCompletion(dataMigration);

builder
    .AddProject<Projects.AzdPipelinesAzureInfra_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService).WaitFor(apiService);


builder.Build().Run();
