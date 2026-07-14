// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>文档统计信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class FileStatisticInfo
{
    /// <summary>
    /// <para>文档历史访问人数，同一用户（user_id）多次访问按一次计算。</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("uv")]
    public int? Uv { get; set; }

    /// <summary>
    /// <para>文档历史访问次数，同一用户（user_id）多次访问按多次计算，但同一用户在间隔在半小时内访问两次视为一次访问</para>
    /// <para>必填：否</para>
    /// <para>示例值：15</para>
    /// </summary>
    [JsonPropertyName("pv")]
    public int? Pv { get; set; }

    /// <summary>
    /// <para>文档历史点赞总数。`-1` 表示对应的文档类型不支持点赞</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("like_count")]
    public int? LikeCount { get; set; }

    /// <summary>
    /// <para>时间戳（单位：秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1627367349</para>
    /// </summary>
    [JsonPropertyName("timestamp")]
    public int? Timestamp { get; set; }

    /// <summary>
    /// <para>今日新增文档访问人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("uv_today")]
    public int? UvToday { get; set; }

    /// <summary>
    /// <para>今日新增文档访问次数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("pv_today")]
    public int? PvToday { get; set; }

    /// <summary>
    /// <para>今日新增文档点赞数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("like_count_today")]
    public int? LikeCountToday { get; set; }
}
