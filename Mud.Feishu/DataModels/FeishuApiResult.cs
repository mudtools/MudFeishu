// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
/// API分页列表响应结果模型
/// </summary>
/// <typeparam name="T"></typeparam>
public class FeishuApiPageListResult<T> : FeishuApiResult<ApiPageListResult<T>>
{

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
