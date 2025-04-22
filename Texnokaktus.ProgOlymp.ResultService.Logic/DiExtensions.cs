using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic;

public static class DiExtensions
{
    public static IServiceCollection AddLogicServices(this IServiceCollection services) =>
        services.AddScoped<IParticipantService, ParticipantService>()
                .AddScoped<IResultService, Services.ResultService>();
}
