// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.ToolSurface.Schema;

/// <summary>
/// 单工具的 <b>per-tool affordance</b>（决策指导）值对象：由 <c>{AffordanceDirectory}{工具名}.md</c>
/// 编译期解析而来，与工具描述符<b>同 pass</b> 发射。
/// </summary>
/// <remarks>
/// <para>
/// <b>值相等</b>：本类型参与增量管线（作为 <c>EmitToolSurface</c> 输入的组成部分），
/// 引用相等会让每次击键都判定为 <c>Modified</c>。
/// </para>
/// <para>
/// <b>四节恒存在且非空</b>（由 <see cref="AffordanceAssets"/> 的结构校验保证）⇒ JSON 四键恒出现，
/// 无需"缺键"分支；这也让下游（宿主渲染 / MCP 客户端 / 守卫）可以无条件按四键读取。
/// </para>
/// <para>
/// <b>为什么不与 guidance 合并</b>：guidance 是域级/跨工具的工作流资产，按域键读取、按需拉取；
/// affordance 是单工具的选型与前置条件，随描述符<b>推</b>给模型可见面。两者的键空间、消费时机、
/// 预算口径都不同，合并会让"域变宽即压缩文案"（F-1 纪律 22）与"单工具变胖即压缩文案"互相污染。
/// </para>
/// </remarks>
internal sealed class AffordanceEntry : IEquatable<AffordanceEntry?>
{
    /// <summary>构造 affordance 值对象（四节均为已解析的条目数组）。</summary>
    public AffordanceEntry(
        string toolName,
        ImmutableArray<string> when,
        ImmutableArray<string> avoidWhen,
        ImmutableArray<string> prerequisites,
        ImmutableArray<string> tips)
    {
        ToolName = toolName;
        When = when;
        AvoidWhen = avoidWhen;
        Prerequisites = prerequisites;
        Tips = tips;
    }

    /// <summary>契约工具名（= 资产文件名去 <c>.md</c>，与 <c>{P}Names</c> 同源）。</summary>
    public string ToolName { get; }

    /// <summary><c>WHEN</c> 节：何时使用本工具。</summary>
    public ImmutableArray<string> When { get; }

    /// <summary><c>Avoid when</c> 节：何时<b>不要</b>用本工具（以及应当改用的替代工具）。</summary>
    public ImmutableArray<string> AvoidWhen { get; }

    /// <summary><c>Prerequisites</c> 节：调用前必须先具备的事实（先调用哪个工具、需要哪些键）。</summary>
    public ImmutableArray<string> Prerequisites { get; }

    /// <summary><c>Tips</c> 节：容易踩的坑与翻页/截断等操作提示。</summary>
    public ImmutableArray<string> Tips { get; }

    /// <summary>
    /// 渲染 <c>x-{sdk}.affordance</c> 的 JSON 对象片段（四键恒出现，全部为字符串数组）。
    /// </summary>
    /// <remarks>
    /// 手工拼接而非序列化器：与 <see cref="SchemaWriter"/> 同款纪律——生成器运行在编译器进程内
    /// （<c>netstandard2.0</c>、AOT 无关），反射式序列化既不可用也无必要；转义统一走
    /// <see cref="JsonText.Quote"/>（全工程唯一转义实现）。
    /// </remarks>
    public string ToJson()
    {
        var json = new StringBuilder();
        json.Append('{');
        AppendArray(json, "when", When);
        json.Append(',');
        AppendArray(json, "avoid_when", AvoidWhen);
        json.Append(',');
        AppendArray(json, "prerequisites", Prerequisites);
        json.Append(',');
        AppendArray(json, "tips", Tips);
        json.Append('}');
        return json.ToString();
    }

    private static void AppendArray(StringBuilder json, string key, ImmutableArray<string> items)
    {
        json.Append('"').Append(key).Append("\":[");
        for (var i = 0; i < items.Length; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append(JsonText.Quote(items[i]));
        }

        json.Append(']');
    }

    /// <inheritdoc />
    public bool Equals(AffordanceEntry? other)
        => other is not null
            && ToolName == other.ToolName
            && When.SequenceEqual(other.When)
            && AvoidWhen.SequenceEqual(other.AvoidWhen)
            && Prerequisites.SequenceEqual(other.Prerequisites)
            && Tips.SequenceEqual(other.Tips);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as AffordanceEntry);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolName);
            foreach (var item in When)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(item);
            }

            foreach (var item in AvoidWhen)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(item);
            }

            foreach (var item in Prerequisites)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(item);
            }

            foreach (var item in Tips)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(item);
            }

            return hash;
        }
    }
}