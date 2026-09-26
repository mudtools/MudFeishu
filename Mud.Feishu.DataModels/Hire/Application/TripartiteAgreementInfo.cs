// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 三方协议信息（获取三方协议列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TripartiteAgreementInfo
{
    /// <summary>
    /// <para>三方协议 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>三方协议状态：1 未开始 / 2 已申请 / 3 学生处理中 / 4 公司处理中 / 5 学校处理中 / 6 已终止 / 7 已完成 / 8 解约处理中 / 9 已解约</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("state")]
    public int? State { get; set; }

    /// <summary>
    /// <para>创建时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>修改时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public string? ModifyTime { get; set; }
}
