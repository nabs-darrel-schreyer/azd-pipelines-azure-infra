using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AzdPipelinesAzureInfra.Persistence;

public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=TestSqlDb;Integrated Security=True;TrustServerCertificate=True;");
        return new TestDbContext(optionsBuilder.Options);
    }
}