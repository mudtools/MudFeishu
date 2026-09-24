// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>创建的会话信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AilySession
{
    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：session_4dfunz7sp1g8m</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：9</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>会话的创建时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1711975665710</para>
    /// <para>最大长度：13</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// <para>会话的上次更新时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1711975665710</para>
    /// <para>最大长度：13</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;

    /// <summary>
    /// <para>会话的创建人（Aily UserID）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1794840334557292</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// <para>可自行构造的 Context [上下文变量](https://aily.feishu.cn/hc/1u7kleqg/en70bqqj#6a446d5e)；在 Workflow 技能中可消费这部分全局变量</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("channel_context")]
    public string? ChannelContext { get; set; }

    /// <summary>
    /// <para>会话的自定义变量内容，变量数据保存在服务端 Session 中，可在 `GetSession` 时原样返回，无需在 API 调用侧存储</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}