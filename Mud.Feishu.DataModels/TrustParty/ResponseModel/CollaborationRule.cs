// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 可搜可见规则（关联组织协作规则）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class CollaborationRule
{
    /// <summary>
    /// 规则ID
    /// </summary>
    [JsonPropertyName("rule_id")]
    public string? RuleId { get; set; }

    /// <summary>
    /// 规则主体（我方组织内的可见实体），实体数量之和需小于100
    /// </summary>
    [JsonPropertyName("subjects")]
    public CollaborationRuleEntities? Subjects { get; set; }

    /// <summary>
    /// 规则主体超出分享范围时为 false，且主体不返回
    /// </summary>
    [JsonPropertyName("subject_is_valid")]
    public bool? SubjectIsValid { get; set; }

    /// <summary>
    /// 规则客体（对方组织内的可见实体），实体数量之和需小于100
    /// </summary>
    [JsonPropertyName("objects")]
    public CollaborationRuleEntities? Objects { get; set; }

    /// <summary>
    /// 规则客体超出分享范围时为 false，且客体不返回
    /// </summary>
    [JsonPropertyName("object_is_valid")]
    public bool? ObjectIsValid { get; set; }
}
