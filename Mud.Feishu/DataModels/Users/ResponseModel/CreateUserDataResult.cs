namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 创建用户操作的结果。
/// </summary>
public class CreateUserResult
{
    /// <summary>
    /// 创建的用户详细信息。
    /// </summary>
    [JsonPropertyName("user")]
    public UserDetail? User { get; set; }
}


/// <summary>
/// 用户头像信息，包含不同尺寸的头像URL。
/// </summary>
public class AvatarInfo
{
    /// <summary>
    /// 72x72像素的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_72")]
    public string? Avatar72 { get; set; }

    /// <summary>
    /// 240x240像素的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_240")]
    public string? Avatar240 { get; set; }

    /// <summary>
    /// 640x640像素的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_640")]
    public string? Avatar640 { get; set; }

    /// <summary>
    /// 原始尺寸的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_origin")]
    public string? AvatarOrigin { get; set; }
}

/// <summary>
/// 用户状态信息。
/// </summary>
public class UserStatus
{
    /// <summary>
    /// 用户是否被冻结。
    /// </summary>
    [JsonPropertyName("is_frozen")]
    public bool IsFrozen { get; set; }

    /// <summary>
    /// 用户是否已离职。
    /// </summary>
    [JsonPropertyName("is_resigned")]
    public bool IsResigned { get; set; }

    /// <summary>
    /// 用户是否已激活。
    /// </summary>
    [JsonPropertyName("is_activated")]
    public bool IsActivated { get; set; }

    /// <summary>
    /// 用户是否已退出。
    /// </summary>
    [JsonPropertyName("is_exited")]
    public bool IsExited { get; set; }

    /// <summary>
    /// 用户是否未加入。
    /// </summary>
    [JsonPropertyName("is_unjoin")]
    public bool IsUnjoin { get; set; }
}

/// <summary>
/// 用户排序信息列表。
/// </summary>
public class UserOrder
{
    /// <summary>
    /// 排序信息对应的部门 ID。表示用户所在的、且需要排序的部门。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 用户在其直属部门内的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("user_order")]
    public int? UserOrderValue { get; set; }

    /// <summary>
    /// 用户所属的多个部门之间的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("department_order")]
    public int? DepartmentOrder { get; set; }

    /// <summary>
    /// 标识是否为用户的唯一主部门，主部门为用户所属部门中排序第一的部门（department_order 最大）。
    /// </summary>
    [JsonPropertyName("is_primary_dept")]
    public bool? IsPrimaryDept { get; set; }
}

/// <summary>
/// 自定义字段。
/// </summary>
public class CustomAttribute
{
    /// <summary>
    /// 自定义字段类型。可选值有：TEXT：文本 HREF：网页 ENUMERATION：枚举 PICTURE_ENUM：图片 GENERIC_USER：用户
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 自定义字段 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 自定义字段取值。
    /// </summary>
    [JsonPropertyName("value")]
    public CustomAttributeValue? Value { get; set; }
}

/// <summary>
/// 自定义字段取值。
/// </summary>
public class CustomAttributeValue
{
   
    /// <summary>
    /// 自定义字段类型为 TEXT 时，该参数必填，用于定义字段值。
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// 自定义字段类型为 HREF 时，该参数必填，用于定义默认 URL。例如，手机端跳转小程序，PC端跳转网页。
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// 自定义字段类型为 HREF 时，该参数用于定义 PC 端 URL。
    /// <para>注意：请以 http:// 或 https:// 开头。</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// 自定义字段类型为 ENUMERATION 或 PICTURE_ENUM 时，该参数用于定义选项 ID。
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// 自定义字段类型为 GENERIC_USER 时，该参数用于定义引用人员。
    /// </summary>
    [JsonPropertyName("generic_user")]
    public GenericUser? GenericUser { get; set; }
}

/// <summary>
/// 定义引用人员。
/// </summary>
public class GenericUser
{
    /// <summary>
    /// 引用人员的用户 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// 用户类型。可选值有：1：用户
    /// </summary>
    [JsonPropertyName("type")]
    public required int Type { get; set; } = 1;
}