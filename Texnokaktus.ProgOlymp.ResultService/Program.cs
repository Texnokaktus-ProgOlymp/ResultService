using Microsoft.EntityFrameworkCore;
using Serilog;
using Texnokaktus.ProgOlymp.ResultService.DataAccess;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure;
using Texnokaktus.ProgOlymp.ResultService.Logic;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddDataAccess(optionsBuilder => optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDb"))
                                                      .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()))
       .AddLogicServices()
       .AddGrpcClients(builder.Configuration);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

app.MapGrpcReflectionService();
app.MapGrpcService<ResultServiceImpl>();

await app.RunAsync();
