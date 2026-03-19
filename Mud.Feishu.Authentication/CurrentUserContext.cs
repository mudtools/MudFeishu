// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Authentication;

/// <summary>
/// 基于 AsyncLocal 的用户上下文实现
/// </summary>
/// <remarks>
/// <para>实现原理：</para>
/// <list type="bullet">
///   <item><description>使用 static AsyncLocal&lt;T&gt; 确保在异步调用链中正确传递数据</description></item>
///   <item><description>每个请求有独立的 UserInfo 实例，互不干扰</description></item>
///   <item><description>注册为 Singleton 生命周期，全局共享同一个实例</description></item>
/// </list>
/// <para>线程安全说明：</para>
/// AsyncLocal 会自动将数据绑定到当前的异步控制流上下文，
/// 确保在不同的异步方法之间正确传递，而不会受到并发请求的影响。
/// </remarks>
public class CurrentUserContext : ICurrentUserContext
{
    /// <summary>
    /// 使用静态字段确保在整个应用程序生命周期内，所有请求共享同一个 AsyncLocal 实例
    /// </summary>
    private static readonly AsyncLocal<UserInfo?> _currentUser = new();

    /// <inheritdoc />
    public string? OpenId => _currentUser.Value?.OpenId;

    /// <inheritdoc />
    public string? UnionId => _currentUser.Value?.UnionId;

    /// <inheritdoc />
    public string? UserId => _currentUser.Value?.UserId;

    /// <inheritdoc />
    public string? Name => _currentUser.Value?.Name;

    /// <inheritdoc />
    public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUser.Value?.OpenId);

    /// <inheritdoc />
    public void SetUser(string openId, string? unionId = null, string? userId = null, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new ArgumentException("OpenId cannot be null, empty or whitespace.", nameof(openId));
        }

        _currentUser.Value = new UserInfo
        {
            OpenId = openId,
            UnionId = unionId,
            UserId = userId,
            Name = name
        };
    }

    /// <inheritdoc />
    public void Clear()
    {
        _currentUser.Value = null;
    }

    /// <summary>
    /// 内部用户信息类
    /// </summary>
    private sealed class UserInfo
    {
        public string? OpenId { get; set; }
        public string? UnionId { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
    }
}
