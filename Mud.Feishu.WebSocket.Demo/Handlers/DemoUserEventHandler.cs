// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using Mud.Feishu.WebSocket.Services;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Demo.Handlers;

/// <summary>
/// 演示用户事件处理器
/// </summary>
public class DemoUserEventHandler : IFeishuEventHandler
{
    private readonly ILogger<DemoUserEventHandler> _logger;
    private readonly DemoEventService _eventService;

    public DemoUserEventHandler(ILogger<DemoUserEventHandler> logger, DemoEventService eventService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    public string SupportedEventType => "contact.user.created_v3";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        //_logger.LogInformation("👤 [用户事件] 开始处理用户创建事件: {EventId}", eventData.EventId);

        try
        {
            // 解析用户数据
            var userData = ParseUserData(eventData);

            // 记录事件到服务
            await _eventService.RecordUserEventAsync(userData, cancellationToken);

            // 模拟业务处理
            await ProcessUserEventAsync(userData, cancellationToken);

            _logger.LogInformation("✅ [用户事件] 用户创建事件处理完成: 用户ID {UserId}, 用户名 {UserName}",
                userData.UserId, userData.UserName);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "❌ [用户事件] 处理用户创建事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private UserData ParseUserData(EventData eventData)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventData.Event?.ToString() ?? "{}");

            // 尝试从不同的JSON结构中解析用户信息
            var userElement = jsonElement.GetProperty("user");

            return new UserData
            {
                UserId = userElement.GetProperty("user_id").GetString() ?? "",
                UserName = userElement.GetProperty("name").GetString() ?? "",
                Email = TryGetProperty(userElement, "email") ?? "",
                Department = TryGetProperty(userElement, "department") ?? "",
                Phone = TryGetProperty(userElement, "phone") ?? "",
                Avatar = TryGetProperty(userElement, "avatar") ?? "",
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析用户数据失败");
            throw new InvalidOperationException("无法解析用户数据", ex);
        }
    }

    private async Task ProcessUserEventAsync(UserData userData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔄 [用户事件] 开始处理用户数据: {UserId}", userData.UserId);

        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 模拟用户数据处理：数据库存储、缓存更新、通知发送等
        if (string.IsNullOrWhiteSpace(userData.UserId))
        {
            throw new ArgumentException("用户ID不能为空");
        }

        // 模拟发送欢迎通知
        _logger.LogInformation("📧 [用户事件] 发送欢迎通知给用户: {UserName} ({Email})",
            userData.UserName, userData.Email);

        // 模拟更新统计信息
        _eventService.IncrementUserCount();

        await Task.CompletedTask;
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }
}

/// <summary>
/// 用户数据模型
/// </summary>
public class UserData
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Avatar { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ProcessedAt { get; init; }
}