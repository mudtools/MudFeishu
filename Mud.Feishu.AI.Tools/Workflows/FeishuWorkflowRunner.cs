// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Tools.Catalog;

namespace Mud.Feishu.AI.Tools.Workflows;

/// <summary>
/// 工作流执行器：顺序执行步骤、失败即停、产出「步骤轨迹 + 聚合报告」。
/// </summary>
/// <remarks>
/// <para>
/// <b>幂等与重入</b>：只读链天然可重跑；<c>start_at</c> 让"第 3 步失败"的调用可以从第 3 步续跑，
/// 而不是从第 1 步重来（长逐字稿 + 大任务列表时这一步能省掉大部分 token 与配额）。
/// </para>
/// <para>
/// <b>失败语义</b>：失败步骤的错误文本原样进入轨迹，调用方拿到的是<b>失败结果</b>（带 <c>resume_from</c>），
/// 不是"看起来成功的半份报告"——半份报告最容易被模型当成完整结论继续推理。
/// </para>
/// <para>
/// <b>嵌套调用的边界</b>：步骤通过注册表拿到的 handler <b>就是</b> <c>FeishuToolBinding</c> 包装后的执行链，
/// 因此授权、令牌预检、结果净化、审计、重试、预算全部照旧生效；工作流本身不做任何特权提升。
/// </para>
/// </remarks>
/// <param name="registry">工具注册表（用于解析步骤工具）。</param>
internal sealed class FeishuWorkflowRunner(FeishuToolRegistry registry)
{
    private readonly FeishuToolRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    /// <summary>执行工作流。</summary>
    /// <param name="definition">工作流定义。</param>
    /// <param name="parameters">工作流入参（键为参数名）。</param>
    /// <param name="context">执行上下文。</param>
    /// <param name="startAt">起始步骤（1 起）。</param>
    /// <param name="dryRun">是否只列计划不执行。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>执行结论（步骤轨迹 + 报告 + 续跑坐标）。</returns>
    public async Task<FeishuWorkflowRun> RunAsync(
        FeishuWorkflowDefinition definition,
        IReadOnlyDictionary<string, object?> parameters,
        FeishuToolContext context,
        int startAt,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var outcomes = new List<FeishuWorkflowStepOutcome>(definition.Steps.Count);
        var data = new Dictionary<int, JsonNode?>();
        int? resumeFrom = null;

        foreach (var step in definition.Steps.OrderBy(static s => s.Index))
        {
            if (step.Index < startAt)
            {
                outcomes.Add(new FeishuWorkflowStepOutcome(
                    step.Index, step.ToolName, step.Purpose, FeishuWorkflowStepStatuses.Skipped, 0, null, null));
                continue;
            }

            if (dryRun)
            {
                outcomes.Add(new FeishuWorkflowStepOutcome(
                    step.Index, step.ToolName, step.Purpose, FeishuWorkflowStepStatuses.Planned, 0, null, null));
                continue;
            }

            if (!_registry.TryGet(step.ToolName, out var tool)
                || !_registry.EnabledTools.Any(t => string.Equals(t.Name, step.ToolName, StringComparison.Ordinal)))
            {
                outcomes.Add(new FeishuWorkflowStepOutcome(
                    step.Index,
                    step.ToolName,
                    step.Purpose,
                    FeishuWorkflowStepStatuses.Failed,
                    0,
                    $"步骤工具未启用或未注册：{step.ToolName}（白名单是宿主策略边界，工作流不得旁路）",
                    null));
                resumeFrom = step.Index;
                break;
            }

            var stopwatch = Stopwatch.StartNew();
            var result = await tool!.Handler(step.Bind(parameters), context, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (result.Error is not null)
            {
                outcomes.Add(new FeishuWorkflowStepOutcome(
                    step.Index,
                    step.ToolName,
                    step.Purpose,
                    FeishuWorkflowStepStatuses.Failed,
                    stopwatch.ElapsedMilliseconds,
                    result.Text,
                    null));
                resumeFrom = step.Index;
                break;
            }

            var parsed = ParseStepData(result.Text);
            data[step.Index] = parsed;
            outcomes.Add(new FeishuWorkflowStepOutcome(
                step.Index,
                step.ToolName,
                step.Purpose,
                FeishuWorkflowStepStatuses.Ok,
                stopwatch.ElapsedMilliseconds,
                null,
                parsed));
        }

        var report = dryRun || resumeFrom is not null
            ? (JsonObject?)null
            : definition.Compose(data);

        return new FeishuWorkflowRun(definition, outcomes, report, resumeFrom, dryRun);
    }

    private static JsonNode? ParseStepData(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：步骤结果是工具投影的自由文本（非 JSON 也不影响编排），
            // 把它当字符串保留即可——上抛会让"某步返回纯文本"变成整链失败，语义更差。
            return JsonValue.Create(text);
        }
    }
}

/// <summary>一次工作流执行的结论。</summary>
/// <param name="Definition">定义。</param>
/// <param name="Outcomes">步骤轨迹（含被跳过/预演的步骤）。</param>
/// <param name="Report">聚合报告（预演或中途失败时为 <see langword="null"/>）。</param>
/// <param name="ResumeFrom">续跑坐标（成功为 <see langword="null"/>）。</param>
/// <param name="DryRun">是否预演。</param>
internal sealed record FeishuWorkflowRun(
    FeishuWorkflowDefinition Definition,
    IReadOnlyList<FeishuWorkflowStepOutcome> Outcomes,
    JsonObject? Report,
    int? ResumeFrom,
    bool DryRun)
{
    /// <summary>是否失败（有中断即失败；跳过/预演不算）。</summary>
    public bool Failed => ResumeFrom is not null;
}

/// <summary>从工具结果 JSON 中容错取值（不假设投影层级的深浅）。</summary>
internal static class JsonScan
{
    /// <summary>深拷贝（<see langword="null"/> 透传）。</summary>
    /// <param name="node">源节点。</param>
    /// <returns>副本。</returns>
    public static JsonNode? Clone(JsonNode? node) => node?.DeepClone();

    /// <summary>递归查找布尔属性。</summary>
    /// <param name="node">根节点。</param>
    /// <param name="property">属性名。</param>
    /// <returns>属性值；未找到为 <see langword="null"/>。</returns>
    public static bool? TryReadBool(JsonNode? node, string property)
        => Find(node, property) is JsonValue value && value.TryGetValue<bool>(out var result) ? result : null;

    /// <summary>递归查找整数属性。</summary>
    /// <param name="node">根节点。</param>
    /// <param name="property">属性名。</param>
    /// <returns>属性值；未找到为 <see langword="null"/>。</returns>
    public static int? TryReadInt(JsonNode? node, string property)
        => Find(node, property) is JsonValue value && value.TryGetValue<int>(out var result) ? result : null;

    /// <summary>递归查找 <c>items</c> 数组并取元素数（找不到按 0 计，不臆造）。</summary>
    /// <param name="node">根节点。</param>
    /// <returns>元素数。</returns>
    public static int CountItems(JsonNode? node) => Find(node, "items") is JsonArray array ? array.Count : 0;

    private static JsonNode? Find(JsonNode? node, string property)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.TryGetPropertyValue(property, out var direct))
                {
                    return direct;
                }

                foreach (var child in obj)
                {
                    if (Find(child.Value, property) is { } nested)
                    {
                        return nested;
                    }
                }

                return null;
            case JsonArray array:
                foreach (var child in array)
                {
                    if (Find(child, property) is { } nested)
                    {
                        return nested;
                    }
                }

                return null;
            default:
                return null;
        }
    }
}
