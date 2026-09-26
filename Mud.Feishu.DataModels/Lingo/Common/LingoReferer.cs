// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 词条的关联信息（referer），用于相关联系人、公开群、云文档、值班号、链接等
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoReferer
{
    /// <summary>
    /// <para>对应相关信息 ID（联系人 open_id、群 chat_id、值班号 ID 等）</para>
    /// <para>必填：视场景而定（请求体中 users/chats/oncalls 为必填）</para>
    /// <para>示例值：ou_30b07b63089e***18789914dac63d36</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>对应相关信息的描述，如相关联系人的备注、相关链接的标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：飞书官网</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>链接地址</para>
    /// <para>必填：否（docs、links 必填）</para>
    /// <para>示例值：https://www.feishu.cn/hc/zh-CN</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
