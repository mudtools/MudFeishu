// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 基于 AsyncLocal 的默认用户上下文实现
/// </summary>
/// <remarks>
/// 作为 IFeishuCurrentUserContext 的默认注册实现，确保在未调用 AddFeishuUserContext() 时
/// 仍能提供可用的用户上下文。如果需要日志记录等增强功能，请使用
/// Mud.Feishu.Authentication 中的 CurrentUserContext 并调用 AddFeishuUserContext()。
/// <para>UserId 回退机制：</para>
/// 在飞书生态中，用户访问令牌（UserAccessToken）以 OpenId 为缓存键存储。
/// 源生成器使用 ICurrentUserContext.UserId 作为令牌查找键，
/// 因此在 SetUser 时若 userId 未显式提供，则自动回退到 openId，
/// 确保令牌查找键与存储键一致。
/// </remarks>
internal sealed class DefaultFeishuCurrentUserContext : IFeishuCurrentUserContext
{
    private static readonly AsyncLocal<UserInfo?> _currentUser = new();

    public string? OpenId => _currentUser.Value?.OpenId;
    public string? UnionId => _currentUser.Value?.UnionId;
    public string? UserId => _currentUser.Value?.UserId;
    public string? Name => _currentUser.Value?.Name;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUser.Value?.OpenId);

    /// <inheritdoc />
    public void SetUser(string openId, string? unionId = null, string? userId = null, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(openId))
            throw new ArgumentException("OpenId cannot be null, empty or whitespace.", nameof(openId));

        // 当 userId 未显式提供（null 或空白）时，回退到 openId，
        // 因为 UserTokenManager 使用 OpenId 作为令牌缓存键，
        // 源生成器使用 UserId 属性作为令牌查找键，两者必须一致。
        userId = string.IsNullOrWhiteSpace(userId) ? openId : userId;

        _currentUser.Value = new UserInfo
        {
            OpenId = openId,
            UnionId = unionId,
            UserId = userId,
            Name = name
        };
    }

    public void SetUserId(string? userId)
    {
        var current = _currentUser.Value;
        if (userId == null)
        {
            _currentUser.Value = null;
            return;
        }

        _currentUser.Value = new UserInfo
        {
            OpenId = current?.OpenId,
            UnionId = current?.UnionId,
            UserId = userId,
            Name = current?.Name
        };
    }

    public void Clear()
    {
        _currentUser.Value = null;
    }

    private sealed class UserInfo
    {
        public string? OpenId { get; set; }
        public string? UnionId { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
    }
}
