using Microsoft.EntityFrameworkCore;

namespace AzdPipelinesAzureInfra.ApiService.Persistence;

public sealed class TestDbContext(
    DbContextOptions<TestDbContext> options)
    : DbContext(options)
{
    public DbSet<PersonEntity> People => Set<PersonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("test");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestDbContext).Assembly);
    }
}
