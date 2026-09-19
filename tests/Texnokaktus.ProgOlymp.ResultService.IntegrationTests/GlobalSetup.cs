using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public abstract class GlobalSetup
{
    protected CustomWebApplicationFactory Factory;

    [SetUp]
    public async Task Setup()
    {
        Factory = new();
        Factory.StartServer();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }

        await Factory.DisposeAsync();
    }
}
