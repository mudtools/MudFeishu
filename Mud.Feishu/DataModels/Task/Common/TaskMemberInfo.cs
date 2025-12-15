// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;


/// <summary>
/// <para>任务成员数据结构。遵循member格式，只支持user类型）。</para>
/// </summary>
public class TaskMember
{
    /// <summary>
    /// <para>表示member的id</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>成员的类型，可以是user或者app。</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>默认值：user</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>成员角色，可以是"assignee"或者"follower"。</para>
    /// <para>必填：是</para>
    /// <para>示例值：assignee</para>
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}


/// <summary>
/// <para>任务成员数据结构。遵循member格式，只支持user类型）。</para>
/// </summary>
public class TaskMemberInfo : TaskMember
{
    /// <summary>
    /// <para>成员名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张明德（明德）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

