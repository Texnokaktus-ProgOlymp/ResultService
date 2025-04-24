using Microsoft.EntityFrameworkCore;
using Serilog;
using Texnokaktus.ProgOlymp.ResultService.DataAccess;
using Texnokaktus.ProgOlymp.ResultService.Endpoints;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure;
using Texnokaktus.ProgOlymp.ResultService.Logic;
using Texnokaktus.ProgOlymp.ResultService.Services;
using Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddDataAccess(optionsBuilder => optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDb"))
                                                      .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()))
       .AddLogicServices()
       .AddScoped<IResultService, ResultService>()
       .AddGrpcClients(builder.Configuration);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

app.MapGrpcReflectionService();
app.MapGrpcService<ResultServiceImpl>();

app.MapResultEndpoints();

await app.RunAsync();
