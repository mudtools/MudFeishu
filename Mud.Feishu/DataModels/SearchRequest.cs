namespace Mud.Feishu.DataModels;

/// <summary>
/// 搜索请求基础模型，用于包含搜索查询参数。
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// 搜索关键词，匹配字段为部门名称（不支持匹配部门国际化名称）。
    /// <para>示例值："DemoName"</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}