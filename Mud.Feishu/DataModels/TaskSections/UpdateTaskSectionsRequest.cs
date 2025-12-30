// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskSections;

/// <summary>
/// 更新自定义分组请求体
/// </summary>
public class UpdateTaskSectionsRequest
{
    /// <summary>
    /// <para>要更新的自定义分组的数据。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("section")]
    public UpdateSectionData Section { get; set; } = new();

    /// <summary>
    /// <para>要更新的字段名，支持：</para>
    /// <para>* `name` - 自定义字段名字</para>
    /// <para>* `insert_before` - 要让当前自定义分组放到某个自定义分组前面的secion_guid，用于改变当前自定义分组的位置。</para>
    /// <para>* `insert_after` - 要让当前自定义分组放到某个自定义分组后面的secion_guid，用于改变当前自定义分组的位置。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[] UpdateFields { get; set; } = [];
}


/// <summary>
/// <para>要更新的自定义分组的数据。</para>
/// </summary>
public class UpdateSectionData
{
    /// <summary>
    /// <para>自定义分组名。如更新，不允许设为空，支持最大100个utf8字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：已经审核过的任务</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>要将新分组插入到自定义分分组的前面的目标分组的guid。</para>
    /// <para>必填：否</para>
    /// <para>示例值：e6e37dcc-f75a-5936-f589-12fb4b5c80c2</para>
    /// </summary>
    [JsonPropertyName("insert_before")]
    public string? InsertBefore { get; set; }

    /// <summary>
    /// <para>要将新分组插入到自定义分分组的后面的目标分组的guid。</para>
    /// <para>必填：否</para>
    /// <para>示例值：e6e37dcc-f75a-5936-f589-12fb4b5c80c2</para>
    /// </summary>
    [JsonPropertyName("insert_after")]
    public string? InsertAfter { get; set; }
}