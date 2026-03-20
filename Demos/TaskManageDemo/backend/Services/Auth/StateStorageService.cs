// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// State 存储服务接口
/// </summary>
public interface IStateStorageService
{
    /// <summary>
    /// 生成并存储 State
    /// </summary>
    string GenerateState();

    /// <summary>
    /// 验证 State 是否有效
    /// </summary>
    bool ValidateState(string state);

    /// <summary>
    /// 移除 State
    /// </summary>
    void RemoveState(string state);

    /// <summary>
    /// 清理过期的 State
    /// </summary>
    void CleanExpiredStates();
}

/// <summary>
/// State 存储服务实现
/// </summary>
public class StateStorageService : IStateStorageService
{
    private readonly ConcurrentDictionary<string, DateTime> _states = new();
    private readonly TimeSpan _expirationTime;

    public StateStorageService(TimeSpan expirationTime)
    {
        _expirationTime = expirationTime;
    }

    public string GenerateState()
    {
        var state = Guid.NewGuid().ToString("N");
        _states[state] = DateTime.UtcNow;
        return state;
    }

    public bool ValidateState(string state)
    {
        if (!_states.TryGetValue(state, out var createdAt))
        {
            return false;
        }

        // 检查是否过期
        if (DateTime.UtcNow - createdAt > _expirationTime)
        {
            _states.TryRemove(state, out _);
            return false;
        }

        return true;
    }

    public void RemoveState(string state)
    {
        _states.TryRemove(state, out _);
    }

    public void CleanExpiredStates()
    {
        var now = DateTime.UtcNow;
        var expiredStates = _states
            .Where(x => now - x.Value > _expirationTime)
            .Select(x => x.Key)
            .ToList();

        foreach (var state in expiredStates)
        {
            _states.TryRemove(state, out _);
        }
    }
}
