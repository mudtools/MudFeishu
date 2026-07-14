// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>设置公式字段的数据类型</para>
/// <para>**注意**：非所有多维表格都支持该能力。请参考[获取多维表格元数据](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app/get)接口返回的formula_type 判断，当 `formula_type` 等于 2 时，表示需要设置该字段。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppTableFieldPropertyType
{
    /// <summary>
    /// <para>公式字段对应的数据类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：文本（默认值）、条码</item>
    /// <item>2：数字（默认值）、进度、货币、评分</item>
    /// <item>3：单选</item>
    /// <item>4：多选</item>
    /// <item>5：日期</item>
    /// <item>7：复选框</item>
    /// <item>11：人员</item>
    /// <item>13：电话号码</item>
    /// <item>15：超链接</item>
    /// <item>17：附件</item>
    /// <item>18：单向关联</item>
    /// <item>20：公式</item>
    /// <item>21：双向关联</item>
    /// <item>22：地理位置</item>
    /// <item>23：群组</item>
    /// <item>1001：创建时间</item>
    /// <item>1002：最后更新时间</item>
    /// <item>1003：创建人</item>
    /// <item>1004：修改人</item>
    /// <item>1005：自动编号</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("data_type")]
    public int DataType { get; set; }

    /// <summary>
    /// <para>公式数据属性信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ui_property")]
    public AppTableFieldPropertyTypeUiProperty? UiProperty { get; set; }

    /// <summary>
    /// <para>公式字段在界面上的展示类型，例如进度字段是数字的一种展示形态。了解更多，参考[字段编辑指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-field/guide)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：Progress</para>
    /// <para>可选值：<list type="bullet">
    /// <item>Number：数字</item>
    /// <item>Progress：进度</item>
    /// <item>Currency：货币</item>
    /// <item>Rating：评分</item>
    /// <item>DateTime：日期</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("ui_type")]
    public string? UiType { get; set; }
}
