// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 租户上下文作用域工厂：工具执行链切换 <c>appKey</c> 的唯一入口（TMA2-20：租户=应用上下文）。
/// </summary>
/// <remarks>
/// <para>
/// 生成的 HTTP 客户端共享单例 <c>IAppContextHolder</c>（默认 <c>AsyncLocalAppContextSwitcher</c>），
/// 任意一个客户端的 <c>BeginScope(appKey)</c> 都会切换异步流内的环境应用上下文——
/// 对所有客户端同时生效。默认实现取 <c>IFeishuTenantV1Message</c> 客户端作为作用域入口
/// （10 工具中 IM 域必注册该客户端）。
/// </para>
/// </remarks>
public interface IFeishuAppContextScopeFactory
{
    /// <summary>
    /// 切换环境应用上下文到 <paramref name="appKey"/>；返回的作用域释放时恢复原上下文。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <returns>作用域（<see cref="IDisposable.Dispose"/> 恢复原上下文）。</returns>
    IDisposable BeginScope(string appKey);
}

/// <summary><see cref="IFeishuAppContextScopeFactory"/> 默认实现（经消息客户端切换环境上下文）。</summary>
public sealed class FeishuAppContextScopeFactory(Mud.Feishu.IFeishuTenantV1Message messageClient) : IFeishuAppContextScopeFactory
{
    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));

    /// <inheritdoc />
    public IDisposable BeginScope(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));

        return _messageClient.BeginScope(appKey);
    }
}
