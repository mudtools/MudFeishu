// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 投递阶段信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ApplicationStageInfo
{
    /// <summary>
    /// <para>阶段 ID，可通过获取招聘流程信息接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>阶段中文名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("zh_name")]
    public string? ZhName { get; set; }

    /// <summary>
    /// <para>阶段英文名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>阶段类型：1 筛选型 / 2 评估型 / 3 笔试型 / 4 面试型 / 5 Offer型 / 6 待入职 / 7 已入职 / 8 其它类型 / 255 系统默认</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }
}
