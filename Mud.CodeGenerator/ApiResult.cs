namespace Mud.Feishu;

/// <summary>
/// API响应结果模型
/// </summary>
public class ApiResult
{
    /// <summary>
    /// 错误码，0表示成功，非 0 取值表示失败
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 错误描述
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }
}

/// <summary>
/// API响应结果模型
/// </summary>
public class ApiResult<T> : ApiResult
    where T : class, new()
{
    /// <summary>
    /// 响应结果数据对象。
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
