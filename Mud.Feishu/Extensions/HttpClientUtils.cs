// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Mud.Feishu.Extensions;
/// <summary>
/// 为HttpClient提供扩展方法的工具类
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// 发送HTTP请求并反序列化响应结果
    /// </summary>
    /// <typeparam name="TResult">响应结果的类型，必须是类类型并具有无参构造函数</typeparam>
    /// <param name="client">HTTP客户端实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="jsonSerializerOptions">JSON序列化选项，可选</param>
    /// <param name="logger">日志记录器，可选</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>反序列化后的响应结果，如果响应内容为空则返回默认值</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    public static async Task<TResult?> SendRequest<TResult>(
        this HttpClient client,
        HttpRequestMessage httpRequestMessage,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
        where TResult : class, new()
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (httpRequestMessage == null) throw new ArgumentNullException(nameof(httpRequestMessage));

        try
        {
            using var response = await client.SendAsync(httpRequestMessage, cancellationToken);

            // 检查响应状态码是否表示成功
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger?.LogError("HTTP请求失败: {StatusCode}, 响应: {Response}",
                    (int)response.StatusCode, errorContent);

                throw new HttpRequestException(
                    $"HTTP请求失败: {(int)response.StatusCode} - {errorContent}");
            }

            // 从响应内容读取为流
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            // 如果响应流为空，则返回默认值
            if (stream.Length == 0)
            {
                return default;
            }

            // 反序列化流为指定类型的结果
            return await JsonSerializer.DeserializeAsync<TResult>(
                stream,
                jsonSerializerOptions ?? new JsonSerializerOptions(),
                cancellationToken);
        }
        catch (Exception ex) when (logger != null)
        {
            logger.LogError(ex, "HTTP请求异常: {Url}", httpRequestMessage.RequestUri);
            throw;
        }
    }
}