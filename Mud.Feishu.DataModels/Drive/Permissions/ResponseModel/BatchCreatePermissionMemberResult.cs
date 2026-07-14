// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 批量增加协作者权限响应体
/// <para>批量添加协作者权限的响应体，包含每个协作者的权限信息。</para>
/// </summary>
public class BatchCreatePermissionMemberResult
{
    /// <summary>
    /// <para>添加成功的协作者列表</para>
    /// <para>**注意**：当只有部分成功时，`members` 会返回成功的部分，在不同场景下，部分成功返回的错误码可能不同，请不要依赖错误码去判断是否为部分成功</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("members")]
    public PermissionMember[] Members { get; set; } = [];
}