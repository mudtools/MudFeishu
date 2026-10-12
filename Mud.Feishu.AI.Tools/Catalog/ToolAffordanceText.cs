// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// per-tool affordance 的<b>读取与渲染</b>（R-3 / F-7）：把编译期信封常量里的
/// <c>x-{sdk}.affordance</c> 变成模型可见文本，并作为结构化导出的透传入口。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要"第二条出口"</b>：<see cref="ToolSchemaJson.ExtractParameters"/> 取的是<b>纯参数
/// JSON Schema</b>（MEAI <c>AIFunctionDeclaration.JsonSchema</c> 契约），<c>x-{sdk}</c> 元数据
/// 刻意<b>不进 Schema 关键字空间</b>。于是 affordance 虽然已在编译期产物里，却不会自动到达模型。
/// 本类把"从信封取 affordance + 渲染成模型可读块"收敛为唯一实现。
/// </para>
/// <para>
/// <b>落到哪个可见面</b>：
/// <list type="bullet">
/// <item><see cref="FeishuToolAIFunction.Description"/> 尾部追加 <see cref="ForDescription"/> 的产物——
/// 工具描述是模型"选工具"时唯一必然读到的字段，且两链（进程内 Agent / MCP 桥）共用同一实例 ⇒ 逐字节一致；</item>
/// <item><c>IToolSchemaExporter.Export(McpEnvelope)</c> 透传结构化 <c>affordance</c>（给宿主 / CI / 桥接器）；</item>
/// <item>OpenAI Functions 方言本就逐字节透传整个信封（含 <c>x-feishu</c>），无需额外处理。</item>
/// </list>
/// </para>
/// <para>
/// <b>无资产 ⇒ 空串 ⇒ 逐字节不变</b>：<see cref="ForDescription"/> 在工具没有 affordance 时返回
/// 空串，故"未配资产的 166 个工具"的描述与升级前完全相同（这是可断言的回归锚）。
/// </para>
/// </remarks>
internal static class ToolAffordanceText
{
    /// <summary>
    /// 厂商扩展键（剖面槽 <c>SchemaExtensionKey</c> 的值）。
    /// </summary>
    /// <remarks>
    /// 字面量常量而非从剖面反射读取：配置文件（<c>FeishuToolProfile</c>）是 <c>internal sealed</c> 的
    /// 特性载体，运行期读它需要反射（AOT 不友好），且本键是<b>线格式事实</b>——由
    /// <c>AffordanceContractGuards</c> 断言"本常量 == 剖面槽值"，改一处不改另一处即报红。
    /// </remarks>
    public const string ExtensionKey = "x-feishu";

    /// <summary>扩展块里的 <c>affordance</c> 子对象（无则 <see langword="null"/>）。</summary>
    /// <param name="envelope">信封形态的工具 Schema 对象。</param>
    public static JsonNode? TryGetNode(JsonObject envelope)
    {
        // netstandard2.0 没有 ArgumentNullException.ThrowIfNull（四 TFM 下编译期即暴露）。
        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        return envelope.TryGetPropertyValue(ExtensionKey, out var extension)
               && extension is JsonObject block
               && block.TryGetPropertyValue("affordance", out var affordance)
            ? affordance
            : null;
    }

    /// <summary>从信封常量取 affordance 的原始 JSON 文本（无则 <see langword="null"/>）。</summary>
    /// <param name="schemaJson">编译期完整信封常量。</param>
    public static string? TryGetJson(string schemaJson) => ToolSchemaJson.ExtractAffordanceJson(schemaJson, ExtensionKey);

    /// <summary>
    /// 渲染为追加到工具 <c>Description</c> 尾部的决策指导块（无资产时为空串）。
    /// </summary>
    /// <param name="schemaJson">编译期完整信封常量。</param>
    /// <returns>形如 <c>\n\n【决策指导】何时用：…\n不要用：…\n前置：…\n提示：…</c>；无资产时为空串。</returns>
    /// <remarks>
    /// 四节<b>恒出现</b>（引擎保证四键非空），故这里没有"缺节"分支：缺节在构建期就是
    /// <c>MUDFT028</c> 错误，不可能走到运行期。
    /// </remarks>
    public static string ForDescription(string schemaJson)
    {
        var json = TryGetJson(schemaJson);
        if (json is null || JsonNode.Parse(json) is not JsonObject affordance)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(512);
        AppendSection(builder, "\n\n【决策指导】何时用：", affordance, "when");
        AppendSection(builder, "\n不要用：", affordance, "avoid_when");
        AppendSection(builder, "\n前置：", affordance, "prerequisites");
        AppendSection(builder, "\n提示：", affordance, "tips");
        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string label, JsonObject affordance, string key)
    {
        builder.Append(label);

        if (!affordance.TryGetPropertyValue(key, out var value) || value is not JsonArray items)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(items[i]?.GetValue<string>());
        }
    }
}
