// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Bitable;

/// <summary>
/// 发生变更后的字段
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class BitableTableRecordActionField
{
    /// <summary>
    /// <para>发生变更的字段 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>发生变更前的字段值。该字段为 JSON 序列化后的字符串，序列化前的结构请参考 [数据结构](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/bitable/development-guide/bitable-structure)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_value")]
    public string? FieldValue { get; set; }

    /// <summary>
    /// <para>人员字段补充信息。有人员、创建人、修改人类型字段变更时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_identity_value")]
    public BitableTableRecordActionFieldIdentity? FieldIdentityValue { get; set; }

}
