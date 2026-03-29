// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>视图详细信息</para>
/// </summary>
public class AppViewDetailInfo : AppViewInfo
{
    /// <summary>
    /// <para>视图公共等级</para>
    /// <para>必填：否</para>
    /// <para>示例值：Public</para>
    /// <para>可选值：<list type="bullet">
    /// <item>Public：公共视图</item>
    /// <item>Locked：锁定视图</item>
    /// <item>Private：个人视图</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("view_public_level")]
    public string? ViewPublicLevel { get; set; }

    /// <summary>
    /// <para>个人视图的所有者的 ID，ID 类型和查询参数 user_id_type 保持一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_2910013f1e6456f16a0ce75ede950a0a</para>
    /// </summary>
    [JsonPropertyName("view_private_owner_id")]
    public string? ViewPrivateOwnerId { get; set; }
}