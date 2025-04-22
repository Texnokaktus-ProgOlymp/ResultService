using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Infrastructure;

public static class DiExtensions
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpcClient<ParticipantService.ParticipantServiceClient>(options => options.Address = configuration.GetConnectionStringUri(nameof(ParticipantService)));

        return services.AddScoped<IParticipantServiceClient, ParticipantServiceClient>();
    }

    private static Uri? GetConnectionStringUri(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name) is { } connectionString
            ? new Uri(connectionString)
            : null;
}
