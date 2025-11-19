namespace Mud.Feishu.DataModels;

/// <summary>
/// API响应结果模型
/// </summary>
public class FeishuApiResult
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
public class FeishuApiResult<T> : FeishuApiResult
    where T : class
{
    /// <summary>
    /// 响应结果数据对象。
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// API列表响应结果模型
/// </summary>
/// <typeparam name="T"></typeparam>
public class FeishuApiListResult<T> : FeishuApiResult<ApiListResult<T>>
{

}

/// <summary>
/// API响应结果中data数据为空的模型
/// </summary>
public class FeishuNullDataApiResult : FeishuApiResult<object>
{

}
