namespace Mud.Feishu.DataModels;

/// <summary>
/// 列表数据响应结果。
/// </summary>
public class ListApiResult
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
