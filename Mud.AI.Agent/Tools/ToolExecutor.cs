// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mud.AI.Agent.Tools;

/// <summary>
/// 执行器骨架收敛（WP3 / R-C 根因 + F-3）：把"参数校验失败回填 / JSON 解析失败回填 /
/// ApiResult 解包 + 失败回填 + 成功投影 + 截断"三段机械骨架从 22 个执行器方法中抽出。
/// </summary>
/// <remarks>
/// <para>
/// <b>边界（与 R3 判断一致，理由加固）</b>：投影（<c>ProjectXxx</c> 的字段点选）是<b>有意策展</b>，
/// 留在各执行器；骨架（catch/回填/截断）是<b>纯机械</b>，收拢于此。本类型是 internal 值结构 +
/// 数个方法，<b>零新抽象层次、零新契约面</b>。
/// </para>
/// <para>
/// <b>出站预算与标记委托给 <see cref="ToolResultPipeline"/></b>（唯一出口）。
/// 本类型不再自行比较长度/拼装标记——"截断是出口的性质，而不是某个执行器记得写的一步"。
/// </para>
/// <para>
/// <b>per-method 实例</b>：工具名 + 截断上限在构造时绑定一次，执行器方法内
/// <c>FeishuToolNames.X</c> 只出现 1 处（WP3 验收指标，守卫机械断言防回归）。
/// </para>
/// </remarks>
/// <param name="toolName">工具名（结构化错误的锚点）。</param>
/// <param name="maxResultLength">结果截断上限（来自 <c>FeishuAgentOptions.MaxToolResultLength</c>；≤ 0 = 无预算）。</param>
public readonly struct ToolExecutor(string toolName, int maxResultLength)
{
    /// <summary>写工具构造：写工具载荷小（单 ID），无截断语义，不注入截断上限。</summary>
    public ToolExecutor(string toolName)
        : this(toolName, 0)
    {
    }

    /// <summary>
    /// <b>R-4：<c>IOptions</c> 空值守卫的唯一地点</b>——执行器只声明"工具名 + 依赖"，
    /// 构造参数校验集中于此。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该守卫只允许存在于此处：执行器各自重写（<c>(options ?? throw …).Value.MaxXxx</c>，含多个预算项的域
    /// 就写多遍）会让判据分裂。由守卫
    /// <c>ToolExecutorSkeletonGuards.OptionsNullGuard_ShouldLiveOnlyInToolExecutor</c> 机械锁定不回潮。
    /// </para>
    /// <para>
    /// <b>为什么是泛型而不是 <c>IOptions&lt;FeishuAgentOptions&gt;</c></b>（v2.3 中立化修正）：
    /// 骨架进 <c>Mud.AI.Agent</c> 后不得反向依赖任何产品包（飞书/企微的 Options 都在各自的下游包）——
    /// 连同类型搬走违反中立命名，不搬则成环。泛型化后 233 个调用点
    /// <c>ToolExecutor.Require(options)</c> 靠类型推断<b>零改动</b>，空值守卫仍只在骨架一处。
    /// 返回已校验的 Options 让"守卫一次、读取多次"成为唯一形态（执行器需要多个预算项，
    /// 按值分别开放会重复过守卫——正是重复的来源）。
    /// </para>
    /// </remarks>
    /// <param name="options">宿主注入的 Agent 选项（<see langword="null"/> ⇒ 立即抛，构造期即暴露配置错误）。</param>
    /// <returns>已校验的选项值。</returns>
    // 泛型参数带 DynamicallyAccessedMembers：IOptions<TOptions> 的上游契约要求
    // PublicParameterlessConstructor（配置绑定源生成经 new T() 构造）；不标注即 IL2091。
    // netstandard2.0 的 BCL 该特性为 internal，按仓库既有体例 #if 裁剪（见 FeishuWebSocketServiceBuilder）。
#if NET6_0_OR_GREATER
    public static TOptions Require<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(
        IOptions<TOptions> options) where TOptions : class
#else
    public static TOptions Require<TOptions>(
        IOptions<TOptions> options) where TOptions : class
#endif
        => (options ?? throw new ArgumentNullException(nameof(options))).Value;

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
    public async Task<ToolResult> RunAsync(Func<Task<ToolResult>> body)
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            // 有意静默（守卫白名单）：异常被**转换**为模型可见的结构化错误文本（换了一条上报通道：
            // 回填模型 + FeishuToolBinding 侧同样计 Error 指标与审计），不是吞掉。
            // R-3 / BUG-8：闭集非法值携带的候选（ToolArgumentException.Suggestions）随载荷回填模型。
            return FromStructuredError(
                ToolErrorCategory.Validation, ToolErrorSubtype.InvalidArgs, ex.Message,
                suggestions: (ex as ToolArgumentException)?.Suggestions);
        }
        catch (JsonException ex)
        {
            // 有意静默（守卫白名单）：同 ArgumentException 分支——转为结构化错误文本回填模型。
            return FromStructuredError(ToolErrorCategory.Validation, ToolErrorSubtype.ShapeMismatch, $"参数不是合法 JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// 统一 ApiResult 解包路径（<b>纯文本载荷</b>）：失败回填 + 成功取文本 + 截断。
    /// 适用不做字段投影的出参（如 docx 正文纯文本——信封投影会改变行为）。
    /// </summary>
    public ToolResult FromPlainText<T>(ToolApiOutcome<T> outcome, Func<T, string?> text) where T : class
    {
        if (!outcome.Ok)
        {
            return FromApiOutcomeError(outcome.Code, outcome.ErrorText!, outcome.Failure);
        }

        return ToolResultPipeline.Ok(text(outcome.Data!), maxResultLength);
    }

    /// <summary>
    /// 统一 ApiResult 解包路径（<b>不截断</b>）：失败回填 + 成功投影。写工具专用——
    /// 载荷是单个 ID 的迷你信封，无截断语义（也不持有 MaxToolResultLength）。
    /// </summary>
    public ToolResult FromApiUntruncated<T>(ToolApiOutcome<T> outcome, Func<T, JsonObject> project) where T : class
    {
        if (!outcome.Ok)
        {
            return FromApiOutcomeError(outcome.Code, outcome.ErrorText!, outcome.Failure);
        }

        // maxLength = 0 ⇒ 无预算（与单参构造的语义一致）。
        return ToolResultPipeline.OkJson(project(outcome.Data!), 0);
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
    /// <param name="outcome">产品侧 API 结果读取器（如飞书 <c>FeishuApiResultReader.Read</c>）的解包结果。</param>
    /// <returns>失败时的结构化错误结果；成功时为 <see langword="null"/>。</returns>
    public ToolResult? FailIfError<T>(ToolApiOutcome<T> outcome) where T : class
        => outcome.Ok
            ? null
            : FromApiOutcomeError(outcome.Code, outcome.ErrorText!, outcome.Failure);

    /// <summary>
    /// <b>统一分页出口（页状态显式版）</b>：给<b>翻页契约字段名与基类不一致</b>的历史 DTO 用。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么需要它</b>：仓内少数结果类型是"自带 <c>items</c>/<c>total</c>/<c>has_more</c>/<c>page_token</c>
    /// 但<b>不继承</b> <c>ApiPageListResult</c>"的独立 DTO（如 <c>SearchMinutesResult</c>）。
    /// 没有本重载时，这类工具只能退回"手写两段（聚合 + 单页）"——正是本方法要消灭的形态。
    /// </para>
    /// <para>
    /// <b>纪律</b>：<paramref name="readPageState"/> 必须读<b>该 DTO 自己声明的</b>那两个字段
    /// （而不是另找等价字段猜值）；空值语义按"<c>has_more == true</c> 才算有下一页"处理
    /// （可空布尔的历史 DTO 里，<c>null</c> 表示"未知"，此时停在第 N 页而不要无限翻）。
    /// </para>
    /// </remarks>
    public async Task<ToolResult> FromPagedApiAsync<TPage>(
        Func<string?, CancellationToken, Task<ToolApiOutcome<TPage>>> fetchPage,
        Func<TPage, JsonObject> project,
        Func<TPage, (bool HasMore, string? PageToken)> readPageState,
        bool? fetchAll,
        int? maxItems,
        string? pageToken,
        int optionsDefaultMaxItems,
        int optionsDefaultMaxPages,
        CancellationToken cancellationToken,
        bool untruncated = false)
        // 引用类型约束来自 ToolApiOutcome<T>（它承载 T? 数据）；不继承 ApiPageListResult 的
        // 历史 DTO 也满足它，故本重载的适用面比约束版更宽。
        where TPage : class
    {
        if (fetchAll == true)
        {
            var budget = ToolPagination.ResolveMaxItems(maxItems, optionsDefaultMaxItems);
            var aggregated = await ToolPagination.AggregateOutcomesAsync(
                fetchPage,
                readPageState,
                page => project(page)["items"]!.AsArray(),
                budget,
                ToolPagination.ResolveMaxPages(optionsDefaultMaxPages),
                cancellationToken).ConfigureAwait(false);

            return FromPagedResult(aggregated, budget);
        }

        var single = await fetchPage(pageToken, cancellationToken).ConfigureAwait(false);

        // untruncated：少数工具的**单页**语义是"不截断"（如 minutes.search 的逐条预览已自带窗口）。
        // 显式开关而不是"顺手统一成 FromApi"——那会把既有单页行为从"不截断"静默改成"截断"，
        // 正是 P-6 红线要拦的形态。
        return untruncated ? FromApiUntruncated(single, project) : FromApi(single, project);
    }

    /// <summary>
    /// 统一 ApiResult 解包路径：失败回填 + 成功投影 + 截断（取代 22 处 <c>if (!outcome.Ok)</c>）。
    /// </summary>
    /// <typeparam name="T">业务载荷类型。</typeparam>
    /// <param name="outcome"><see cref="FeishuApiResultReader.Read"/> 的解包结果。</param>
    /// <param name="project">有意策展的投影（字段点选语义所在，不可自动推导）。</param>
    public ToolResult FromApi<T>(ToolApiOutcome<T> outcome, Func<T, JsonObject> project) where T : class
    {
        if (!outcome.Ok)
        {
            return FromApiOutcomeError(outcome.Code, outcome.ErrorText!, outcome.Failure);
        }

        return ToolResultPipeline.OkJson(project(outcome.Data!), maxResultLength);
    }

    /// <summary>
    /// 统一 <c>fetch_all</c> 聚合信封出口（B1）：首页即失败 → 回到结构化错误路径
    /// （保留产品 code 与<b>产品侧读数</b> → <see cref="ToolApiFailure"/>）；
    /// 其余情况 → 聚合信封（items/has_more/truncated/next_page_token/_budget/partial_error）。
    /// </summary>
    /// <remarks>
    /// 截断标记的<b>双来源合并</b>：聚合层截断（触达 <c>max_items</c>/<c>max_pages</c>）与
    /// 结果长度截断（<c>MaxToolResultLength</c>）任一为真即 <c>Truncated=true</c>——
    /// 否则"信封被长度截断"会对宿主与审计不可见（B4 的可见性要求）。
    /// </remarks>
    /// <param name="result">聚合结果。</param>
    /// <param name="maxItems">本条调用的结果预算上限（写入 <c>_budget</c>）。</param>
    public ToolResult FromPagedResult(ToolPagination.PagedFetchResult result, int maxItems)
    {
        // 首页即失败：没有任何已取数据 ⇒ 不是"部分成功"，而是该调用的确定性失败。
        if (result.Error is not null && result.PagesFetched == 0)
        {
            return FromApiOutcomeError(result.ApiCode, result.Error, result.Failure);
        }

        var envelope = ToolPagination.BuildEnvelope(result, maxItems);

        // R-1：长度维度经唯一出口（截断与标记同源），聚合维度由上面的信封自述。
        var lengthBounded = ToolResultPipeline.OkJson(envelope, maxResultLength);
        if (!result.Truncated)
        {
            return lengthBounded;
        }

        // 聚合层已截断：原因以聚合层为准（信封内已带具体预算/页数原因），标记合并为真。
        return ToolResult.FromText(
            lengthBounded.ToString(), truncated: true, ToolPagination.AggregateTruncationReason);
    }

    /// <summary>
    /// 构造带结构化错误载荷的错误结果（B2 错误契约：首行 JSON + 人类可读正文）。
    /// </summary>
    /// <remarks>
    /// R-1 / B-2：载荷构造收口到 <see cref="ToolErrorFactory"/>（唯一构造点），
    /// 本方法只负责"人类可读正文由执行链的分类文案生成"这一层。
    /// </remarks>
    private ToolResult FromStructuredError(
        ToolErrorCategory category,
        string subtype,
        string reason,
        int? apiCode = null,
        IReadOnlyList<string>? suggestions = null)
        => ToolResultPipeline.Error(
            toolName,
            category,
            subtype,
            ToolErrorText.StructuredError(toolName, category, reason, apiCode),
            maxResultLength,
            apiCode,
            suggestions: suggestions);

    /// <summary>
    /// 从产品 API outcome 构造错误结果（分类取<b>产品侧读数</b>——中立层不认识任何产品码表）。
    /// </summary>
    /// <param name="apiCode">产品业务 code（进载荷 <c>api_code</c>）。</param>
    /// <param name="errorText">可读错误文本。</param>
    /// <param name="failure">产品侧读数；<see langword="null"/> 时回落 <c>api/upstream_error</c>。</param>
    private ToolResult FromApiOutcomeError(int? apiCode, string errorText, ToolApiFailure? failure)
    {
        var reading = failure ?? ToolApiFailure.Unclassified;
        return FromStructuredError(reading.Category, reading.Subtype, errorText, apiCode, reading.Suggestions);
    }
}
