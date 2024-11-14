using JsonIngestion.Dependencies;
using JsonIngestion.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

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
