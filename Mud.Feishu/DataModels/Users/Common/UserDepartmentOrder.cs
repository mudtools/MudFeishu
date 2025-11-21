// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户部门排序。
/// </summary>
public class UserDepartmentOrder
{
    /// <summary>
    /// 排序信息对应的部门 ID。表示用户所在的、且需要排序的部门。
    /// </summary>
    [JsonPropertyName("department_id")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? DepartmentId
    { get; set; }

    /// <summary>
    /// 用户在其直属部门内的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("user_order")]
    public int? UserOrder { get; set; }

    /// <summary>
    /// 用户所属的多个部门之间的排序。数值越大，排序越靠前。
    /// </summary>
    [JsonPropertyName("department_order")]
    public int? DepartmentOrderValue { get; set; }

    /// <summary>
    /// 是否为主部门。
    /// </summary>
    [JsonPropertyName("is_primary_dept")]
    public bool IsPrimaryDept { get; set; }
}