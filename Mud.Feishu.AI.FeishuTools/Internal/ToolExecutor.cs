// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// 执行器骨架收敛（WP3 / R-C 根因 + F-3）：把"参数校验失败回填 / JSON 解析失败回填 /
/// ApiResult 解包 + 失败回填 + 成功投影 + 截断"三段机械骨架从 22 个执行器方法中抽出。
/// </summary>
/// <remarks>
/// <para>
/// <b>边界（与 R3 判断一致，理由加固）</b>：投影（<c>ProjectXxx</c> 的字段点选）是<b>有意策展</b>，
/// 留在各执行器；骨架（catch/回填/截断）是<b>纯机械</b>，收拢于此。本类型是 internal 值结构 +
/// 两个方法，<b>零新抽象层次、零新契约面</b>。
/// </para>
/// <para>
/// <b>per-method 实例</b>：工具名 + 截断上限在构造时绑定一次，执行器方法内
/// <c>FeishuToolNames.X</c> 只出现 1 处（WP3 验收指标，守卫机械断言防回归）。
/// </para>
/// </remarks>
/// <param name="toolName">工具名（结构化错误的锚点）。</param>
/// <param name="maxResultLength">结果截断上限（来自 <c>FeishuAgentOptions.MaxToolResultLength</c>）。</param>
internal readonly struct ToolExecutor(string toolName, int maxResultLength)
{
    /// <summary>写工具构造：写工具载荷小（单 ID），无截断语义，不注入截断上限。</summary>
    public ToolExecutor(string toolName)
        : this(toolName, 0)
    {
    }

    /// <summary>
    /// 工具名（方法体内<b>策展错误路径</b>复用：dry-run 摘要、filter/sort 解析错误等——
    /// 保证 <c>FeishuToolNames.X</c> 在每个执行器方法中只出现 1 处，其余经本属性）。
    /// </summary>
    public string ToolName => toolName;
    /// <summary>
    /// 统一执行骨架：参数校验失败（<see cref="ArgumentException"/>）与 JSON 解析失败
    /// （<see cref="JsonException"/>）的结构化回填（取代 24 + 2 处 catch）。
    /// </summary>
    /// <remarks>
    /// 行为等价性：与被取代的逐方法 try/catch 完全同构——catch 覆盖整个方法体
    /// （校验、下游调用与投影），补偿性回填不替换原始异常的"结构化错误"语义。
    /// </remarks>
    public async Task<FeishuToolResult> RunAsync(Func<Task<FeishuToolResult>> body)
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(toolName, ex.Message));
        }
        catch (JsonException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(toolName, $"参数不是合法 JSON: {ex.Message}"));
        }
    }

    /// <summary>
    /// 统一 ApiResult 解包路径（<b>纯文本载荷</b>）：失败回填 + 成功取文本 + 截断。
    /// 适用不做字段投影的出参（如 docx 正文纯文本——信封投影会改变行为）。
    /// </summary>
    public FeishuToolResult FromPlainText<T>(FeishuApiOutcome<T> outcome, Func<T, string?> text) where T : class
    {
        if (!outcome.Ok)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(toolName, outcome.Code, outcome.ErrorText!));
        }

        return FeishuToolResult.FromText(ToolResultText.Truncate(text(outcome.Data!) ?? string.Empty, maxResultLength));
    }

    /// <summary>
    /// 统一 ApiResult 解包路径（<b>不截断</b>）：失败回填 + 成功投影。写工具专用——
    /// 载荷是单个 ID 的迷你信封，无截断语义（也不持有 MaxToolResultLength）。
    /// </summary>
    public FeishuToolResult FromApiUntruncated<T>(FeishuApiOutcome<T> outcome, Func<T, JsonObject> project) where T : class
    {
        if (!outcome.Ok)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(toolName, outcome.Code, outcome.ErrorText!));
        }

        return FeishuToolResult.FromText(project(outcome.Data!).ToJsonString());
    }

    /// <summary>
    /// <b>多步链路</b>的失败短路（WP7 上传 → 发送两步链路）：解包失败时返回结构化错误结果，
    /// 成功时返回 <see langword="null"/>，调用方据此继续下一步。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 用法：<c>if (executor.FailIfError(uploadOutcome) is { } failure) { return failure; }</c>——
    /// 与 <c>FromApi*</c> 同一体例（错误回填仍集中在 <see cref="ToolExecutor"/>），
    /// 中间步骤因此<b>不需要</b>在具体执行器里手写 <c>if (!outcome.Ok)</c> 骨架（WP3 守卫锁死该形态）。
    /// </para>
    /// <para>
    /// 为什么中间步骤不能直接用 <c>FromApi*</c>：那需要一个"成功投影"，
    /// 而中间步骤的产物（如 <c>image_key</c>）不是工具结果，投影只能写成恒等函数——那会掩盖意图。
    /// </para>
    /// </remarks>
    /// <typeparam name="T">业务载荷类型。</typeparam>
    /// <param name="outcome"><see cref="FeishuApiResultReader.Read"/> 的解包结果。</param>
    /// <returns>失败时的结构化错误结果；成功时为 <see langword="null"/>。</returns>
    public FeishuToolResult? FailIfError<T>(FeishuApiOutcome<T> outcome) where T : class
        => outcome.Ok
            ? null
            : FeishuToolResult.FromError(FeishuToolBinding.StructuredError(toolName, outcome.Code, outcome.ErrorText!));

    /// <summary>
    /// 统一 ApiResult 解包路径：失败回填 + 成功投影 + 截断（取代 22 处 <c>if (!outcome.Ok)</c>）。
    /// </summary>
    /// <typeparam name="T">业务载荷类型。</typeparam>
    /// <param name="outcome"><see cref="FeishuApiResultReader.Read"/> 的解包结果。</param>
    /// <param name="project">有意策展的投影（字段点选语义所在，不可自动推导）。</param>
    public FeishuToolResult FromApi<T>(FeishuApiOutcome<T> outcome, Func<T, JsonObject> project) where T : class
    {
        if (!outcome.Ok)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(toolName, outcome.Code, outcome.ErrorText!));
        }

        var envelope = project(outcome.Data!);
        return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), maxResultLength));
    }

}
