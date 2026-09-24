// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 执行数据知识问答（Ask）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AskDataKnowledgeRequest
{
    /// <summary>
    /// <para>输入消息（message 包含 content 参数，当前仅支持纯文本输入）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("message")]
    public AskDataKnowledgeMessage Message { get; set; } = new();

    /// <summary>
    /// <para>控制知识问答所依据的数据知识范围，默认值为空，此时不限制数据知识范围</para>
    /// <para>必填：否</para>
    /// <para>示例值：["asset_aadg2b5os5wjg"]</para>
    /// <para>最大长度：65535</para>
    /// </summary>
    [JsonPropertyName("data_asset_ids")]
    public string[]? DataAssetIds { get; set; }

    /// <summary>
    /// <para>控制知识问答所依据的数据知识分类范围，默认值为空，此时不限制数据知识分类范围</para>
    /// <para>必填：否</para>
    /// <para>示例值：["spring_5862e4fea8__c__asset_tag_aadg2b5ql4gbs"]</para>
    /// <para>最大长度：65535</para>
    /// </summary>
    [JsonPropertyName("data_asset_tag_ids")]
    public string[]? DataAssetTagIds { get; set; }
}
