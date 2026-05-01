using System.Text;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Services;
using FeishuFileServer.Services.Feishu;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FeishuFileServer.Extensions;

/// <summary>
/// 服务集合扩展方法类
/// 提供服务注册的扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加应用程序服务
    /// 包括数据库上下文、JWT认证和其他基础服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加数据库上下文
        services.AddDbContext<FeishuFileDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
        });

        // 添加JWT认证
        services.AddJwtAuthentication(configuration);

        // 添加控制器
        services.AddControllers();

        // 添加Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    /// <summary>
    /// 添加JWT认证服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettingsSection = configuration.GetSection("JwtSettings");
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
        if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
        {
            throw new InvalidOperationException("JWT配置未找到或Secret为空，请检查appsettings.json中的JwtSettings配置");
        }

        services.Configure<JwtSettings>(jwtSettingsSection);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    /// <summary>
    /// 添加飞书服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFeishuApp(configuration, "FeishuApps");

        services.CreateFeishuServicesBuilder()
            .AddModules(FeishuModule.Organization)
            .AddModules(FeishuModule.Drive)
            .Build();

        services.AddScoped<IFeishuDriveService, FeishuDriveService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IVersionService, VersionService>();
        services.AddScoped<IRecycleBinService, RecycleBinService>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<IOperationLogService, OperationLogService>();
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IChunkUploadService, ChunkUploadService>();
        services.AddScoped<IFeishuSyncService, FeishuSyncService>();

        return services;
    }

    /// <summary>
    /// 添加CORS配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("CorsSettings").Get<CorsSettings>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(corsSettings?.AllowedOrigins?.ToArray() ?? new[] { "http://localhost:3000" })
                    .WithMethods(corsSettings?.AllowedMethods?.ToArray() ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" })
                    .WithHeaders(corsSettings?.AllowedHeaders?.ToArray() ?? new[] { "*" })
                    .AllowCredentials();
            });
        });

        return services;
    }
}
