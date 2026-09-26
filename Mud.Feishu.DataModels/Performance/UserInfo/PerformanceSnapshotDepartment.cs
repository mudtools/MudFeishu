// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效周期人员部门快照
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceSnapshotDepartment
{
    /// <summary>
    /// <para>部门 ID，与入参 department_id_type 类型一致</para>
    /// <para>示例值：od-4e6ac4d14bcd5071a37a39de902c7141</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>部门名称（下划线形态 zh_cn / en_us）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }
}
