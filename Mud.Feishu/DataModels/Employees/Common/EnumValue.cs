// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 枚举字段值
/// </summary>
public class EnumValue
{
    /// <summary>
    /// 选项结果ID  示例值：["1"]
    /// </summary>
    [JsonPropertyName("enum_ids")]
    public List<string> EnumIds { get; set; } = [];

    /// <summary>
    /// 选项名
    /// </summary>
    [JsonPropertyName("enum_name")]
    public object? EnumName { get; set; }

    /// <summary>
    /// 选项类型 可选值有：1：文本  2：图片
    /// </summary>
    [JsonPropertyName("enum_type")]
    public required string EnumType { get; set; }
}
