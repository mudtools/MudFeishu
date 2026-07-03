// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.DataModels.WsEndpoint;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// ProtoBuf 帧构建器 - 负责构建飞书 WebSocket 协议帧和解析控制帧
/// </summary>
public static class FrameBuilder
{
    /// <summary>
    /// 帧方法常量：控制帧
    /// </summary>
    public const int MethodControl = 0;

    /// <summary>
    /// 帧方法常量：数据帧
    /// </summary>
    public const int MethodData = 1;

    /// <summary>
    /// 构建 ProtoBuf 二进制 Ping 帧
    /// </summary>
    /// <param name="serviceId">服务ID，从 WebSocket URL 的 service_id 查询参数提取</param>
    /// <returns>序列化后的 ProtoBuf 二进制数据</returns>
    public static byte[] BuildPingFrame(int serviceId)
    {
        var frame = new EventProtoData
        {
            Service = serviceId,
            Method = MethodControl,
            SeqID = 0,
            LogID = 0,
            Headers = new[]
            {
                new ProtoHeader { Key = "type", Value = "ping" }
            }
        };

        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, frame);
        return stream.ToArray();
    }

    /// <summary>
    /// 判断是否为控制帧（Method == 0）
    /// </summary>
    /// <param name="frame">ProtoBuf 帧对象</param>
    /// <returns>如果是控制帧返回 true，否则返回 false</returns>
    public static bool IsControlFrame(EventProtoData frame)
    {
        return frame?.Method == MethodControl;
    }

    /// <summary>
    /// 判断是否为数据帧（Method == 1）
    /// </summary>
    /// <param name="frame">ProtoBuf 帧对象</param>
    /// <returns>如果是数据帧返回 true，否则返回 false</returns>
    public static bool IsDataFrame(EventProtoData frame)
    {
        return frame?.Method == MethodData;
    }

    /// <summary>
    /// 从 Pong 控制帧中解析 ClientConfig
    /// <para>Pong 帧的 Payload 是 JSON 格式的 ClientConfig，包含服务端下发的动态配置</para>
    /// </summary>
    /// <param name="frame">ProtoBuf 帧对象</param>
    /// <param name="logger">可选的日志记录器</param>
    /// <returns>解析成功返回 ClientConfigInfo，否则返回 null</returns>
    public static ClientConfigInfo? ExtractClientConfig(EventProtoData frame, ILogger? logger = null)
    {
        if (frame?.Payload == null || frame.Payload.Length == 0)
        {
            logger?.LogDebug("Pong 帧 Payload 为空，跳过 ClientConfig 解析");
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(frame.Payload);
            var config = JsonSerializer.Deserialize<ClientConfigInfo>(json, JsonOptions.Default);
            if (config != null)
            {
                logger?.LogDebug("成功解析 ClientConfig: PingInterval={PingInterval}s, ReconnectCount={ReconnectCount}, ReconnectInterval={ReconnectInterval}s, ReconnectNonce={ReconnectNonce}s",
                    config.PingInterval, config.ReconnectCount, config.ReconnectInterval, config.ReconnectNonce);
            }
            return config;
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "解析 Pong 帧 ClientConfig 失败，Payload 长度: {Length}", frame.Payload?.Length ?? 0);
            return null;
        }
    }

    /// <summary>
    /// 从 WebSocket URL 中提取 service_id 查询参数
    /// </summary>
    /// <param name="url">WebSocket 连接 URL</param>
    /// <returns>service_id 整数值，提取失败返回 null</returns>
    public static int? ExtractServiceId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            var uri = new Uri(url);
            // 手动解析查询参数，避免依赖 System.Web.HttpUtility（netstandard2.0 不保证可用）
            var query = uri.Query.TrimStart('?');
            if (string.IsNullOrEmpty(query))
                return null;

            foreach (var pair in query.Split('&'))
            {
                var kv = pair.Split(new[] { '=' }, 2);
                if (kv.Length == 2 && kv[0] == "service_id")
                {
                    if (int.TryParse(kv[1], out var serviceId))
                        return serviceId;
                    return null;
                }
            }
        }
        catch
        {
            // URL 解析失败，返回 null
        }

        return null;
    }
}
