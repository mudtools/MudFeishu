// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 批量查询补充信息请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryAdditionalInformationListRequest
{
    /// <summary>
    /// <para>评估周期 ID，长度 1~100 字符；三个筛选参数均为空时返回该周期的全部补充信息</para>
    /// <para>必填：是</para>
    /// <para>示例值：7348736302176534547</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>补充信息 ID 列表（0~50 个），优先级 item_ids &gt; external_ids &gt; reviewee_user_ids</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7350195758357807123"]</para>
    /// </summary>
    [JsonPropertyName("item_ids")]
    public string[]? ItemIds { get; set; }

    /// <summary>
    /// <para>外部系统补充信息 ID 列表（0~50 个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["6789523104723558912"]</para>
    /// </summary>
    [JsonPropertyName("external_ids")]
    public string[]? ExternalIds { get; set; }

    /// <summary>
    /// <para>被评估人 ID 列表（0~50 个），与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_3245842393d09e9428ad4655da6e30b3"]</para>
    /// </summary>
    [JsonPropertyName("reviewee_user_ids")]
    public string[]? RevieweeUserIds { get; set; }
}
