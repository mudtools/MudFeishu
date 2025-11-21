// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组信息请求体。
/// </summary>
public class UserGroupInfoRequest
{
    /// <summary>
    /// 用户组名字，长度不能超过 100 字符。
    /// </summary>
    [JsonPropertyName("name")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? Name
    { get; set; }

    /// <summary>
    /// 用户组描述，长度不能超过 500 字符。
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 用户组的类型。默认取值 1，表示普通用户组。2：暂不支持使用该值
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// 自定义用户组 ID。
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }
}
