// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationGadget
/// <para>小程序能力</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationGadget
{
    /// <summary>
    /// <para>pc 支持的小程序模式，bit 位表示</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：sidebar 模式</para>
    /// <para>- 2：pc 模式</para>
    /// <para>- 4：主导航模式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_pc_mode")]
    public int? EnablePcMode { get; set; }

    /// <summary>
    /// <para>schema url 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schema_urls")]
    public string[]? SchemaUrls { get; set; }

    /// <summary>
    /// <para>pc 端是否使用小程序版本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_use_mobile_pkg")]
    public bool? PcUseMobilePkg { get; set; }

    /// <summary>
    /// <para>pc 的小程序版本号</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_version")]
    public string? PcVersion { get; set; }

    /// <summary>
    /// <para>移动端小程序版本号</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_version")]
    public string? MobileVersion { get; set; }

    /// <summary>
    /// <para>移动端兼容的最低飞书版本</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_min_lark_version")]
    public string? MobileMinLarkVersion { get; set; }

    /// <summary>
    /// <para>pc 端兼容的最低飞书版本</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_min_lark_version")]
    public string? PcMinLarkVersion { get; set; }
}
