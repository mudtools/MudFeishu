// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>新增记录的内容</para>
/// </summary>
public class AppTableRecordInfo
{
    /// <summary>
    /// <para>成功新增的记录的数据</para>
    /// <para>必填：是</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public object Fields { get; set; } = new();

    /// <summary>
    /// <para>新增记录的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("record_id")]
    public string? RecordId { get; set; }

    /// <summary>
    /// <para>该记录的创建人信息。本接口不返回该参数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("created_by")]
    public OpsPerson? CreatedBy { get; set; }

    /// <summary>
    /// <para>该记录的创建时间。本接口不返回该参数</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("created_time")]
    public long? CreatedTime { get; set; }

    /// <summary>
    /// <para>该记录最近一次更新的修改人。本接口不返回该参数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("last_modified_by")]
    public OpsPerson? LastModifiedBy { get; set; }

    /// <summary>
    /// <para>该记录最近一次的更新时间。本接口不返回该参数</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("last_modified_time")]
    public long? LastModifiedTime { get; set; }

    /// <summary>
    /// <para>记录分享链接，本接口不返回该参数，批量获取记录接口将返回该参数</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.example.com/record/WVoXrzIaqeorcJcHgzAcg8AQnNd</para>
    /// </summary>
    [JsonPropertyName("shared_url")]
    public string? SharedUrl { get; set; }

    /// <summary>
    /// <para>记录链接，本接口不返回该参数，查询记录接口将返回该参数</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.example.com/record/WVoXrzIaqeorcJcHgzAcg8AQnNd</para>
    /// </summary>
    [JsonPropertyName("record_url")]
    public string? RecordUrl { get; set; }
}