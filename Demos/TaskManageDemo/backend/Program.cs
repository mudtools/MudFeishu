// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mud.Feishu.Authentication;
using Serilog;
using Serilog.Events;
using System.Text;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Extensions;
using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Auth;
using TaskManageDemo.Backend.Utils;
using FluentValidation;
using FluentValidation.AspNetCore;
using TaskManageDemo.Backend.Services.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Mud.Feishu", LogEventLevel.Debug)
    .MinimumLevel.Override("Mud.HttpUtils", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 7,
        encoding: System.Text.Encoding.UTF8
    ));

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 配置 OAuth 选项
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));

// 配置 JWT 认证
var oauthOptions = builder.Configuration.GetSection("OAuth").Get<OAuthOptions>() ?? new OAuthOptions();
var jwtSecret = oauthOptions.Jwt.Secret;
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT密钥未配置，请在 appsettings.json 中配置 OAuth:Jwt:Secret");
}

// 强制检查 JWT 密钥强度（至少32个字符）- 测试环境跳过
var isTesting = builder.Environment.IsEnvironment("Testing");
if (!isTesting && jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT密钥必须至少32个字符，当前长度: " + jwtSecret.Length);
}

// 检查密钥复杂度（不能是简单重复字符或常见弱密码）- 测试环境跳过
if (!isTesting && IsWeakJwtSecret(jwtSecret))
{
    throw new InvalidOperationException("JWT密钥太弱，请使用包含大小写字母、数字和特殊字符的复杂密钥");
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

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddFeishuServices(builder.Configuration);
builder.Services.AddWebhookServices(builder.Configuration);
builder.Services.AddBusinessServices(builder.Configuration);

// 添加飞书用户上下文服务
builder.Services.AddFeishuUserContext(o =>
{
    o.OpenIdClaimType = "open_id";
    o.UnionIdClaimType = "union_id";
    o.UserIdClaimType = "user_id";
    o.NameClaimType = "name";
    o.EnableSensitiveLog = true;
});

// 添加 FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加敏感数据脱敏服务
builder.Services.AddSingleton<ISensitiveDataMasker, SensitiveDataMasker>();

builder.Services.AddHealthChecks()
    .AddSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=TaskManage.db",
        name: "database",
        tags: new[] { "db", "sqlite" })
    .AddCheck<FeishuApiHealthCheck>("feishu_api", tags: new[] { "external", "feishu" })
    .AddCheck<DiskSpaceHealthCheck>("disk_space", tags: new[] { "system" })
    .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "system" });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskManage API",
        Version = "v1",
        Description = "任务分配与跟踪管理系统 API"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddCors(options =>
{
    // 生产环境使用严格策略
    options.AddPolicy("ProductionPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173", "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
              .WithHeaders("Authorization", "Content-Type", "X-Request-ID", "X-Correlation-ID")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });

    // 开发环境使用宽松策略
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskManageDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbPath = builder.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(dbPath))
        {
            var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(dbPath, @"Data Source=(.+?)(?:;|$)");
            if (dataSourceMatch.Success)
            {
                var dbFilePath = dataSourceMatch.Groups[1].Value;
                var dbDirectory = Path.GetDirectoryName(dbFilePath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    logger.LogInformation("创建数据库目录: {Directory}", dbDirectory);
                }
            }
        }

        // 生产环境使用 Migrate() 而不是 EnsureCreated()
        if (app.Environment.IsProduction())
        {
            logger.LogInformation("应用数据库迁移...");
            await dbContext.Database.MigrateAsync();
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        // 初始化权限数据
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        await permissionService.InitializePermissionsAsync();

        // 初始化默认角色
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
        await roleService.InitializeDefaultRolesAsync();

        // 初始化管理员账号
        var localAuthService = scope.ServiceProvider.GetRequiredService<ILocalAuthService>();
        await localAuthService.InitializeAdminAccountAsync();

        logger.LogInformation("数据库初始化完成");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库初始化失败");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRequestLogging();
app.UseRequestValidation();
app.UseApiVersioning();
app.UseRateLimiting(new RateLimitOptions
{
    Window = TimeSpan.FromMinutes(1),
    MaxRequests = 200,
    Enabled = !app.Environment.IsDevelopment()
});
app.UseGlobalExceptionHandler();

// CORS 中间件必须在认证之前
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("ProductionPolicy");
}

// 认证和授权中间件
app.UseAuthentication();

if (!isTesting)
{
    app.UseFeishuUserAuthentication(); // 飞书用户认证中间件
}

app.UseAuthorization();

if (!isTesting)
{
    // 原有的飞书认证中间件（保留用于向后兼容）
    app.UseFeishuAuthentication();
    app.UseFeishuAuthorization();
}

app.UseFeishuWebhook();
app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.Run();

// 使 Program 类对测试项目可见
public partial class Program
{
    /// <summary>
    /// 检查 JWT 密钥是否为弱密码
    /// </summary>
    private static bool IsWeakJwtSecret(string secret)
    {
        // 检查是否为重复字符
        if (secret.Distinct().Count() <= 3)
            return true;

        // 检查常见弱密码模式
        var weakPatterns = new[]
        {
            "password", "123456", "secret", "admin", "default",
            "your_jwt_secret", "jwt_secret", "token_secret"
        };

        foreach (var pattern in weakPatterns)
        {
            if (secret.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 检查是否全部为数字或全部为字母
        if (secret.All(char.IsDigit) || secret.All(char.IsLetter))
            return true;

        return false;
    }
}
