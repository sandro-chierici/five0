using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Contracts;
using ResourcesManager.Implements.DB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContextFactory<ResourceContext>(
    (optionsBuilder) =>
    {
        optionsBuilder.UseNpgsql(
            builder.Configuration.GetConnectionString("ResourcesManagerDb") 
            ?? @"Host=localhost;Username=postgres;Password=five0_rm;Database=five0_rm");
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// App services
builder.Services.AddSingleton<IDatabaseQuery, DbServiceQuery>();
builder.Services.AddSingleton<IDatabaseCommand, DbServiceCommand>();

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
