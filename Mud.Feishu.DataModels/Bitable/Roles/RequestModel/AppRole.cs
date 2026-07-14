// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>应用自定义角色</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppRole
{
    /// <summary>
    /// <para>自定义权限的名字</para>
    /// <para>必填：是</para>
    /// <para>示例值：普通用户</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// <para>针对数据表的权限设置</para>
    /// <para>必填：是</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("table_roles")]
    public TableRole[] TableRoles { get; set; } = [];

    /// <summary>
    /// <para>针对仪表盘的权限设置</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("block_roles")]
    public BlockRole[]? BlockRoles { get; set; }

    /// <summary>
    /// <para>多维表格点位的权限。</para>
    /// <para>- 未设置时，表示自定义角色拥有所有点位权限。</para>
    /// <para>- 设置时，可设置以下两种权限：</para>
    /// <para>- `base_complex_edit` : 设置是否可以创建副本、下载、打印多维表格</para>
    /// <para>- `copy`: 设置是否可以复制多维表格内容</para>
    /// <para>该参数类型为 map，其中 key 是权限点位名称，value 是权限开关。value 枚举值有：</para>
    /// <para>- `0`：无权限</para>
    /// <para>- `1`：有权限</para>
    /// <para>**注意**：仅高级权限为 v2 版本的多维表格支持该参数。是否是 v2 版本可调用[获取多维表格元数据](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app/get)查看。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"base_complex_edit": 1, "copy": 0}</para>
    /// </summary>
    [JsonPropertyName("base_rule")]
    public object? BaseRule { get; set; }
}
