using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

[NonParallelizable]
public abstract class SetupBase
{
    [SetUp]
    public async Task Setup()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
