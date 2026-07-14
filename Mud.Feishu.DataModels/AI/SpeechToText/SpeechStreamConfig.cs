// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>配置属性</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class SpeechStreamConfig
{
    /// <summary>
    /// <para>仅包含字母数字和下划线的 16 位字符串作为同一数据流的标识，用户生成</para>
    /// <para>必填：是</para>
    /// <para>示例值：asd1234567890ddd</para>
    /// </summary>
    [JsonPropertyName("stream_id")]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据流分片的序号，序号从 0 开始，每次请求递增 1</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("sequence_id")]
    public int SequenceId { get; set; }

    /// <summary>
    /// <para>数据流标记：1 首包，2 正常结束，等待结果返回，3 中断数据流不返回最终结果，0 传输语音中间的数据包</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("action")]
    public int Action { get; set; }

    /// <summary>
    /// <para>语音格式，目前仅支持：pcm</para>
    /// <para>必填：是</para>
    /// <para>示例值：pcm</para>
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// <para>引擎类型，目前仅支持：16k_auto 中英混合</para>
    /// <para>必填：是</para>
    /// <para>示例值：16k_auto</para>
    /// </summary>
    [JsonPropertyName("engine_type")]
    public string EngineType { get; set; } = string.Empty;
}
