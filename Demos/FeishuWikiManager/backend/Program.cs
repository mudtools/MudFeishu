// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Data;
using FeishuWikiManager.Filters;
using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<SetUserContextFilter>();
})
.AddJsonOptions(options =>
{
    // 使用 camelCase 命名策略
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // 允许反序列化时不区分大小写
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));

var oauthOptions = builder.Configuration.GetSection("OAuth").Get<OAuthOptions>() ?? new OAuthOptions();
builder.Services.AddSingleton<IStateStorageService>(_ =>
    new StateStorageService(TimeSpan.FromMinutes(oauthOptions.StateExpirationMinutes)));

builder.Services.AddSingleton<IJwtTokenService>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OAuthOptions>>().Value;
    return new JwtTokenService(options.Jwt);
});

var jwtSecret = oauthOptions.Jwt.Secret;
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT密钥未配置");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = oauthOptions.Jwt.Issuer,
            ValidAudience = oauthOptions.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWikiService, WikiService>();

builder.Services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<SetUserContextFilter>();

builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

builder.Services.CreateFeishuServicesBuilder()
    .AddModules(
        FeishuModule.Organization,
        FeishuModule.Wiki
    )
    .Build()
    .AddLogging(options => options.AddConsole());

builder.Services.AddHostedService<StateCleanupService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar"));
}

app.UseHttpsRedirection();
app.UseCors("AllowVueDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public class StateCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public StateCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var stateStorage = scope.ServiceProvider.GetRequiredService<IStateStorageService>();
                stateStorage.CleanExpiredStates();
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理过期state时发生错误: {ex.Message}");
            }
        }
    }
}
