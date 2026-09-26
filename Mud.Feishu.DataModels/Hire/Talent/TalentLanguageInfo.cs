// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才语言能力（获取人才 v1 详情/列表响应 language_list 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentLanguageInfo
{
    /// <summary>
    /// <para>语言能力 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>语言：1 英语 … 24 普通话 / 25 粤语</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("language")]
    public int? Language { get; set; }

    /// <summary>
    /// <para>熟练度：1 入门 / 2 日常交流 / 3 商务会议 / 4 无障碍沟通 / 5 母语</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("proficiency")]
    public int? Proficiency { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public TalentCustomizedDataChild[]? CustomizedDataList { get; set; }
}
