using Chat_Room.Server.Service_Class;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 註冊 SignalR 服務
builder.Services.AddSignalR();

// 註冊 CORS 跨網域存取

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:52020") // ?? Vue 啟動的網址
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // ?? SignalR 憑證傳遞
    });
});


var app = builder.Build();

// 啟用 CORS  middleware（必須放在 UseAuthorization 之前）
app.UseCors();

app.UseDefaultFiles();
app.UseStaticFiles();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 對應 SignalR Hub 的路由位址
// 這樣前端才能透過 http://localhost:xxxx/hubs/chat 連進來
app.MapHub<ChatHub>("/hubs/chat");

app.MapFallbackToFile("/index.html");

app.Run();
