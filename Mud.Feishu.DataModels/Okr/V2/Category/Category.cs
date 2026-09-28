// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 分类信息
/// </summary>
public class Category
{
    /// <summary>
    /// <para>分类 id</para>
    /// <para>示例值：7342342398472398473</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>分类创建时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>分类更新时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>分类类型：person 个人、team 团队</para>
    /// <para>示例值：person</para>
    /// </summary>
    [JsonPropertyName("category_type")]
    public string? CategoryType { get; set; }

    /// <summary>
    /// <para>是否启用</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// <para>颜色：blue 蓝色、purple 紫色、wathet 浅蓝、turquoise 青绿、indigo 靛蓝、orange 橙色</para>
    /// <para>示例值：blue</para>
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// <para>分类名称（多语言）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public CategoryName? Name { get; set; }
}
