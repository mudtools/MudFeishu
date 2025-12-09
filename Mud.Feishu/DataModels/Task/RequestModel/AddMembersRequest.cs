// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// 添加任务成员请求体
/// </summary>
public class AddMembersRequest
{
    /// <summary>
    /// <para>要添加的members列表，单请求支持最大50个成员（去重后)。关于member的格式。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("members")]
    public TaskMemberInfo[] Members { get; set; } = [];

    /// <summary>
    /// <para>幂等token，如果提供则实现幂等行为。</para>
    /// <para>必填：否</para>
    /// <para>示例值：6d99f59c-4d7d-4452-98d6-3d0556393cf6</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：10</para>
    /// </summary>
    [JsonPropertyName("client_token")]
    public string? ClientToken { get; set; }
}
