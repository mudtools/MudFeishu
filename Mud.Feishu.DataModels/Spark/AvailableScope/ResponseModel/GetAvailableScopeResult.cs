// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 获取妙搭产品使用权限响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class GetAvailableScopeResult
{
    /// <summary>
    /// <para>使用权限范围类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：ALLOW_PART</para>
    /// <para>可选值：<list type="bullet">
    /// <item>ALLOW_ALL：全部允许</item>
    /// <item>DENY_ALL：全部拒绝</item>
    /// <item>ALLOW_PART：部分允许（仅 department_ids/user_ids 指定范围）</item>
    /// <item>DENY_PART：部分拒绝（仅 department_ids/user_ids 指定范围）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("available_scope")]
    public string? AvailableScope { get; set; }

    /// <summary>
    /// <para>部门 ID 列表（仅 ALLOW_PART/DENY_PART 时有效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["dpt_12345"]</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public string[]? DepartmentIds { get; set; }

    /// <summary>
    /// <para>成员 ID 列表（仅 ALLOW_PART/DENY_PART 时有效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["usr_67890"]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }
}
