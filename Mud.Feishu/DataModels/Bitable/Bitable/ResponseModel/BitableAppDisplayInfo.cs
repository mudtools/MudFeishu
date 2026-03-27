// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 多维表格应用显示信息
/// </summary>
public class BitableAppDisplayInfo
{
    /// <summary>
    /// <para>多维表格的唯一标识 app_token</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("app_token")]
    public string? AppToken { get; set; }

    /// <summary>
    /// <para>多维表格的名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>多维表格的版本号。对多维表格进行修改时更新，如新增、删除数据表，修改数据表名等，初始为 1，每次更新+1</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("revision")]
    public int? Revision { get; set; }

    /// <summary>
    /// <para>多维表格是否开启了高级权限。取值包括：</para>
    /// <para>- true：开启了高级权限</para>
    /// <para>- false：关闭了高级权限</para>
    /// <para>必填：否</para>
    /// <para>示例值：\-</para>
    /// </summary>
    [JsonPropertyName("is_advanced")]
    public bool? IsAdvanced { get; set; }

    /// <summary>
    /// <para>多维表格的时区</para>
    /// <para>必填：否</para>
    /// <para>示例值：Asia/Beijing</para>
    /// </summary>
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// <para>多维表格的公式字段类型。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：不支持指定公式字段类型</item>
    /// <item>2：支持指定公式字段类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("formula_type")]
    public int? FormulaType { get; set; }

    /// <summary>
    /// <para>文档高级权限版本。</para>
    /// <para>必填：否</para>
    /// <para>示例值：v1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>v1：v1版本</item>
    /// <item>v2：v2版本</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("advance_version")]
    public string? AdvanceVersion { get; set; }
}
