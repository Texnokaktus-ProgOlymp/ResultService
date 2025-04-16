using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;

namespace Texnokaktus.ProgOlymp.ResultService.DataAccess;

public static class DiExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection serviceCollection,
                                                   Action<DbContextOptionsBuilder> optionsAction) =>
        serviceCollection.AddDbContext<AppDbContext>(optionsAction);
}
