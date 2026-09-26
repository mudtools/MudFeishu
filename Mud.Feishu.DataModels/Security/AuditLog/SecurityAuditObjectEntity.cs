// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志的操作对象（audit_object_entity）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditObjectEntity
{
    /// <summary>
    /// <para>操作对象类型，字段详情见飞书枚举值列表附录</para>
    /// <para>示例值：106</para>
    /// </summary>
    [JsonPropertyName("object_type")]
    public string? ObjectType { get; set; }

    /// <summary>
    /// <para>操作对象值</para>
    /// <para>示例值：Lwd1smp3nl01AndDEMzbsfqacBb</para>
    /// </summary>
    [JsonPropertyName("object_value")]
    public string? ObjectValue { get; set; }

    /// <summary>
    /// <para>操作对象名称，当前针对文档、会话、应用类型开放，如会话名、文档名等</para>
    /// </summary>
    [JsonPropertyName("object_name")]
    public string? ObjectName { get; set; }

    /// <summary>
    /// <para>操作对象所有者，当前针对文档类型开放</para>
    /// </summary>
    [JsonPropertyName("object_owner")]
    public string? ObjectOwner { get; set; }

    /// <summary>
    /// <para>操作对象扩展字段，字段详情见飞书枚举值列表附录</para>
    /// </summary>
    [JsonPropertyName("object_detail")]
    public SecurityAuditObjectDetail? ObjectDetail { get; set; }
}
