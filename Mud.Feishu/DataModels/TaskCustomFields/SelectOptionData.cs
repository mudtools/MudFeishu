// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 选项
/// </summary>
public class SelectOptionData
{
    /// <summary>
    /// <para>选项名称，不能为空，最大50个字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：高优</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>选项的颜色索引值，取值0～54。如不填写会自动从未使用的颜色索引值中随机选一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：54</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("color_index")]
    public int? ColorIndex { get; set; }

    /// <summary>
    /// <para>选项是否隐藏。隐藏后的选项在界面不可见，也不可以再通过openapi将字段值设为该选项。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_hidden")]
    public bool? IsHidden { get; set; }
}
