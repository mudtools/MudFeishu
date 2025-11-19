// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工查询请求体。
/// </summary>
public class EmployeeQueryRequest
{
    /// <summary>
    /// 员工ID，与employee_id_type类型保持一致
    /// </summary>
    [JsonPropertyName("employee_ids")]
    public required List<string> EmployeeIds { get; set; } = [];

    /// <summary>
    /// 需要查询的字段列表。将按照传递的字段列表返回有权限的行、列数据。不传则不会返回任何字段。
    /// </summary>
    [JsonPropertyName("required_fields")]
    public required List<string> RequiredFields { get; set; } = [];
}