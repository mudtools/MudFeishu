namespace Mud.Feishu.DataModels.Users;

public class UserData
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("en_name")]
    public string EnName { get; set; }

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonPropertyName("avatar_thumb")]
    public string AvatarThumb { get; set; }

    [JsonPropertyName("avatar_middle")]
    public string AvatarMiddle { get; set; }

    [JsonPropertyName("avatar_big")]
    public string AvatarBig { get; set; }

    [JsonPropertyName("open_id")]
    public string OpenId { get; set; }

    [JsonPropertyName("union_id")]
    public string UnionId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("enterprise_email")]
    public string EnterpriseEmail { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; }

    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; }

    [JsonPropertyName("employee_no")]
    public string EmployeeNo { get; set; }
}
