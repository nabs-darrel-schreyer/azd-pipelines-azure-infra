using AzdPipelinesAzureInfra.DataMigrations;
using AzdPipelinesAzureInfra.Persistence;

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

// Register IConfgurationClient based on connection string type
builder.Services.AddSingleton<IConfgurationClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString(Strings.AppConfigurationName);
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("App Configuration connection string not found.");
    }

    // Check if connection string contains localhost (emulator)
    if (connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) || 
        connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
    {
        var httpClient = new HttpClient();
        return new EmulatorConfigurationClient(connectionString, httpClient);
    }

    // Azure App Configuration - either URI with managed identity or full connection string
    if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) &&
        (uri.Scheme == "http" || uri.Scheme == "https"))
    {
        return new AzureConfigurationClient(uri);
    }

    // Full connection string with authentication
    return new AzureConfigurationClient(connectionString);
});

var host = builder.Build();

host.Run();


public static class Strings
{
    public const string AppConfigurationName = "appconfig";
}