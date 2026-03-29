// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>筛选条件</para>
/// </summary>
public class AppTableViewPropertyFilterInfoConditionData : AppTableViewPropertyFilterInfoCondition
{
    /// <summary>
    /// <para>过滤条件的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：conNaOEK6O</para>
    /// </summary>
    [JsonPropertyName("condition_id")]
    public string? ConditionId { get; set; }

    /// <summary>
    /// <para>用于过滤的字段类型</para>
    /// <para>- 1：多行文本</para>
    /// <para>- 2：数字</para>
    /// <para>- 3：单选</para>
    /// <para>- 4：多选</para>
    /// <para>- 5：日期</para>
    /// <para>- 7：复选框</para>
    /// <para>- 11：人员</para>
    /// <para>- 13：电话号码</para>
    /// <para>- 15：超链接</para>
    /// <para>- 17：附件</para>
    /// <para>- 18：单向关联</para>
    /// <para>- 19：查找引用</para>
    /// <para>- 20：公式</para>
    /// <para>- 21：双向关联</para>
    /// <para>- 22：地理位置</para>
    /// <para>- 23：群组</para>
    /// <para>- 1001：创建时间</para>
    /// <para>- 1002：最后更新时间</para>
    /// <para>- 1003：创建人</para>
    /// <para>- 1004：修改人</para>
    /// <para>- 1005：自动编号</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("field_type")]
    public int? FieldType { get; set; }
}