// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalForm;

/// <summary>
/// 组件类型枚举
/// </summary>
public enum WidgetType
{
    /// <summary>
    /// 单行文本输入框
    /// </summary>
    Input,
    /// <summary>
    /// 多行文本输入框
    /// </summary>
    Textarea,
    /// <summary>
    /// 日期选择器
    /// </summary>
    Date,
    /// <summary>
    /// 日期区间选择器
    /// </summary>
    DateInterval,
    /// <summary>
    /// 单选框（新版）
    /// </summary>
    RadioV2,
    /// <summary>
    /// 复选框（新版）
    /// </summary>
    CheckboxV2,
    /// <summary>
    /// 数字输入框
    /// </summary>
    Number,
    /// <summary>
    /// 金额输入框
    /// </summary>
    Amount,
    /// <summary>
    /// 公式计算组件
    /// </summary>
    Formula,
    /// <summary>
    /// 关联组件
    /// </summary>
    Connect,
    /// <summary>
    /// 文档组件
    /// </summary>
    Document,
    /// <summary>
    /// 附件组件（新版）
    /// </summary>
    AttachmentV2,
    /// <summary>
    /// 图片组件
    /// </summary>
    Image,
    /// <summary>
    /// 字段列表组件
    /// </summary>
    FieldList,
    /// <summary>
    /// 部门选择组件
    /// </summary>
    Department,
    /// <summary>
    /// 电话号码输入框
    /// </summary>
    Telephone,
    /// <summary>
    /// 地址输入框
    /// </summary>
    Address,
    /// <summary>
    /// 班次组选择组件
    /// </summary>
    ShiftGroup
}