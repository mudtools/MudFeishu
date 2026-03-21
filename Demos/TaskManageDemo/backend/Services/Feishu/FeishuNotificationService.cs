// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu;

namespace TaskManageDemo.Backend.Services.Feishu;

/// <summary>
/// 飞书通知服务实现
/// </summary>
public class FeishuNotificationService : IFeishuNotificationService
{
    private readonly IFeishuTenantV1Message _messageApi;
    private readonly ILogger<FeishuNotificationService> _logger;

    /// <summary>
    /// 初始化飞书通知服务
    /// </summary>
    public FeishuNotificationService(
        IFeishuTenantV1Message messageApi,
        ILogger<FeishuNotificationService> logger)
    {
        _messageApi = messageApi;
        _logger = logger;
    }

    /// <summary>
    /// 发送任务分配通知
    /// </summary>
    public async Task SendTaskAssignedNotificationAsync(
        string assigneeFeishuId,
        string taskTitle,
        string taskGuid,
        CancellationToken cancellationToken = default)
    {
        var content = new
        {
            zh_cn = new
            {
                title = "📋 新任务分配",
                elements = new object[]
                {
                    new
                    {
                        tag = "div",
                        text = new
                        {
                            tag = "lark_md",
                            content = $"**任务**: {taskTitle}\n\n**时间**: {DateTime.Now:yyyy-MM-dd HH:mm}"
                        }
                    },
                    new
                    {
                        tag = "action",
                        actions = new object[]
                        {
                            new
                            {
                                tag = "button",
                                text = new { tag = "plain_text", content = "查看任务" },
                                type = "primary",
                                url = $"https://feishu.cn/task/{taskGuid}"
                            }
                        }
                    }
                }
            }
        };

        await SendMessageAsync(assigneeFeishuId, content, cancellationToken);
    }

    /// <summary>
    /// 发送任务截止提醒
    /// </summary>
    public async Task SendTaskDueReminderAsync(
        string assigneeFeishuId,
        string taskTitle,
        string taskGuid,
        DateTime dueTime,
        CancellationToken cancellationToken = default)
    {
        var content = new
        {
            zh_cn = new
            {
                title = "⏰ 任务截止提醒",
                elements = new object[]
                {
                    new
                    {
                        tag = "div",
                        text = new
                        {
                            tag = "lark_md",
                            content = $"**任务**: {taskTitle}\n\n**截止时间**: {dueTime:yyyy-MM-dd HH:mm}"
                        }
                    },
                    new
                    {
                        tag = "action",
                        actions = new object[]
                        {
                            new
                            {
                                tag = "button",
                                text = new { tag = "plain_text", content = "查看任务" },
                                type = "primary",
                                url = $"https://feishu.cn/task/{taskGuid}"
                            }
                        }
                    }
                }
            }
        };

        await SendMessageAsync(assigneeFeishuId, content, cancellationToken);
    }

    /// <summary>
    /// 发送任务完成通知
    /// </summary>
    public async Task SendTaskCompletedNotificationAsync(
        string creatorFeishuId,
        string taskTitle,
        string taskGuid,
        CancellationToken cancellationToken = default)
    {
        var content = new
        {
            zh_cn = new
            {
                title = "✅ 任务已完成",
                elements = new object[]
                {
                    new
                    {
                        tag = "div",
                        text = new
                        {
                            tag = "lark_md",
                            content = $"**任务**: {taskTitle}\n\n**完成时间**: {DateTime.Now:yyyy-MM-dd HH:mm}"
                        }
                    },
                    new
                    {
                        tag = "action",
                        actions = new object[]
                        {
                            new
                            {
                                tag = "button",
                                text = new { tag = "plain_text", content = "查看任务" },
                                type = "primary",
                                url = $"https://feishu.cn/task/{taskGuid}"
                            }
                        }
                    }
                }
            }
        };

        await SendMessageAsync(creatorFeishuId, content, cancellationToken);
    }

    private async Task SendMessageAsync(string receiveId, object content, CancellationToken cancellationToken)
    {
        var request = new SendMessageRequest
        {
            ReceiveId = receiveId,
            MsgType = "interactive",
            Content = JsonSerializer.Serialize(content)
        };

        var result = await _messageApi.SendMessageAsync(request, receive_id_type: "user_id", cancellationToken: cancellationToken);

        if (result?.Data != null)
        {
            _logger.LogInformation("消息发送成功: {MessageId}", result.Data.MessageId);
        }
        else
        {
            _logger.LogWarning("消息发送失败: {Result}", JsonSerializer.Serialize(result));
        }
    }
}
