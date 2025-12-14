using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AzdPipelinesAzureInfra.ApiService.Persistence;

public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=TestSqlDatabase;Integrated Security=True;TrustServerCertificate=True;");
        return new TestDbContext(optionsBuilder.Options);
    }
}