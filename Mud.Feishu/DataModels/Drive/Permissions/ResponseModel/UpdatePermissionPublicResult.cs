// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 更新云文档权限设置响应体
/// </summary>
public class UpdatePermissionPublicResult
{
    /// <summary>
    /// <para>本次更新后的文档权限设置。如权限设置未更新，则不返回对应参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("permission_public")]
    public DrivePermissionsDetail? PermissionPublic { get; set; }

}

/// <summary>
/// 云文档权限设置详情
/// </summary>
public class DrivePermissionsDetail : DrivePermissions
{
    /// <summary>
    /// <para>知识库中的子页面是否已限制权限，不再继承父级页面的权限设置。</para>
    /// <para>**枚举值有：**</para>
    /// <para>- `true`: 已限制权限</para>
    /// <para>- `false`: 未限制权限</para>
    /// <para>**提示**：当知识库中的子页面权限范围小于父级页面时，该页面权限将默认限制权限。</para>
    /// <para>![image.png](//sf3-cn.feishucdn.com/obj/open-platform-opendoc/a99780710c3f7e5e390280ff6d87fc47_HIjzKDxscr.png?maxWidth=200)</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("lock_switch")]
    public bool? LockSwitch { get; set; }
}
