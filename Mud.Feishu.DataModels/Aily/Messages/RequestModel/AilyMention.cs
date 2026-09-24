// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>被@的实体</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AilyMention
{
    /// <summary>
    /// <para>实体 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_5ad573a6411d72b8305fda3a9c15c70e</para>
    /// <para>最大长度：64</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    /// <summary>
    /// <para>身份提供者</para>
    /// <para>必填：否</para>
    /// <para>示例值：FEISHU</para>
    /// <para>可选值：<list type="bullet">
    /// <item>AILY：Aily 账号体系</item>
    /// <item>FEISHU：飞书账号体系</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("identity_provider")]
    public string? IdentityProvider { get; set; }

    /// <summary>
    /// <para>被@实体在消息体中的占位符</para>
    /// <para>必填：否</para>
    /// <para>示例值：@_user_1</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// <para>被@实体的名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>飞书智能伙伴创建平台账号体系下的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：1794840334557292</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("aily_id")]
    public string? AilyId { get; set; }
}
