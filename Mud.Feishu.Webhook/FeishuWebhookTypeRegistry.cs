// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 通用应用-类型注册表
/// 提供按 AppKey 隔离的类型注册和查询功能，用于处理器和拦截器的统一管理
/// </summary>
/// <typeparam name="T">注册的类型标记接口（如 IFeishuEventHandler、IFeishuEventInterceptor）</typeparam>
public class FeishuWebhookTypeRegistry<T>
{
    private readonly ConcurrentDictionary<string, List<Type>> _registry = new();
    private readonly object _freezeLock = new();
    private volatile bool _isFrozen;

    /// <summary>
    /// 冻结注册表，冻结后 Register 将抛出 InvalidOperationException
    /// </summary>
    /// <remarks>
    /// R3-P2-12：冻结与注册必须在**同一把锁**下判定，否则"检查 <c>_isFrozen</c> → 再 Add"
    /// 之间存在窗口：并发注册可在冻结生效后仍写入，使冻结语义失效（热更重放期间注册两次）。
    /// </remarks>
    public void Freeze()
    {
        lock (_freezeLock)
        {
            _isFrozen = true;
        }
    }

    /// <summary>
    /// 注册类型
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="type">注册的类型</param>
    public virtual void Register(string appKey, Type type)
    {
        if (string.IsNullOrEmpty(appKey))
            throw new ArgumentException("应用键不能为空", nameof(appKey));

        // R3-P2-12：先取 list（GetOrAdd 本身线程安全），再在冻结锁内做"判定 + 写入"，
        // 保证冻结生效后再无写入。
        var list = _registry.GetOrAdd(appKey, _ => new List<Type>());

        lock (_freezeLock)
        {
            if (_isFrozen)
                throw new InvalidOperationException("注册表已冻结，不允许运行时热注册");

            lock (list)
            {
                if (!list.Contains(type))
                    list.Add(type);
            }
        }
    }

    /// <summary>
    /// 获取应用的所有注册类型
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>类型列表</returns>
    public virtual IReadOnlyList<Type> GetAll(string appKey)
    {
        if (_registry.TryGetValue(appKey, out var types))
        {
            lock (types)
            {
                return types.ToArray();
            }
        }
        return Array.Empty<Type>();
    }

    /// <summary>
    /// 获取所有已注册的应用键
    /// </summary>
    /// <returns>应用键列表</returns>
    public virtual IReadOnlyList<string> GetAllAppKeys()
    {
        return _registry.Keys.ToArray();
    }

    /// <summary>
    /// 检查应用是否已注册类型
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>是否已注册</returns>
    public virtual bool HasAny(string appKey)
    {
        return _registry.TryGetValue(appKey, out var list) && list.Count > 0;
    }
}
