// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 创建会议室层级请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class CreateRoomLevelRequest
{
    /// <summary>
    /// <para>层级名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：测试层级</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>父层级ID。</para>
    /// <para>**说明**：如需在租户层级（即根层级）下创建会议室层级，可以先调用[查询会议室层级详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room_level/get)接口，将路径参数 `room_level_id` 传入 `0` 进行查询，返回结果中的 `room_level_id` 值即为根层级 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：omb_4ad1a2c7a2fbc5fc9570f38456931293</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>自定义层级ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：10000</para>
    /// </summary>
    [JsonPropertyName("custom_group_id")]
    public string? CustomGroupId { get; set; }
}
