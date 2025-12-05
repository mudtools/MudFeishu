// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Organization;
/// <summary>
/// 部门HRBP（人力资源业务合作伙伴）信息类
/// 用于存储和传输飞书平台中与部门相关的人力资源业务合作伙伴的信息
/// </summary>
public class DepartmentHrbp
{
    /// <summary>
    /// 用户在飞书平台的唯一标识（Union ID）
    /// 用于跨应用识别用户身份的全局唯一标识符
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// 用户在当前企业内的用户ID
    /// 用于在当前企业内标识用户的唯一标识符
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户在当前应用中的开放ID
    /// 用于在当前应用范围内标识用户的唯一标识符
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }
}