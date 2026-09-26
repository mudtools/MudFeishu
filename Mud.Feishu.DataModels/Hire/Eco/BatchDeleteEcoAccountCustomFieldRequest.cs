// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 批量删除账号自定义字段请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BatchDeleteEcoAccountCustomFieldRequest
{
    /// <summary>
    /// <para>适用范围：1（背调）、2（笔试）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public int? Scope { get; set; }

    /// <summary>
    /// <para>要删除的自定义字段的 key 列表；不支持删除全部字段（须至少保留一个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["account_token_v1"]</para>
    /// </summary>
    [JsonPropertyName("custom_field_key_list")]
    public string[]? CustomFieldKeyList { get; set; }
}
