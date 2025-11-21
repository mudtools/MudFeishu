// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 分页搜索员工信息请求体
/// </summary>
public class EmployeePageQueryRequest
{
    /// <summary>
    /// 搜索关键词。支持员工ID、员工名称、员工手机号、员工邮箱的搜索。
    /// <para>其中员工ID、员工手机号支持精确搜索，员工名称、员工邮箱支持模糊搜索，员工名称支持国际化名称的搜索。</para>
    /// </summary>
    [JsonPropertyName("query")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? Query
    { get; set; }

    /// <summary>
    /// 分页信息
    /// </summary>
    [JsonPropertyName("page_request")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        PageRequest PageRequest
    { get; set; } = new PageRequest();

    /// <summary>
    /// 需要查询的字段列表。将按照传递的字段列表返回有权限的行、列数据。不传则不会返回任何字段
    /// <para>示例值：["base_info.name.name"]</para>
    /// </summary>
    [JsonPropertyName("required_fields")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        List<string> RequiredFields
    { get; set; } = [];
}
