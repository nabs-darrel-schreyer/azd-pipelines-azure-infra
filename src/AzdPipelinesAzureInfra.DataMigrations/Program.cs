using AzdPipelinesAzureInfra.DataMigrations;
using AzdPipelinesAzureInfra.Persistence;
using Nabs.Launchpad.Core.SeedData;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddAzureAppConfiguration(Strings.AppConfigurationName, configureOptions: options =>
{
    options.Select("*", "AzdPipelines");
});

builder.AddSqlServerDbContext<TestDbContext>("test-db");

builder.AddDataMigrationServices(Strings.AppConfigurationName);

var host = builder.Build();

host.Run();


public static class Strings
{
    public const string AppConfigurationName = "appconfig";
}