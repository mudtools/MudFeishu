// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>识别出的实体列表</para>
/// </summary>
public class VatEntity
{
    /// <summary>
    /// <para>识别的实体类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：buyer_name</para>
    /// <para>可选值：<list type="bullet">
    /// <item>invoice_name：发票抬头</item>
    /// <item>invoice_code：发票代码</item>
    /// <item>invoice_no：发票号码</item>
    /// <item>invoice_date：开票日期</item>
    /// <item>total_price：合计金额（不含税）</item>
    /// <item>total_tax：合计税额</item>
    /// <item>big_total_price_and_tax：合计总额（大写）</item>
    /// <item>check_code：校验码</item>
    /// <item>total_price_and_tax：合计总额</item>
    /// <item>buyer_name：购买方名称</item>
    /// <item>buyer_taxpayer_no：购买方纳税人识别号</item>
    /// <item>buyer_address_phone：购买方地址&amp;电话所有人</item>
    /// <item>buyer_account：购买方开户行&amp;账号</item>
    /// <item>seller_name：销售方名称</item>
    /// <item>seller_taxpayer_no：销售方纳税人识别号</item>
    /// <item>seller_address_phone：销售方地址&amp;电话</item>
    /// <item>seller_account：销售方开户行&amp;账号</item>
    /// <item>payee：收款人</item>
    /// <item>invoice_date：开票日期</item>
    /// <item>password_area：密码区</item>
    /// <item>remarks：备注</item>
    /// <item>reviewer：复核人</item>
    /// <item>drawer：开票人</item>
    /// <item>is_sealed：是否盖章</item>
    /// <item>seller_name_in_seal：印章内销售方名称</item>
    /// <item>seller_taxpayer_no_in_seal：印章内销售方纳税人识别号</item>
    /// <item>invoice_special_seal：印章名称</item>
    /// <item>machine_num：机器编号</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>识别出字段的文本信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：发呆公司</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>识别出的票据详细信息</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("items")]
    public KvEntity[][]? Items { get; set; }


}