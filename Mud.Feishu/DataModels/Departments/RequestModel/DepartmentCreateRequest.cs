// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 创建部门请求体。
/// </summary>
public class DepartmentCreateRequest : DepartmentRequestBase
{
    /// <summary>
    /// 自定义部门 ID。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 部门绑定的单位自定义 ID 列表，当前只支持绑定一个单位。
    /// </summary>
    [JsonPropertyName("unit_ids")]
    public List<string> UnitIds { get; set; } = [];

    /// <summary>
    /// 部门 HRBP 的用户 ID 列表。 ID 类型与查询参数 user_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("department_hrbps")]
    public List<string> DepartmentHrbps { get; set; } = [];
}
