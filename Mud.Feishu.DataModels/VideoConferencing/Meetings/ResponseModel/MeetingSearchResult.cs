// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 返回结果列表
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingSearchResult
{
    /// <summary>
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>必填：否</para>
    /// <para>示例值：6911188411932033028</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>包含基本信息的卡片，用户搜索关键词命中的文本片段，使用&lt;h&gt;&lt;/h&gt;标签包裹标注</para>
    /// <para>必填：否</para>
    /// <para>示例值：会议名 \n 片段1＜h&gt;搜索词/h&gt;片段2\n 会议时间 | 组织者：组织者姓名 | ID: 会议ID</para>
    /// </summary>
    [JsonPropertyName("display_info")]
    public string? DisplayInfo { get; set; }

    /// <summary>
    /// <para>会议元信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meta_data")]
    public MeetingMeta? MetaData { get; set; }


}
