// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// <para>识别出的实体类型</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class VehicleInvoiceEntity
{
    /// <summary>
    /// <para>识别的字段种类</para>
    /// <para>必填：否</para>
    /// <para>示例值：SalerName</para>
    /// <para>可选值：<list type="bullet">
    /// <item>invoice_code：发票代码</item>
    /// <item>invoice_num：发票号码</item>
    /// <item>date：开票日期</item>
    /// <item>print_code：机打代码</item>
    /// <item>print_num：机打号码</item>
    /// <item>machine_num：机器编码</item>
    /// <item>buyer_name：购买方名称</item>
    /// <item>buyer_id：购买方纳税人识别号</item>
    /// <item>vehicle_type：车辆类型</item>
    /// <item>product_model：厂牌型号</item>
    /// <item>certificate_num：合格证号</item>
    /// <item>engine_num：发动机号码</item>
    /// <item>vin：车架号</item>
    /// <item>total_price：价税合计</item>
    /// <item>total_price_little：小写金额</item>
    /// <item>saler_name：销货单位名称</item>
    /// <item>saler_id：销售方纳税人识别号</item>
    /// <item>saler_addr：地址</item>
    /// <item>tax_rate：税率</item>
    /// <item>tax：税额</item>
    /// <item>price：不含税价格</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>识别出字段的文本信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx公司</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
