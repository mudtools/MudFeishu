// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mud.Feishu.AI.Tools.Extraction;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// L2 Schema 渲染器：把 <see cref="CapabilityEntry"/> 渲染为三段式 input + output + _meta 的完整描述符 JSON。
/// </summary>
/// <remarks>
/// <para>输入 Schema 三段式（对齐 CLI）：<c>params</c>（path+query）/ <c>data</c>（body）/ <c>file</c>（format:binary）。</para>
/// <para>输出 Schema：由 <see cref="TypeSchemaResolver"/> 推导。</para>
/// <para>_meta：identity / risk / danger / scopes / doc_url / envelope_version。</para>
/// </remarks>
internal static class SchemaWriter
{
    /// <summary>
    /// 渲染完整描述符 JSON（含 name/description/inputSchema/outputSchema/_meta）。
    /// </summary>
    public static string WriteDescriptor(CapabilityEntry entry, string outputSchema)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"name\":").Append(Quote(entry.ToolName));

        if (!string.IsNullOrWhiteSpace(entry.DocSummary))
        {
            json.Append(",\"description\":").Append(Quote(entry.DocSummary!));
        }

        // Input Schema（三段式）
        json.Append(",\"parameters\":").Append(WriteInputSchema(entry));

        // Output Schema
        json.Append(",\"outputSchema\":").Append(outputSchema);

        // _meta
        json.Append(",\"_meta\":").Append(WriteMeta(entry));

        json.Append('}');
        return json.ToString();
    }

    /// <summary>
    /// 渲染兼容层 Schema JSON（与既有 FeishuToolSchemas 产物一致的格式：name/description/parameters/x-feishu）。
    /// </summary>
    public static string WriteCompatSchema(CapabilityEntry entry)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"name\":").Append(Quote(entry.ToolName));

        if (!string.IsNullOrWhiteSpace(entry.DocSummary))
        {
            json.Append(",\"description\":").Append(Quote(entry.DocSummary!));
        }

        // 兼容层：只产 parameters（与今日产物一致）
        json.Append(",\"parameters\":").Append(WriteInputSchema(entry));

        // x-feishu（兼容既有格式）
        json.Append(",\"x-feishu\":{");
        json.Append("\"is_write\":").Append(entry.Risk != ToolRisk.Read ? "true" : "false");
        json.Append(",\"required_scopes\":[");
        for (var i = 0; i < entry.Scopes.Count; i++)
        {
            if (i > 0) json.Append(',');
            json.Append(Quote(entry.Scopes[i]));
        }

        json.Append("]}}");
        return json.ToString();
    }

    /// <summary>
    /// 渲染 input Schema（三段式 params/data/file）。
    /// </summary>
    private static string WriteInputSchema(CapabilityEntry entry)
    {
        var json = new StringBuilder();
        json.Append("{\"type\":\"object\",\"properties\":{");

        var required = new List<string>();
        var first = true;

        foreach (var param in entry.Parameters)
        {
            if (!first) json.Append(',');
            first = false;

            json.Append(Quote(param.Name)).Append(':');

            var paramSchema = WriteParameterSchema(param);
            json.Append(paramSchema);

            if (param.IsRequired)
            {
                required.Add(param.Name);
            }
        }

        json.Append("}");

        if (required.Count > 0)
        {
            json.Append(",\"required\":[");
            for (var i = 0; i < required.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append(Quote(required[i]));
            }

            json.Append("]");
        }

        json.Append("}");
        return json.ToString();
    }

    private static string WriteParameterSchema(CapabilityParameter param)
    {
        var json = new StringBuilder();
        var jsonType = MapJsonType(param.CsharpType);
        json.Append("{\"type\":").Append(Quote(jsonType));

        // 数组 items 由元素类型推导（修掉现状硬编码 string）
        if (jsonType == "array")
        {
            var itemType = MapArrayItemType(param.CsharpType);
            json.Append(",\"items\":{\"type\":").Append(Quote(itemType)).Append('}');
        }

        // 文件上传/下载 → format: binary（MapJsonType 不产 "binary"，需直接检查 C# 类型）
        if (param.ParameterKind == "FormContent" || IsBinaryType(param.CsharpType))
        {
            json.Append(",\"format\":\"binary\"");
        }

        if (!string.IsNullOrWhiteSpace(param.DocDescription))
        {
            json.Append(",\"description\":").Append(Quote(param.DocDescription!));
        }

        json.Append('}');
        return json.ToString();
    }

    private static string WriteMeta(CapabilityEntry entry)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"envelope_version\":\"1.0\"");
        json.Append(",\"identity\":\"").Append(entry.Identity.ToString().ToLowerInvariant()).Append('\"');
        json.Append(",\"risk\":\"").Append(RiskToString(entry.Risk)).Append('\"');
        json.Append(",\"danger\":").Append(entry.Risk != ToolRisk.Read ? "true" : "false");
        json.Append(",\"scopes\":[");
        for (var i = 0; i < entry.Scopes.Count; i++)
        {
            if (i > 0) json.Append(',');
            json.Append(Quote(entry.Scopes[i]));
        }

        json.Append("]");
        json.Append(",\"required_scopes\":[");
        for (var i = 0; i < entry.Scopes.Count; i++)
        {
            if (i > 0) json.Append(',');
            json.Append(Quote(entry.Scopes[i]));
        }

        json.Append("]");
        json.Append('}');
        return json.ToString();
    }

    private static string RiskToString(ToolRisk risk) => risk switch
    {
        ToolRisk.Read => "read",
        ToolRisk.Write => "write",
        ToolRisk.HighRiskWrite => "high-risk-write",
        _ => "read",
    };

    private static string MapJsonType(string csharpType)
    {
        // 可空类型展开
        var type = csharpType;
        if (type.EndsWith("?"))
            type = type.Substring(0, type.Length - 1);

        // 去掉命名空间前缀
        var shortName = type.Contains('.') ? type.Substring(type.LastIndexOf('.') + 1) : type;

        return shortName switch
        {
            "Boolean" or "bool" => "boolean",
            "Int16" or "Int32" or "Int64" or "UInt16" or "UInt32" or "UInt64"
                or "short" or "int" or "long" or "ushort" or "uint" or "ulong" => "integer",
            "Single" or "Double" or "Decimal"
                or "float" or "double" or "decimal" => "number",
            "String" or "string" => "string",
            "DateTime" or "DateTimeOffset" => "string",
            "TimeSpan" => "string",
            "Guid" => "string",
            "Byte[]" or "byte[]" => "string",
            _ when shortName.StartsWith("List<") || shortName.StartsWith("IReadOnlyList<")
                || shortName.StartsWith("IEnumerable<") || shortName.StartsWith("IList<")
                || shortName.EndsWith("[]") => "array",
            _ when shortName.StartsWith("Dictionary<") || shortName.StartsWith("IDictionary<") => "object",
            _ => "string",
        };
    }

    private static string MapArrayItemType(string csharpType)
    {
        // 从泛型参数取元素类型
        var type = csharpType;
        if (type.EndsWith("?"))
            type = type.Substring(0, type.Length - 1);

        // List<T> → T
        var lt = type.IndexOf('<');
        var gt = type.LastIndexOf('>');
        if (lt >= 0 && gt > lt)
        {
            var element = type.Substring(lt + 1, gt - lt - 1);
            // 如果元素是复合类型，降级为 object（防 items 过度嵌套）
            var elementShort = element.Contains('.') ? element.Substring(element.LastIndexOf('.') + 1) : element;
            if (elementShort == "String" || elementShort == "string")
                return "string";
            if (elementShort == "Int32" || elementShort == "int" || elementShort == "Int64" || elementShort == "long")
                return "integer";
            if (elementShort == "Boolean" || elementShort == "bool")
                return "boolean";
            return "object";
        }

        // T[] → T
        if (type.EndsWith("[]"))
        {
            var element = type.Substring(0, type.Length - 2);
            var elementShort = element.Contains('.') ? element.Substring(element.LastIndexOf('.') + 1) : element;
            if (elementShort == "String" || elementShort == "string")
                return "string";
            if (elementShort == "Int32" || elementShort == "int")
                return "integer";
            return "object";
        }

        return "string";
    }

    /// <summary>检测 C# 类型名是否为二进制类型（byte[] / Stream），用于 format: binary 标注。</summary>
    private static bool IsBinaryType(string csharpType)
    {
        var type = csharpType.TrimEnd('?');
        var shortName = type.Contains('.') ? type.Substring(type.LastIndexOf('.') + 1) : type;
        return shortName is "Byte[]" or "byte[]"
            or "Stream" or "FileStream" or "MemoryStream";
    }

    private static string Quote(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < ' ')
                        sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    else
                        sb.Append(ch);
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }
}
