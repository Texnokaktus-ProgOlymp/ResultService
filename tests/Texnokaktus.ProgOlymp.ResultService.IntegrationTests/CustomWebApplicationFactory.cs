using System.Data.Common;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using GRPC = Texnokaktus.ProgOlymp.Common.Contracts.Grpc;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public readonly ParticipantService.ParticipantServiceClient ParticipantServiceClientMock =
        Substitute.For<ParticipantService.ParticipantServiceClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
           .ConfigureServices(services =>
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    services.RemoveAll<DbConnection>();

                    services.AddSingleton<DbConnection>(_ =>
                        {
                            var connection = new SqliteConnection("DataSource=:memory:");
                            connection.Open();

                            return connection;
                        }
                    );

                    services.AddDbContext<AppDbContext>((container, options) =>
                        {
                            var connection = container.GetRequiredService<DbConnection>();
                            options.UseSqlite(connection);
                        }
                    );

                    services.RemoveAll<ParticipantService.ParticipantServiceClient>();
                    services.AddSingleton(ParticipantServiceClientMock);

                    SubstituteExtensions.Participants.Init(ParticipantServiceClientMock);
                }
            )
           .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                }
            );

        builder.UseEnvironment("Testing");
        Environment.SetEnvironmentVariable("SERVICE_NAME", "result-service");

        base.ConfigureWebHost(builder);
    }

    public GRPC.Results.ResultService.ResultServiceClient CreateResultServiceClient() => new(CreateGrpcChannel());

    public GRPC.Results.ResultQueryingService.ResultQueryingServiceClient CreateResultQueryingServiceClient() => new(CreateGrpcChannel());

    private GrpcChannel CreateGrpcChannel()
    {
        var httpClient = CreateClient();

        return GrpcChannel.ForAddress(
            httpClient.BaseAddress!,
            new()
            {
                HttpClient = httpClient
            }
        );
    }
}
