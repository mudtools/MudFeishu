// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuOAuthDemo.Models;
using FeishuOAuthDemo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Text;

// 如果未显式设置环境变量，默认使用 Development（适用于从 bin 目录直接运行的场景）
// 通过 dotnet run 启动时 launchSettings.json 会自动设置 ASPNETCORE_ENVIRONMENT=Development
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
        retainedFileCountLimit: 7, // 保留 7 天的日志
        encoding: System.Text.Encoding.UTF8
    ));

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 添加控制器
builder.Services.AddControllers();

// 添加OpenAPI支持
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// 添加CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 绑定配置
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));

// 添加State存储服务
var oauthOptions = builder.Configuration.GetSection("OAuth").Get<OAuthOptions>() ?? new OAuthOptions();
builder.Services.AddSingleton<IStateStorageService>(_ =>
    new StateStorageService(TimeSpan.FromMinutes(oauthOptions.StateExpirationMinutes)));

// 添加JWT令牌服务
builder.Services.AddSingleton<IJwtTokenService>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OAuthOptions>>().Value;
    return new JwtTokenService(options.Jwt);
});

// 添加用户服务
builder.Services.AddSingleton<IUserService, UserService>();

// 添加JWT认证
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

// 添加飞书应用服务
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册API服务
builder.Services.CreateFeishuServicesBuilder()
    .AddModules(FeishuModule.Organization)
    .Build();

// 添加飞书用户上下文服务
builder.Services.AddFeishuUserContext(o =>
{
    o.OpenIdClaimType = "open_id";
    o.UnionIdClaimType = "union_id";
    o.UserIdClaimType = "user_id";
    o.NameClaimType = "name";
    o.EnableSensitiveLog = true;
});


// 添加后台服务清理过期的state
builder.Services.AddHostedService<StateCleanupService>();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar"));
}

// 仅在生产环境启用 HTTPS 重定向（开发环境使用 HTTP，避免端口未配置警告）
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowVueDev");

app.UseAuthentication();
app.UseFeishuUserAuthentication(); // 飞书用户认证中间件
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// State清理后台服务
/// </summary>
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
                // 等待5分钟后清理过期的state
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var stateStorage = scope.ServiceProvider.GetRequiredService<IStateStorageService>();
                stateStorage.CleanExpiredStates();
            }
            catch (TaskCanceledException)
            {
                // 正常关闭，无需处理
                break;
            }
            catch (OperationCanceledException)
            {
                // 正常关闭，无需处理
                break;
            }
            catch (Exception ex)
            {
                // 记录其他异常，但继续运行
                Console.WriteLine($"清理过期state时发生错误: {ex.Message}");
            }
        }
    }
}
