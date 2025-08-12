using Microsoft.AspNetCore.Mvc.Testing;
using LocalizationManager.Web.DataLayer;
using LocalizationManager.Web.DataLayer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalizationManager.Web.Tests;

public class ApiTests : IClassFixture<ApiTests.TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public ApiTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test factory that replaces the real DB with EF Core InMemory and seeds a small dataset.
    /// </summary>
    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // 1) Remove all existing registrations of LocalizationDbContext & its options (e.g., Npgsql)
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(LocalizationDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<LocalizationDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                ).ToList();
                foreach (var d in descriptors)
                    services.Remove(d);

                // 2) Add InMemory provider for tests
                services.AddDbContext<LocalizationDbContext>(opts =>
                    opts.UseInMemoryDatabase("loc-tests-db"));

                // 3) Build provider and seed small dataset
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalizationDbContext>();

                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();

                if (!ctx.Cultures.Any())
                    ctx.Cultures.Add(new CultureEntity { Code = "en-US", DisplayName = "English (US)" });

                if (!ctx.LocalizationRecords.Any())
                {
                    ctx.LocalizationRecords.Add(new LocalizationRecordEntity
                    {
                        Id = Guid.NewGuid(),
                        Env = "DEV",
                        CultureCode = "en-US",
                        GroupName = "Core.CommonStrings",
                        Key = "Home.Title",
                        Value = "Hello",
                        UpdatedUtc = DateTime.UtcNow
                    });
                }

                ctx.SaveChanges();
            });
        }
    }
}
