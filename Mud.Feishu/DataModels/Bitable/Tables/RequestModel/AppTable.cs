// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>数据表</para>
/// </summary>
public class AppTable
{
    /// <summary>
    /// <para>数据表名称。该字段必填。</para>
    /// <para>**注意**：</para>
    /// <para>- 名称中的首尾空格将会被默认去除</para>
    /// <para>- 数据表名称不可以包含 `/ \ ? * : [ ]` 等特殊字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：一个新的数据表</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>默认表格视图的名称。</para>
    /// <para>注意：</para>
    /// <para>- 名称中的首尾空格将会被去除</para>
    /// <para>- 名称中不允许包含 [ ] 两个字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：表格视图</para>
    /// </summary>
    [JsonPropertyName("default_view_name")]
    public string? DefaultViewName { get; set; }

    /// <summary>
    /// <para>数据表的初始字段。</para>
    /// <para>**注意**：</para>
    /// <para>- 如果传入了 `default_view_name` 字段，则必须传入 `fields` 字段</para>
    /// <para>- 如果不传 `default_view_name` 字段，则 `fields` 字段为可选字段</para>
    /// <para>- 若 `default_view_name` 字段和 `fields` 字段都不传，将会创建一个仅包含索引字段的空数据表。</para>
    /// <para>- 数据表的第一个字段为索引字段。索引字段仅支持以下类型：</para>
    /// <para>- 1：多行文本</para>
    /// <para>- 2：数字</para>
    /// <para>- 5：日期</para>
    /// <para>- 13：电话号码</para>
    /// <para>- 15：超链接</para>
    /// <para>- 20：公式</para>
    /// <para>- 22：地理位置</para>
    /// <para>必填：否</para>
    /// <para>最大长度：300</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public AppTableHeader[]? Fields { get; set; }


}