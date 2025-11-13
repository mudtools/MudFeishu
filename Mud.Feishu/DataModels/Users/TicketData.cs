namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// jsapi_ticket 响应数据
/// </summary>
public class TicketData
{
    /// <summary>
    /// ticket 的有效时间（单位：秒）
    /// </summary>
    [JsonPropertyName("expire_in")]
    public int ExpireIn { get; set; }

    /// <summary>
    /// jsapi_ticket，调用 JSAPI 的临时票据
    /// </summary>
    [JsonPropertyName("ticket")]
    public string? Ticket { get; set; }
}