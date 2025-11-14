namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 根据部门ID获取用户详细信息的返回结果数组
/// </summary>
public class GetDepartmentUserInfosResult : ListApiResult
{
    /// <summary>
    /// 获取用户详细信息列表。
    /// </summary>
    [JsonPropertyName("items")]
    public GetUserInfoResult[] Items { get; set; } = [];
}