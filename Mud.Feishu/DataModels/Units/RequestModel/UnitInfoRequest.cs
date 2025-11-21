// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位信息请求体
/// </summary>
public class UnitInfoRequest
{
    /// <summary>
    /// 自定义单位 ID，租户内唯一，创建后不可修改。
    /// <para>默认值：空，若不传值则由系统自动生成一个默认 ID。</para>
    /// <para>示例值："BU121"</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string? UnitId { get; set; }

    /// <summary>
    /// 单位名字。
    /// </summary>
    /// <remarks>
    /// <para>注意：在租户内，传入的 name 和 unit_type 不允许同时重复。例如，已存在一个名字 A、类型 A的单位，此时再创建一个名字 A、类型 A 的单位将会创建失败。</para>
    /// <para>示例值："消费者事业部"</para>
    /// </remarks>
    [JsonPropertyName("name")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? Name
    { get; set; }

    /// <summary>
    /// 自定义单位类型，创建后不可修改。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注意：在租户内，传入的 name 和 unit_type 不允许同时重复。例如，已存在一个名字 A、类型 A的单位，此时再创建一个名字 A、类型 A 的单位将会创建失败。
    /// </para>
    /// <para>示例值："子公司"</para>
    /// </remarks>
    [JsonPropertyName("unit_type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? UnitType
    { get; set; }
}
