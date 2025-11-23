using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.Application.Configuration;
using ResourcesManager.Business.Application.ExternalServices;
using ResourcesManager.Infrastructure.DB;
using Services.ResourcesManager.Infrastructure.Services;
using ResourcesManager.Business.Application.ExternalServices.SyncService;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContextFactory<ResourceContext>(
    (optionsBuilder) =>
    {
        optionsBuilder.UseNpgsql(
            builder.Configuration.GetConnectionString("ResourcesDb") 
            ?? @"Host=localhost;Username=postgres;Password=five0_rm;Database=five0_resources");
    });

builder.Services.Configure<Five0Config>(builder.Configuration.GetSection("Five0"));
builder.Services.Configure<ResourceDataServiceConfig>(builder.Configuration.GetSection("ResourceDataService"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

// App services
builder.Services.AddSingleton<IDatabaseQuery, DbServiceQuery>();
builder.Services.AddSingleton<IDatabaseCommand, DbServiceCommand>();

// External Services
builder.Services.AddTimeServiceClient(builder.Configuration);
builder.Services.AddSingleton<ISyncService, SyncServiceClient>();

builder.Services.AddEventServiceClient(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
