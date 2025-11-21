// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 部门与单位的绑定关系请求体
/// </summary>
public class UnitBindDepartmentRequest
{
    /// <summary>
    /// 单位 ID。
    /// </summary>
    [JsonPropertyName("unit_id")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? UnitId
    { get; set; }

    /// <summary>
    /// 单位关联的部门 ID，ID 类型与 department_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("department_id")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? DepartmentId
    { get; set; }

    /// <summary>
    /// 此次调用中的部门 ID 类型。
    /// </summary>
    [JsonPropertyName("department_id_type")]
    public string? DepartmentIdType { get; set; } = "open_department_id";
}
