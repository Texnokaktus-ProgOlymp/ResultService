using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.JudgeService.DataAccess;
using Texnokaktus.ProgOlymp.ResultService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccess(optionsBuilder => optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDb"))
                                                               .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<GreeterService>();

await app.RunAsync();
