// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书HTTP客户端接口，用于发送飞书相关的HTTP请求
/// </summary>
public interface IEnhancedHttpClient
{
    /// <summary>
    /// 发送飞书请求并返回指定类型的结果
    /// </summary>
    /// <typeparam name="TResult">期望的返回结果类型</typeparam>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>返回类型为TResult的异步任务，可能为null</returns>
    Task<TResult?> SendAsync<TResult>(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件内容并以字节数组形式返回
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>返回字节数组的异步任务，可能为null</returns>
    Task<byte[]?> DownloadAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步下载大文件并保存到指定路径
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="filePath">用于保存文件的本地路径</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task<FileInfo> DownloadLargeAsync(HttpRequestMessage request, string filePath, bool overwrite = true, CancellationToken cancellationToken = default);
}
