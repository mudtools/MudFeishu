// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary></summary>
public class SchemaSortOptions
{
    /// <summary>
    /// <para>排序的优先级，可选范围为 0~4，0为最高优先级。如果优先级相同，则随机进行排序。默认为0</para>
    /// <para>**示例值**：0</para>
    /// <para>**可选值有**：</para>
    /// <para>0:最高优先级,1:次高优先级,2:次次高优先级,3:次低优先级,4:最低优先级</para>
    /// <para>**默认值**：`0`</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `4`</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：最高优先级</item>
    /// <item>1：次高优先级</item>
    /// <item>2：次次高优先级</item>
    /// <item>3：次低优先级</item>
    /// <item>4：最低优先级</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// <para>排序的顺序。默认为 desc</para>
    /// <para>**示例值**："asc"</para>
    /// <para>**可选值有**：</para>
    /// <para>asc:升序,desc:降序</para>
    /// <para>**默认值**：`desc`</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>asc：升序</item>
    /// <item>desc：降序</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("order")]
    public string? Order { get; set; }
}
