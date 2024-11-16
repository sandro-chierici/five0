using JsonIngestion.Implementation;
using JsonIngestion.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// add console logger service
builder.Services.AddLogging(config =>
{
    config.AddConsole();
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// set my services
builder.Services.AddSingleton<ITokenPersistence, MemoryTokenPersistence>();
builder.Services.AddSingleton<DataProcessor>();


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
