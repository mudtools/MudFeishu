// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 编译期工具 Schema 常量（<see cref="FeishuToolSchemas"/>）的读取器：从<b>完整信封</b>中取出
/// <b>纯参数 JSON Schema</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须取纯参数 Schema</b>：
/// <see cref="Microsoft.Extensions.AI.AIFunctionDeclaration.JsonSchema"/> 的契约是
/// 「a JSON Schema describing the function and its input parameters」——其文档示例为
/// <c>{"type":"object","properties":{...},"required":[...]}</c>，即<b>参数本身</b>的 JSON Schema
/// 文档（由 <c>AIFunctionFactory</c> 从方法参数推导）。
/// </para>
/// <para>
/// 生成器产出的常量是<b>描述符信封</b>（<c>name</c>/<c>description</c>/<c>parameters</c>/<c>x-feishu</c>），
/// 属 OpenAI Functions 方言的「函数声明」形状，<b>不是</b>合法 JSON Schema：信封顶层的
/// <c>name</c>/<c>description</c>/<c>parameters</c> 里没有 JSON Schema 的 <c>type</c> 关键字，
/// 消费方按 Schema 解释时等价于「任意 JSON 可接受」，参数约束全部丢失。
/// </para>
/// <para>
/// 因此本读取器是全包<b>唯一</b>的「信封 → 参数 Schema」转换点（原先
/// <c>FeishuToolAIFunction</c> 用整信封、<c>FeishuToolCatalog</c> 用 <c>parameters</c>，两处口径不一致）。
/// </para>
/// </remarks>
internal static class ToolSchemaJson
{
    /// <summary>空对象参数 Schema（缺 <c>parameters</c> 字段时的兜底，仍是合法 JSON Schema）。</summary>
    public const string EmptyParametersJson = "{\"type\":\"object\",\"properties\":{}}";

    /// <summary>
    /// 取纯参数 JSON Schema（<see cref="JsonElement"/> 形态；调用方持有其生命周期）。
    /// </summary>
    /// <param name="schemaJson">编译期完整信封常量。</param>
    /// <returns>参数 Schema 元素；信封缺 <c>parameters</c> 时返回空对象 Schema。</returns>
    /// <exception cref="JsonException">常量不是合法 JSON（编译期产物损坏时的 fail-fast）。</exception>
    public static JsonElement ExtractParameters(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        if (document.RootElement.TryGetProperty("parameters", out var parameters)
            && parameters.ValueKind == JsonValueKind.Object)
        {
            return parameters.Clone();
        }

        // R3-13：兜底分支同样必须释放 JsonDocument（Clone() 之后的元素可脱离 document 存活）。
        using var fallback = JsonDocument.Parse(EmptyParametersJson);
        return fallback.RootElement.Clone();
    }

    /// <summary>
    /// 取纯参数 JSON Schema（原始文本形态，不重新序列化——保留生成器产出的字节）。
    /// </summary>
    /// <param name="schemaJson">编译期完整信封常量。</param>
    /// <returns>参数 Schema 文本；信封缺 <c>parameters</c> 时返回 <see cref="EmptyParametersJson"/>。</returns>
    /// <exception cref="JsonException">常量不是合法 JSON。</exception>
    public static string ExtractParametersJson(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        return document.RootElement.TryGetProperty("parameters", out var parameters)
               && parameters.ValueKind == JsonValueKind.Object
            ? parameters.GetRawText()
            : EmptyParametersJson;
    }
}
