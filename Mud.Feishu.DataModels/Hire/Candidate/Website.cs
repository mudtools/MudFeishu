// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 招聘官网（获取官网列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class Website
{
    /// <summary>
    /// <para>官网 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>流程类型：1 社招 / 2 校招</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_type_list")]
    public int[]? ProcessTypeList { get; set; }

    /// <summary>
    /// <para>招聘渠道 ID，每个官网拥有唯一的招聘渠道 ID，可用于职位发布至官网</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_channel_id")]
    public string? JobChannelId { get; set; }
}
