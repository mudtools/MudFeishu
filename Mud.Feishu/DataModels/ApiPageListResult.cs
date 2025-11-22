// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// 分页列表数据响应结果。
/// </summary>
public class ApiPageListResult
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

/// <summary>
/// 分页列表数据响应结果。
/// </summary>
/// <typeparam name="T">响应结果类型</typeparam>
public class ApiPageListResult<T> : ApiPageListResult
{
    /// <summary>
    /// 响应结果的列表数据集合。
    /// </summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];
}

/// <summary>
/// 列表数据响应结果。
/// </summary>
/// <typeparam name="T">响应结果类型</typeparam>
public class ApiListResult<T>
{
    /// <summary>
    /// 响应结果的列表数据集合。
    /// </summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];
}

