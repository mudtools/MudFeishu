// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Encodings.Web;

namespace Mud.Feishu.AI.Mcp;

/// <summary>线协议 JSON 的唯一序列化配置（stdio 与 HTTP 两条传输共用）。</summary>
/// <remarks>
/// 两条通道各配一份编码器必然漂移：漂移表现为「同一个中文字符串在 stdio 里是原文、
/// 在 HTTP 里是 <c>\uXXXX</c>」——对模型/客户端既是约 6 倍 token 膨胀也是可读性灾难。
/// </remarks>
internal static class McpWireJson
{
    /// <summary>序列化选项（唯一实例）。</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    /// <summary>序列化为线协议文本。</summary>
    /// <remarks>
    /// 只接 <see cref="JsonObject"/>/<see cref="JsonNode"/>：匿名类型走
    /// <c>JsonSerializer.Serialize(object)</c> 是反射重载，会破 AOT 严格门禁。
    /// </remarks>
    /// <param name="node">待序列化对象。</param>
    /// <returns>JSON 文本。</returns>
    public static string ToWire(this JsonObject node) => node.ToJsonString(Options);
}
