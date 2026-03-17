using FeishuWikiManager.Data;
using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
