var builder = WebApplication.CreateBuilder(args);

// Add SignalR service
builder.Services.AddSignalR();

var app = builder.Build();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

app.Run();
