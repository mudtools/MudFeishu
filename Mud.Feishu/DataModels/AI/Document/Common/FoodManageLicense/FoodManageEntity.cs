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
public class FoodManageEntity
{
    /// <summary>
    /// <para>识别的字段种类</para>
    /// <para>必填：否</para>
    /// <para>示例值：issuer</para>
    /// <para>可选值：<list type="bullet">
    /// <item>validity_period：有效期</item>
    /// <item>issuer：签发人</item>
    /// <item>issuing_authority：发证机关</item>
    /// <item>complaints_hotline：投诉举报电话</item>
    /// <item>license_number：许可证编号</item>
    /// <item>domicile：住所</item>
    /// <item>legal_representative：法定代表人(负责人)</item>
    /// <item>credit_code：社会信用代码(身份证号)</item>
    /// <item>operator：经营者名称</item>
    /// <item>premise：经营场所</item>
    /// <item>daily_supervisor：日常监督管理人员</item>
    /// <item>daily_supervisory_authorities：日常监督管理机构</item>
    /// <item>main_body：主体业态</item>
    /// <item>operating_item：经营项目</item>
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
