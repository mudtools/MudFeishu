// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具执行入口委托：由分域映射层（<c>FeishuToolBinding</c> 及各域处理器）提供，
/// 负责参数映射 → 强类型接口调用 → <c>FeishuApiResult</c> 解包 → 白名单投影/截断。
/// </summary>
/// <param name="arguments">模型 tool_call 的入参字典。</param>
/// <param name="context">执行上下文（appKey/chat/user）。</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>工具执行结果（含文本/截断标记，已投影/裁剪）。</returns>
public delegate Task<FeishuToolResult> FeishuToolHandler(
    IReadOnlyDictionary<string, object?> arguments,
    FeishuToolContext context,
    CancellationToken cancellationToken);

/// <summary>
/// 工具风险分级：<b>唯一真相源是编译期 Schema 的 <c>x-feishu.risk</c></b>
/// （由源生成器从 SDK 事实派生：危险词 → <c>high-risk-write</c>；<c>PUT/PATCH/DELETE</c> → <c>write</c>；
/// 其余 → <c>read</c>）。运行时<b>不得手写</b>本值，否则产生第二真相源并与 golden 脱钩。
/// </summary>
/// <remarks>
/// 取值与生成器内部 <c>ToolRisk</c> 逐一对齐（<c>read</c>=0 / <c>write</c>=1 / <c>high-risk-write</c>=2），
/// 使"风险单调可比较"——配置键 <c>FeishuAgent:MaxToolRisk</c> 的判定依赖该序关系。
/// </remarks>
public enum FeishuToolRisk
{
    /// <summary>只读（GET）。</summary>
    Read = 0,

    /// <summary>写操作（POST/PUT/PATCH/DELETE，未命中危险词）。</summary>
    Write = 1,

    /// <summary>高风险写操作（命中危险词表：删除/清空/关闭/解散/撤回等）。</summary>
    HighRiskWrite = 2,
}

/// <summary>
/// 单个工具的注册表条目（Schema 元数据 + 执行入口）。
/// </summary>
/// <param name="Name">工具名（模型可见契约，如 <c>bitable.query_records</c>）。</param>
/// <param name="Description">模型侧描述。</param>
/// <param name="RequiredScopes">权限点清单（审计消费，SDK 不内建校验）。</param>
/// <param name="IsWrite">是否写操作（读写白名单分离的事实来源；由 <c>x-feishu.is_write</c> 派生）。</param>
/// <param name="Risk">风险分级（由 <c>x-feishu.risk</c> 派生，AT-B13 的策略判定输入；不得手写）。</param>
/// <param name="Identity">工具身份（由 <c>x-feishu.identity</c> 派生：<c>tenant</c> / <c>user</c>，
/// AT-B13 Identity 轴输入；同样不得手写）。</param>
/// <param name="Handler">执行入口（仅启用的工具被调用）。</param>
public sealed record FeishuToolDefinition(
    string Name,
    string Description,
    IReadOnlyList<string> RequiredScopes,
    bool IsWrite,
    FeishuToolRisk Risk,
    string Identity,
    FeishuToolHandler Handler);

/// <summary>
/// 工具注册表：默认「收进注册表不启用」，需 <see cref="MapTool"/> 显式启用（白名单语义）。
/// </summary>
/// <remarks>
/// 工具名是模型可见契约——注册名与工具名契约表的漂移由契约守卫锁定（Phase 1 §7）。
/// 本类型非线程安全的构建期容器：宿主启动期 <c>Register</c>/<c>MapTool</c>，运行期只读。
/// </remarks>
public sealed class FeishuToolRegistry
{
    private readonly Dictionary<string, FeishuToolDefinition> _tools = new(StringComparer.Ordinal);
    private readonly HashSet<string> _enabled = new(StringComparer.Ordinal);

    /// <summary>
    /// 注册工具（不启用）。
    /// </summary>
    /// <param name="definition">工具定义。</param>
    /// <returns>注册表（链式）。</returns>
    /// <exception cref="ArgumentException">工具名重复。</exception>
    public FeishuToolRegistry Register(FeishuToolDefinition definition)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new ArgumentException("工具名不能为空", nameof(definition));
        if (_tools.ContainsKey(definition.Name))
            throw new ArgumentException($"工具 '{definition.Name}' 已注册", nameof(definition));

        _tools.Add(definition.Name, definition);
        return this;
    }

    /// <summary>
    /// 启用已注册工具（白名单显式 Map）。
    /// </summary>
    /// <param name="name">工具名。</param>
    /// <returns>注册表（链式）。</returns>
    /// <exception cref="KeyNotFoundException">工具未注册。</exception>
    public FeishuToolRegistry MapTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("工具名不能为空", nameof(name));
        if (!_tools.ContainsKey(name))
            throw new KeyNotFoundException($"工具 '{name}' 未注册——先 Register 再 MapTool");

        _enabled.Add(name);
        return this;
    }

    /// <summary>工具是否已启用。</summary>
    public bool IsEnabled(string name)
        => !string.IsNullOrWhiteSpace(name) && _enabled.Contains(name);

    /// <summary>按名取定义。</summary>
    public bool TryGet(string name, out FeishuToolDefinition? definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _tools.TryGetValue(name, out definition);
    }

    /// <summary>全部已启用工具（给模型侧的 tools 列表来源）。</summary>
    public IReadOnlyList<FeishuToolDefinition> EnabledTools
        => _enabled.Select(n => _tools[n]).ToArray();

    /// <summary>全部已注册工具（目录视图，含未启用）。</summary>
    public IReadOnlyList<FeishuToolDefinition> AllTools
        => _tools.Values.ToArray();
}
