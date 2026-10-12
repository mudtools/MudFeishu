// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 更新应用基础信息请求体。未传入的参数保持不变，仅更新传入的参数。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UpdateApplicationBaseRequest
{
    /// <summary>
    /// <para>应用名称与描述多语言配置，长度范围：1 ～ 100</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18ns")]
    public List<AppI18nInfo>? I18ns { get; set; }

    /// <summary>
    /// <para>应用图标图片链接，最大长度：512 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://s3-imfile.feishucdn.com/static-resource/v1/v2_953a8fc1-50bd-4b2e-87e2-b09e47dba23g</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>应用管理后台链接，最大长度：512 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("homepage_url")]
    public string? HomepageUrl { get; set; }
}
