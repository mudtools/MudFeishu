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
public class BusinessEntity
{
    /// <summary>
    /// <para>识别的字段种类</para>
    /// <para>必填：否</para>
    /// <para>示例值：legal_representative</para>
    /// <para>可选值：<list type="bullet">
    /// <item>certificate_type：证书类型</item>
    /// <item>unified_social_credit_code：统一社会信用代码</item>
    /// <item>company_name：公司名称</item>
    /// <item>company_type：公司类型</item>
    /// <item>domicile：住所</item>
    /// <item>legal_representative：法定代表人</item>
    /// <item>registered_capital：注册资本</item>
    /// <item>established_time：成立日期</item>
    /// <item>established_date：营业期限</item>
    /// <item>business_scope：经营范围</item>
    /// <item>website：企业信用信息公示系统网址</item>
    /// <item>approval_date：核准日期</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>识别出字段的文本信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}