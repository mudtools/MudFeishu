// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using Mud.Feishu.AI.Tools.Extraction;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// L2 描述符渲染器：把 <see cref="CapabilityEntry"/> 渲染为工具描述符常量（编译期 JSON 文本）。
/// </summary>
/// <remarks>
/// <para>
/// <b>形状（单一真相源）</b>：
/// <code>
/// {
///   "name": "bitable.list_tables",
///   "description": "...",
///   "parameters": { "type": "object", "properties": {...}, "required": [...] },
///   "x-feishu": {
///     "risk": "read|write|high-risk-write",
///     "is_write": false,
///     "identity": "tenant|user",
///     "required_scopes": ["..."]
///   }
/// }
/// </code>
/// </para>
/// <para>
/// <b>口径纪律</b>：<c>parameters</c> 是<b>纯参数 JSON Schema</b>，供
/// <c>FeishuToolAIFunction.JsonSchema</c>（MEAI 契约）直接消费；信封其余字段<b>不再</b>进入
/// Schema 关键字空间（旧实现把整信封当 Schema 用，属缺陷）。
/// </para>
/// <para>
/// <b>风险轴</b>（本方案 D5）：<c>risk</c> 是策略判定的权威字段；
/// <c>is_write</c> 是 <c>risk != "read"</c> 的<b>派生出</b>布尔（同一处产出，不构成双真相源），
/// 保留它是为了让 <c>FeishuToolRegistration</c> 与既有契约守卫的读法不变。
/// </para>
/// <para>
/// 旧实现另有 <c>WriteDescriptor</c>（<c>outputSchema</c> + <c>_meta</c>）与
/// <c>MapJsonType</c>（C# 类型字符串二次猜测）——两者均已删除：前者与 <see cref="WriteToolSchema"/>
/// 构成双形状，后者是 AT-B05 三条 Schema 缺陷的根因（改由
/// <see cref="ParameterSchemaRenderer"/> 从符号一次推导）。
/// </para>
/// </remarks>
internal static class SchemaWriter
{
    /// <summary>
    /// 渲染完整工具描述符 JSON（<c>name</c>/<c>description</c>/<c>parameters</c>/<c>x-feishu</c>）。
    /// </summary>
    /// <param name="entry">能力条目。</param>
    /// <param name="sourceMember">声明的 SDK 源成员（<c>接口.方法</c>，可空）。</param>
    public static string WriteToolSchema(CapabilityEntry entry, string? sourceMember = null)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"name\":").Append(JsonText.Quote(entry.ToolName));

        if (!string.IsNullOrWhiteSpace(entry.DocSummary))
        {
            json.Append(",\"description\":").Append(JsonText.Quote(entry.DocSummary!));
        }

        json.Append(",\"parameters\":").Append(WriteInputSchema(entry));
        json.Append(",\"x-feishu\":").Append(WriteExtension(entry, sourceMember));
        json.Append('}');
        return json.ToString();
    }

    /// <summary>
    /// 渲染 <c>parameters</c>（纯参数 JSON Schema）。
    /// </summary>
    /// <remarks>
    /// <b>可见性</b>：由 <see cref="DescriptorValidator"/> 消费（AT-B15：把"渲染产物"作为
    /// 与"参数意图模型"独立的第二个真相来源，做 <c>required ⊆ properties</c> 的真实比对）。
    /// </remarks>
    internal static string WriteInputSchema(CapabilityEntry entry)
    {
        var json = new StringBuilder();
        json.Append("{\"type\":\"object\",\"properties\":{");

        var required = new List<string>();
        var first = true;

        foreach (var param in entry.Parameters)
        {
            if (!first)
            {
                json.Append(',');
            }

            first = false;

            json.Append(JsonText.Quote(param.Name)).Append(':').Append(param.SchemaFragmentJson);

            if (param.IsRequired)
            {
                required.Add(param.Name);
            }
        }

        json.Append('}');

        if (required.Count > 0)
        {
            json.Append(",\"required\":[");
            for (var i = 0; i < required.Count; i++)
            {
                if (i > 0)
                {
                    json.Append(',');
                }

                json.Append(JsonText.Quote(required[i]));
            }

            json.Append(']');
        }

        // 条件必填（R5 / B-6）："至少提供一个"是跨参数约束，required 关键字表达不了
        // （那会让组内每一项都变必填）。用标准 JSON Schema 的 anyOf，**每个成员一个分支**：
        //   "anyOf": [ {"required":["user_id"]}, {"required":["room_id"]} ]
        // 语义 = 任一分支被满足 ⟺ 至少一个成员出现（即"二选一"）；分支顺序 = 声明顺序（golden 稳定）。
        //
        // ⚠️ 曾实现错误（已修，勿回退）：把整组塞进**单个**分支
        //   "anyOf": [ {"required":["user_id","room_id"]} ]
        // 那是 **AND** 语义（两者都必须出现），与"二选一"**完全相反** —— 比不表达更坏：
        // 模型会被结构化地告知"必须同时给 user_id 和 room_id"，从而永不命中正确用法。
        // 该反转由 AnyOfSemanticContractTests 按 JSON Schema 语义机械锁定（不只比文本）。
        if (entry.AnyOfGroups.Count > 0)
        {
            json.Append(",\"anyOf\":[");
            var firstBranch = true;
            for (var g = 0; g < entry.AnyOfGroups.Count; g++)
            {
                foreach (var member in entry.AnyOfGroups[g])
                {
                    if (!firstBranch)
                    {
                        json.Append(',');
                    }

                    firstBranch = false;
                    json.Append("{\"required\":[").Append(JsonText.Quote(member)).Append("]}");
                }
            }

            json.Append(']');
        }

        json.Append('}');
        return json.ToString();
    }

    /// <summary>
    /// 渲染 <c>x-feishu</c> 元数据块（厂商扩展；不占用 JSON Schema 关键字空间）。
    /// </summary>
    private static string WriteExtension(CapabilityEntry entry, string? sourceMember)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"risk\":").Append(JsonText.Quote(RiskToString(entry.Risk)));
        json.Append(",\"is_write\":").Append(entry.Risk != ToolRisk.Read ? "true" : "false");
        json.Append(",\"identity\":").Append(JsonText.Quote(entry.Identity.ToString().ToLowerInvariant()));
        json.Append(",\"required_scopes\":[");

        for (var i = 0; i < entry.Scopes.Count; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append(JsonText.Quote(entry.Scopes[i]));
        }

        json.Append(']');

        // 工具面 ↔ SDK 的显式挂钩（审计/排障：模型可见的能力到底打到哪条 HTTP 路由）。
        // sdk 取声明原文（接口.方法）——它不是从 entry 拼出来的，避免与声明漂移。
        if (!string.IsNullOrEmpty(sourceMember)
            && !string.IsNullOrEmpty(entry.HttpMethod)
            && !string.IsNullOrEmpty(entry.RouteTemplate))
        {
            json.Append(",\"source\":{");
            json.Append("\"sdk\":").Append(JsonText.Quote(sourceMember!));
            json.Append(",\"http\":").Append(JsonText.Quote(entry.HttpMethod));
            json.Append(",\"route\":").Append(JsonText.Quote(entry.RouteTemplate));
            json.Append('}');
        }

        // 返回值形状（MEAI ReturnJsonSchema 的数据源）。
        if (!string.IsNullOrEmpty(entry.OutputSchemaJson) && entry.OutputSchemaJson != "{}")
        {
            json.Append(",\"output_schema\":").Append(entry.OutputSchemaJson);
        }

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
}
