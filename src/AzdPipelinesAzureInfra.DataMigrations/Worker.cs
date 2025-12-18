using AzdPipelinesAzureInfra.Persistence;
using Azure.Data.AppConfiguration;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AzdPipelinesAzureInfra.DataMigrations;

public class Worker(
    IServiceProvider serviceProvider,
    IConfgurationClient configurationClient,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity(
            "Running Data Migrations", ActivityKind.Client);

        try
        {
            await RunAppConfigurationSeedDataAsync(stoppingToken);

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            await RunSqlMigrationAsync(dbContext, stoppingToken);
            await RunSqlSeedDataAsync(dbContext, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private async Task RunAppConfigurationSeedDataAsync(
        CancellationToken cancellationToken)
    {
        var defaultKeyValues = new Dictionary<string, string>
        {
            { "TestKey", "<<default-value-TestKey>>" },
            { "NewKey", "<<default-value-NewKey>>" }
        };

        const string targetLabel = "AzdPipelines";

        foreach (var kvp in defaultKeyValues)
        {
            var selector = new SettingSelector
            {
                KeyFilter = kvp.Key,
                LabelFilter = targetLabel
            };
            try
            {
                var existing = await configurationClient
                    .GetConfigurationSettingsAsync(selector, cancellationToken)
                    .ToListAsync(cancellationToken);
                if (!existing.Any())
                {
                    var newConfig = new ConfigurationSetting(kvp.Key, kvp.Value, targetLabel);
                    await configurationClient.SetConfigurationSettingAsync(newConfig, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error checking existing configuration for key '{kvp.Key}'", ex);
            }
        }
    }

    private static async Task RunSqlMigrationAsync(
        TestDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static async Task RunSqlSeedDataAsync(
        TestDbContext dbContext, CancellationToken cancellationToken)
    {
        var rnd = new Random();

        // I expect that these items are added only once
        Guid[] ids = [
            new Guid("549b8034-909d-4f25-abed-48a9fdb24276"),
            new Guid("7e3f0f92-c31f-4735-b733-7b82e8c7f187"),
            new Guid("653a2397-f046-4d1c-9774-815fc7ec29f9")
            ];
        var people = new List<PersonEntity>();
        foreach (var id in ids)
        {
            people.Add(new PersonEntity
            {
                Id = id,
                Username = $"user{id}",
                FirstName = $"FirstName{id}",
                LastName = $"LastName{id}",
                YearOfBirth = rnd.Next(1885, 2025)
            });
        }

        // I expect this one is added every after a deployment.
        var newId = Guid.NewGuid();
        people.Add(new PersonEntity
        {
            Id = newId,
            Username = $"user{newId}",
            FirstName = $"FirstName{newId}",
            LastName = $"LastName{newId}",
            YearOfBirth = rnd.Next(1985, 2025)
        });

        var existingPeople = await dbContext.People
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var peopleToAdd = people
            .Where(p => !existingPeople.Any(ep => ep.Id == p.Id))
            .ToList();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            dbContext.People.AddRange(peopleToAdd);
            var rowsAdded = await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
