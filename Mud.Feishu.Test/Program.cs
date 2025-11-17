using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
#if net8 || net9
builder.Services.AddSwaggerGen();
#endif

#if net10
builder.Services.AddOpenApi();
#endif

// 添加飞书服务
builder.Services.AddFeishuApiService("Feishu");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
#if net8 || net9
    app.UseSwagger();
    app.UseSwaggerUI();
#endif
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();