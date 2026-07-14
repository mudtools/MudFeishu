// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>针对数据表的权限设置</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class TableRole
{
    /// <summary>
    /// <para>数据表权限。</para>
    /// <para>**提示**：**协作者可编辑自己的记录** 和 **可编辑指定字段** 是 **可编辑记录** 的特殊情况，可通过指定 `rec_rule` 或 `field_perm` 参数实现相同的效果。</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最大值：4</para>
    /// <para>最小值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：无权限</item>
    /// <item>1：仅可阅读</item>
    /// <item>2：可编辑</item>
    /// <item>4：可管理</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("table_perm")]
    public int TablePerm { get; set; }

    /// <summary>
    /// <para>数据表名称（与下方 table_id 至少填写一项）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：数据表1</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("table_name")]
    public string? TableName { get; set; }

    /// <summary>
    /// <para>多维表格数据表的唯一标识。（与上方 table_name 至少填写一项）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：tblKz5D60T4JlfcT</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("table_id")]
    public string? TableId { get; set; }

    /// <summary>
    /// <para>记录筛选条件，当 `table_perm` 为 1 或 2 时生效。用于指定可编辑或可阅读的记录。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rec_rule")]
    public TableRoleRecRule? RecRule { get; set; }

    /// <summary>
    /// <para>记录筛选条件，在 `rec_rule.other_perm` 为 0 时生效。对于未命中 `rec_rule` 的记录，通过 `other_rec_rule` 指定可阅读记录范围；此时，既未命中 `rec_rule`、也未命中 `other_rec_rule` 的记录会被禁止阅读。</para>
    /// <para>**注意**：仅高级权限为 v2 版本的多维表格支持该参数。是否是 v2 版本可调用[获取多维表格元数据](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app/get)查看。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("other_rec_rule")]
    public TableRoleOtherRecRule? OtherRecRule { get; set; }

    /// <summary>
    /// <para>字段权限，仅在 `table_perm` 为 1和 2 时生效。用于设置字段可编辑或可阅读。类型为 map，key 是字段名称，value 是字段权限。对于未设置的字段，默认无权限。value 枚举值有：</para>
    /// <para>- `1`：可阅读</para>
    /// <para>- `2`：可添加</para>
    /// <para>- `3`：可编辑</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"姓名": 1, "年龄": 2}</para>
    /// </summary>
    [JsonPropertyName("field_perm")]
    public object? FieldPerm { get; set; }

    /// <summary>
    /// <para>新增记录权限，仅在 `table_perm` 为 2 时生效，用于设置记录是否可以新增。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：true</para>
    /// </summary>
    [JsonPropertyName("allow_add_record")]
    public bool? AllowAddRecord { get; set; }

    /// <summary>
    /// <para>删除记录权限，仅在 `table_perm` 为 2 时生效，用于设置记录是否可以删除。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：true</para>
    /// </summary>
    [JsonPropertyName("allow_delete_record")]
    public bool? AllowDeleteRecord { get; set; }

    /// <summary>
    /// <para>设置视图的权限。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>最大值：2</para>
    /// <para>最小值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：可阅读</item>
    /// <item>2：可编辑</item>
    /// </list></para>
    /// <para>默认值：2</para>
    /// </summary>
    [JsonPropertyName("view_perm")]
    public int? ViewPerm { get; set; }

    /// <summary>
    /// <para>可读的视图集合，仅在 view_perm 为 1 （视图为可阅读）时生效。</para>
    /// <para>- 未设置时，表示所有视图可读。</para>
    /// <para>- 设置后，表示设置的视图可读，未设置的视图无权限。</para>
    /// <para>该参数类型为 map，其中 key 是[视图 ID](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview#5b05b8ca)，value 是视图对应的权限。value 枚举值有：</para>
    /// <para>- `0`：无权限</para>
    /// <para>- `1`：可阅读</para>
    /// <para>**注意**：仅高级权限为 v2 版本的多维表格支持该参数。是否是 v2 版本可调用[获取多维表格元数据](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app/get)查看。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"vewEYknYcC": 0}</para>
    /// </summary>
    [JsonPropertyName("view_rules")]
    public object? ViewRules { get; set; }

    /// <summary>
    /// <para>设置字段的权限，仅可配置单多选字段、附件字段。可选的点位有：</para>
    /// <para>- `select_option_edit` : 选项配置点位，配置是否可增删改单、多选选项，未设置表示无权限。</para>
    /// <para>- `attachment_export`: 附件操作权限点位，配置是否可导出附件，未设置表示可导出。</para>
    /// <para>该参数类型为两层 map 结构，其中 key 是字段点位权限，value 是字段权限集合。字段权限集合也是一个 map 结构，其中 key 是字段名称，value 是字段点位权限：</para>
    /// <para>- `0`：无权限</para>
    /// <para>- `1`：有权限</para>
    /// <para>**注意**：仅高级权限为 v2 版本的多维表格支持该参数。是否是 v2 版本可调用[获取多维表格元数据](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app/get)查看。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"select_option_edit": {"单选1":0}}</para>
    /// </summary>
    [JsonPropertyName("field_action_rules")]
    public object? FieldActionRules { get; set; }
}
