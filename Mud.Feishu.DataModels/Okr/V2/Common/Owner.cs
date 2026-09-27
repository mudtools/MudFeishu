// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 实体归属者
/// </summary>
public class Owner
{
    /// <summary>
    /// <para>归属者类型：user 员工</para>
    /// <para>示例值：user</para>
    /// </summary>
    [JsonPropertyName("owner_type")]
    public string? OwnerType { get; set; }

    /// <summary>
    /// <para>员工 ID，与 user_id_type 对应</para>
    /// <para>示例值：ou_3bbe8a09c20e89cce9bff989ed840674</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
