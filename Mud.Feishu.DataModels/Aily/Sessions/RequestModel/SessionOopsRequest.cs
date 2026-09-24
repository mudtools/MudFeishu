// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 会话业务操作请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class SessionOopsRequest
{
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
