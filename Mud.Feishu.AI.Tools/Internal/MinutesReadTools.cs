// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Minutes 只读工具执行器（R5 / F-11：<c>minutes.get</c> / <c>minutes.get_artifacts</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么客户端是可空（软依赖）</b>：<c>FeishuToolDomainRegistrars</c> 的契约是
/// "客户端缺席 → 执行器缺席 → 注册器不注册"。若把 minutes 客户端声明为<b>硬依赖</b>，
/// 宿主未启用妙记 API 时会连带让本执行器整体缺席。声明为可空后：未启用时这两个工具
/// 软缺席（执行期给可执行错误），已启用时正常在位。
/// </para>
/// <para>
/// <b>S-13 教训</b>：首版实现用的是硬依赖，结果目录里<b>静默少了这两个工具</b>，
/// 而 MUDFT022/025 均为 Error 且构建干净 —— 说明"绑定生成"与"注册器装配"是两段，
/// 后者失败会被<b>软缺席机制静默吞掉</b>。改软依赖后问题消失。
/// </para>
/// </remarks>
internal sealed class MinutesReadTools(Mud.Feishu.IFeishuTenantV1MinutesMinute? minutesClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1MinutesMinute? _minutesClient = minutesClient;

    [FeishuToolHandler(typeof(IFeishuTenantMinutesGetTool))]
    public Task<FeishuToolResult> GetMinuteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGet);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetMinuteAsync(args.MinuteToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var minute = data.Minute;
                if (minute is null)
                {
                    // 空结果哨兵（F-5）：显式"没找到"，而不是返回空对象让模型猜。
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["minute_token"] = args.MinuteToken,
                        ["message"] = "未找到该妙记，请确认 minute_token 是否正确、以及当前身份是否有权限访问。",
                    };
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["minute_token"] = minute.Token ?? args.MinuteToken,
                    ["title"] = minute.Title,
                    ["url"] = minute.Url,
                    ["duration"] = minute.Duration,
                    ["create_time"] = minute.CreateTime,
                    ["owner_id"] = minute.OwnerId,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantMinutesGetArtifactsTool))]
    public Task<FeishuToolResult> GetMinuteArtifactsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGetArtifacts);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetArtifactsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await Require(executor.ToolName)
                .GetMinuteArtifactsAsync(args.MinuteToken, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var chapters = new JsonArray();
                foreach (var chapter in data.MinuteChapters ?? [])
                {
                    // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
                    chapters.AddNode(new JsonObject
                    {
                        ["title"] = chapter.Title,
                        ["start_ms"] = chapter.StartMs,
                        ["stop_ms"] = chapter.StopMs,
                        ["summary"] = chapter.SummaryContent,
                    });
                }

                var todos = new JsonArray();
                foreach (var todo in data.MinuteTodos ?? [])
                {
                    todos.AddNode(new JsonObject
                    {
                        // 与 body/content 同口径预截断（B-3 纪律：回填的文本一律不等长放任）。
                        ["content"] = ToolResultText.Truncate(todo.Content, PageSizes.MessagePreviewLength),
                        ["is_done"] = todo.IsDone,
                    });
                }

                var transcript = data.Transcript;
                var truncated = ToolResultText.Truncate(transcript, PageSizes.MessagePreviewLength);

                return new JsonObject
                {
                    ["minute_token"] = args.MinuteToken,
                    ["summary"] = data.Summary,
                    ["keywords"] = new JsonArray([.. (data.Keywords ?? []).Select(static k => (JsonNode?)k)]),
                    ["chapters"] = chapters,
                    ["todos"] = todos,

                    // 逐字稿可达数万字：截断并标记，避免"静默丢内容"——模型看到标记才知道还有更多。
                    ["transcript"] = truncated,
                    ["transcript_truncated"] = !string.Equals(truncated, transcript, StringComparison.Ordinal),
                };
            });
        });
    }

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private Mud.Feishu.IFeishuTenantV1MinutesMinute Require(string toolName)
        => _minutesClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1MinutesMinute——宿主须启用妙记（Minutes）API；"
                + "未启用时本工具不在工具列表中（软缺席，不影响其它域工具）");
}