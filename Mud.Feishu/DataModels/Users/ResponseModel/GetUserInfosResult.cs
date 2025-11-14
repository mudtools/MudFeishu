namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 获取用户详细信息的返回结果数组
/// </summary>
public class GetUserInfosResult
{
    /// <summary>
    /// 获取用户详细信息列表。
    /// </summary>
    [JsonPropertyName("items")]
    public GetUserInfoResult[] Items { get; set; } = [];
}

/// <summary>
/// 根据部门ID获取用户详细信息的返回结果数组
/// </summary>
public class GetDepartmentUserInfosResult : GetUserInfosResult
{
    /// <summary>
    /// 是否还有更多项
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    /// <summary>
    /// 分页标记，当 has_more 为 true 时，会同时返回新的 page_token，否则不返回 page_token
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }
}