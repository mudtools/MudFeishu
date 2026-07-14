// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// 批量创建数据项索引结果
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class BatchCreateDataItemIndexResult
{
    /// <summary>
    /// 操作结果列表
    /// </summary>
    [JsonPropertyName("result")]
    public List<ResultItem> Result { get; set; } = [];
}


/// <summary>
/// 单个操作结果项
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class ResultItem
{
    /// <summary>
    /// 错误信息，成功时为空字符串
    /// </summary>
    [JsonPropertyName("err")]
    public string? Err { get; set; }

    /// <summary>
    /// 是否操作成功
    /// </summary>
    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 项目唯一标识
    /// </summary>
    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }
}
