/// <summary>
/// 消息提及对象类，用于表示飞书消息中提及的用户或群组信息
/// </summary>
public class MessageMention
{
    /// <summary>
    /// 提及项的唯一标识键值
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// 提及对象的ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// ID类型，用于区分是用户还是群组等不同类型的标识
    /// </summary>
    [JsonPropertyName("id_type")]
    public string? IdType { get; set; }

    /// <summary>
    /// 提及对象的名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 租户密钥，用于多租户环境下的身份识别
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }
}