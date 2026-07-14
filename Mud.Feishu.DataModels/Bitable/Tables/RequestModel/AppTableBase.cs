// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>tables</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppTableBase
{
    /// <summary>
    /// <para>数据表名称。该字段必填。</para>
    /// <para>**注意**：</para>
    /// <para>- 名称中的首尾空格将会被默认去除。</para>
    /// <para>- 数据表名称不可以包含 `/ \ ? * : [ ]` 等特殊字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：一个新的数据表</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
