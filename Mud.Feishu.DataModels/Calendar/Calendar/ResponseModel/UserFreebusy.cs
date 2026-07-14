// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>用户忙闲信息列表。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class UserFreebusy
{
    /// <summary>
    /// <para>在请求的时间区间内的忙闲时间段信息。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("freebusy_items")]
    public Freebusy[]? FreebusyItems { get; set; }

    /// <summary>
    /// <para>日历创建者的用户 ID，根据查询参数 user_id_type 设置的 ID 类型进行返回。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_c186b6833e2d5faf2bc587e71ddabcef</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
