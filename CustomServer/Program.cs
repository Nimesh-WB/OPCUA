using CustomServer.Services;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<OpcUaServerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

var opcUaServerService = app.Services.GetRequiredService<OpcUaServerService>();
await opcUaServerService.StartServer();

app.Run();
