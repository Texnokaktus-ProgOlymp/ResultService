using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using Texnokaktus.ProgOlymp.OpenTelemetry;
using Texnokaktus.ProgOlymp.ResultService.Converters;
using Texnokaktus.ProgOlymp.ResultService.DataAccess;
using Texnokaktus.ProgOlymp.ResultService.Endpoints;
using Texnokaktus.ProgOlymp.ResultService.Extensions;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure;
using Texnokaktus.ProgOlymp.ResultService.Logic;
using Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddDataAccess(optionsBuilder => optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDb"))
                                                      .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()))
       .AddLogicServices();

var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(builder.Configuration.GetConnectionString("DefaultRedis")!);
builder.Services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

builder.Services
       .AddDataProtection(options => options.ApplicationDiscriminator = Assembly.GetEntryAssembly()?.GetName().Name)
       .PersistKeysToStackExchangeRedis(connectionMultiplexer);

builder.Services.AddGrpcClients(builder.Configuration);

builder.Services.AddOpenApi(options => options.AddSchemaTransformer<SchemaTransformer>());
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddTexnokaktusOpenTelemetry(builder.Configuration, "ResultService", null, null);

builder.Services
       .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        })
       .AddConfiguredJwtBearer(builder.Configuration);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    app.MapGrpcReflectionService();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ResultServiceImpl>();

app.MapGroup("api")
   .MapResultEndpoints();

await app.RunAsync();
