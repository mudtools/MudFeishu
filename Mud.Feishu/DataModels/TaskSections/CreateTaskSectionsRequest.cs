// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskSections;

/// <summary>
/// 创建自定义分组 请求体
/// </summary>
public class CreateTaskSectionsRequest
{
    /// <summary>
    /// <para>自定义分组名。不允许为空，最大100个utf8字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：已经审核过的任务</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>自定义分组的资源类型，支持"tasklist"（清单）或者"my_tasks"（我负责的）。</para>
    /// <para>必填：是</para>
    /// <para>示例值：tasklist</para>
    /// <para>默认值：tasklist</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// <para>自定义分组要归属的资源id。当`resource_type`为"tasklist"时这里需要填写清单的GUID；当`resource_type`为"my_tasks"时，无需填写。</para>
    /// <para>必填：否</para>
    /// <para>示例值：cc371766-6584-cf50-a222-c22cd9055004</para>
    /// </summary>
    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; set; }

    /// <summary>
    /// <para>要将新分组插入到自定义分分组的前面的目标分组的guid。</para>
    /// <para>`insert_before`和`insert_after`均不设置时表示将新分组放到已有的所有自定义分组之后。</para>
    /// <para>如果同时设置`insert_before`和`insert_after`，接口会报错。</para>
    /// <para>必填：否</para>
    /// <para>示例值：e6e37dcc-f75a-5936-f589-12fb4b5c80c2</para>
    /// </summary>
    [JsonPropertyName("insert_before")]
    public string? InsertBefore { get; set; }

    /// <summary>
    /// <para>要将新分组插入到自定义分分组的后面的目标分组的guid。</para>
    /// <para>`insert_before`和`insert_after`均不设置时表示将新分组放到已有的所有自定义分组之后。</para>
    /// <para>如果同时设置`insert_before`和`insert_after`，接口会报错。</para>
    /// <para>必填：否</para>
    /// <para>示例值：e6e37dcc-f75a-5936-f589-12fb4b5c80c2</para>
    /// </summary>
    [JsonPropertyName("insert_after")]
    public string? InsertAfter { get; set; }
}
