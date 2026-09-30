// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Schema 驱动输出投影（WP6/R5）：从工具的 <c>x-feishu.output_schema</c> 推导字段白名单，
/// 对 API 返回的 JSON 做递归字段过滤——只保留 output_schema 中声明的字段。
/// </summary>
/// <remarks>
/// <para>
/// <b>设计定位</b>（R5 WP6）：不引入新特性、不做 DSL（R4 U-1 边界）。本类是纯运行时字段过滤——
/// 把 <c>FeishuToolSchemas</c> 中编译期产出的 <c>output_schema</c> JSON 解析为字段路径集，
/// 再对运行时 <see cref="JsonObject"/> 做递归保留过滤。
/// </para>
/// <para>
/// <b>显式覆盖优先</b>：工具执行器可继续传入手写投影（<c>FromApi(outcome, project)</c>），
/// 本类仅在"无显式投影"时作为默认推导路径（由调用方直接调用 <see cref="Project"/>）。
/// 手写投影是<b>有意策展</b>（字段子集），本类是<b>全量保留</b>（output_schema 定义了什么就保留什么）。
/// </para>
/// <para>
/// <b>截断工具的处理</b>：output_schema 被 MUDFT009 截断的工具（深度超限/循环引用），
/// 其 output_schema 缺少深层字段——本类对 output_schema 未覆盖的路径<b>透传不裁剪</b>
/// （不因 Schema 截断而丢数据），但记日志供审计。
/// </para>
/// <para>
/// <b>AOT 安全</b>：纯 <see cref="JsonNode"/> 操作，零反射。output_schema 的解析在
/// 构造期完成一次并缓存字段路径集。
/// </para>
/// </remarks>
internal static class SchemaProjection
{
    /// <summary>
    /// 从 output_schema JSON 文本提取顶层字段名集合。
    /// </summary>
    /// <param name="outputSchemaJson">output_schema 的 JSON 文本（形如 <c>{"type":"object","properties":{...}}</c>）。</param>
    /// <returns>顶层属性名集合；非 object 类型或无 properties 时返回空集（表示"不过滤"）。</returns>
    /// <remarks>
    /// 只取顶层字段——深层过滤由 <see cref="Project"/> 递归处理，但字段路径集不预编译（避免复杂嵌套解析）。
    /// 顶层字段集的主要用途是"快速判断是否有字段被过滤"。
    /// </remarks>
    internal static HashSet<string>? ExtractTopLevelFields(string? outputSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(outputSchemaJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(outputSchemaJson);
            return ExtractTopLevelFields(doc.RootElement);
        }
        catch (JsonException)
        {
            // output_schema 解析失败 → 不过滤（安全默认：全量保留而非丢弃）。
            return null;
        }
    }

    /// <summary>
    /// 对运行时 JSON 做基于 output_schema 的递归字段过滤。
    /// </summary>
    /// <param name="data">API 返回的 JSON 对象（已解包到 Data 层）。</param>
    /// <param name="outputSchemaJson">output_schema 的 JSON 文本。</param>
    /// <returns>过滤后的 JSON 对象；output_schema 为空或非 object 时原样返回。</returns>
    internal static JsonObject Project(JsonObject data, string? outputSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(outputSchemaJson))
        {
            return data;
        }

        JsonNode? schemaNode;
        try
        {
            schemaNode = JsonNode.Parse(outputSchemaJson);
        }
        catch (JsonException)
        {
            return data;
        }

        if (schemaNode is not JsonObject schemaObj)
        {
            return data;
        }

        return (JsonObject)ProjectNode(data, schemaObj)!;
    }

    /// <summary>递归过滤：对 object 按 properties 白名单保留，对 array 递归过滤每个元素。</summary>
    private static JsonNode? ProjectNode(JsonNode? node, JsonObject schema)
    {
        if (node is null)
        {
            return null;
        }

        // schema 的 type 决定过滤策略
        var type = schema["type"]?.GetValue<string>();

        if (type == "object" && node is JsonObject obj)
        {
            return ProjectObject(obj, schema);
        }

        if (type == "array" && node is JsonArray arr)
        {
            var itemSchema = schema["items"] as JsonObject;
            var result = new JsonArray();
            foreach (var item in arr)
            {
                result.AddNode(itemSchema is not null
                    ? ProjectNode(item, itemSchema)
                    : item?.DeepClone());
            }

            return result;
        }

        // 非 object/array（或 schema 未声明 type）：透传不裁剪。
        return node.DeepClone();
    }

    /// <summary>对 object 按 schema.properties 白名单递归过滤。</summary>
    private static JsonObject ProjectObject(JsonObject obj, JsonObject schema)
    {
        var properties = schema["properties"] as JsonObject;
        if (properties is null || properties.Count == 0)
        {
            // 无 properties 声明 → 透传（不因 schema 缺失而丢字段）
            return (JsonObject)obj.DeepClone();
        }

        var result = new JsonObject();
        foreach (var prop in obj)
        {
            if (properties.TryGetPropertyValue(prop.Key, out var propSchema) && propSchema is JsonObject propSchemaObj)
            {
                result[prop.Key] = ProjectNode(prop.Value, propSchemaObj);
            }
            // output_schema 未声明的字段 → 丢弃（白名单语义）
        }

        return result;
    }

    /// <summary>从 JsonElement 提取顶层字段名集合。</summary>
    private static HashSet<string>? ExtractTopLevelFields(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty("properties", out var properties))
        {
            return null;
        }

        if (properties.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prop in properties.EnumerateObject())
        {
            fields.Add(prop.Name);
        }

        return fields.Count > 0 ? fields : null;
    }
}
