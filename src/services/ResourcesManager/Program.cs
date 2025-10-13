using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.Application.ExternalServices;
using ResourcesManager.Infrastructure.DB;
using Services.ResourcesManager.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContextFactory<ResourceContext>(
    (optionsBuilder) =>
    {
        optionsBuilder.UseNpgsql(
            builder.Configuration.GetConnectionString("ResourcesDb") 
            ?? @"Host=localhost;Username=postgres;Password=five0_rm;Database=five0_resources");
    });

builder.Services.AddDbContextFactory<TenantContext>(
    (optionsBuilder) =>
    {
        optionsBuilder.UseNpgsql(
            builder.Configuration.GetConnectionString("TenantsDb") 
            ?? @"Host=localhost;Username=postgres;Password=five0_rm;Database=five0_tenants");
    });    

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// App services
builder.Services.AddSingleton<IDatabaseQuery, DbServiceQuery>();
builder.Services.AddSingleton<IDatabaseCommand, DbServiceCommand>();

// External Services
builder.Services.AddTimeServiceClient();
builder.Services.AddSingleton<ITimeService, TimeServiceClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
