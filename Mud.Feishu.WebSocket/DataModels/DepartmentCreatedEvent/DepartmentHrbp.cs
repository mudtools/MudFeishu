
namespace Mud.Feishu.WebSocket.DataModels.DepartmentCreatedEvent;
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