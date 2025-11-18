namespace Mud.Feishu.DataModels;

public class SearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; }
}