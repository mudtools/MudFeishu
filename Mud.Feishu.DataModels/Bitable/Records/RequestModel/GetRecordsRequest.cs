// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 批量获取记录请求体
/// </summary>
public class GetRecordsRequest
{
    /// <summary>
    /// <para>记录 ID 列表。</para>
    /// <para>必填：是</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("record_ids")]
    public string[] RecordIds { get; set; } = [];

    /// <summary>
    /// <para>此次调用中使用的用户 id 的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：open_id</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user_id：以user_id来识别用户</item>
    /// <item>union_id：以union_id来识别用户</item>
    /// <item>open_id：以open_id来识别用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("user_id_type")]
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>是否返回记录的分享链接。可选值：</para>
    /// <para>- true：返回分享链接</para>
    /// <para>- false：不返回分享链接</para>
    /// <para>**默认值**：false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("with_shared_url")]
    public bool? WithSharedUrl { get; set; }

    /// <summary>
    /// <para>是否返回自动计算的字段。可选值：</para>
    /// <para>- true：返回自动计算的字段</para>
    /// <para>- false：不返回自动计算的字段</para>
    /// <para>**默认值**：false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("automatic_fields")]
    public bool? AutomaticFields { get; set; }
}