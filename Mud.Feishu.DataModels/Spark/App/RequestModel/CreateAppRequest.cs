// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 创建妙搭应用请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class CreateAppRequest
{
    /// <summary>
    /// <para>应用名称，支持中英文，长度不超过 64 字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：智能客服助手</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>应用类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：HTML</para>
    /// <para>可选值：HTML</para>
    /// </summary>
    [JsonPropertyName("app_type")]
    public string? AppType { get; set; }

    /// <summary>
    /// <para>应用描述，长度不超过 200 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：提供7×24小时智能对话服务，支持常见问题自动解答与工单流转</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用图标地址，支持 PNG/JPG 格式，建议尺寸 128×128 像素</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.com/app-icons/customer-service.png</para>
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }
}
