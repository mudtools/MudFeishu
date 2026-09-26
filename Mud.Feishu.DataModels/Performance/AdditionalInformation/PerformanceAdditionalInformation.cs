// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 被评估人补充信息（additional_information）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceAdditionalInformation
{
    /// <summary>
    /// <para>补充信息 ID；传入系统中已有的补充信息 ID 时执行更新</para>
    /// <para>必填：否（导入时），长度 1~100 字符</para>
    /// <para>示例值：7350194523185610771</para>
    /// </summary>
    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }

    /// <summary>
    /// <para>外部系统补充信息 ID，用于系统间的数据映射；传入已有 external_id 时执行更新</para>
    /// <para>必填：否（导入时），长度 0~100 字符</para>
    /// <para>示例值：6789523104723558912</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>被评估人 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：是（导入时），长度 1~9999 字符</para>
    /// <para>示例值：ou_3245842393d09e9428ad4655da6e30b3</para>
    /// </summary>
    [JsonPropertyName("reviewee_user_id")]
    public string? RevieweeUserId { get; set; }

    /// <summary>
    /// <para>事项</para>
    /// <para>必填：是（导入时），长度 1~1000 字符</para>
    /// <para>示例值：业绩补充说明</para>
    /// </summary>
    [JsonPropertyName("item")]
    public string? Item { get; set; }

    /// <summary>
    /// <para>时间，文本内容、无格式校验</para>
    /// <para>必填：是（导入时），长度 1~100 字符</para>
    /// <para>示例值：2024-03-12</para>
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// <para>具体描述</para>
    /// <para>必填：是（导入时），长度 1~5000 字符</para>
    /// <para>示例值：销售额增长目标超额完成</para>
    /// </summary>
    [JsonPropertyName("detailed_description")]
    public string? DetailedDescription { get; set; }
}
