// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书服务配置内部状态，跟踪已注册的模块
/// </summary>
internal class FeishuServiceConfiguration
{
    private readonly HashSet<FeishuModule> _registeredModules = new();

    /// <summary>
    /// 尝试添加模块到已注册集合
    /// </summary>
    /// <param name="module">要添加的模块</param>
    /// <returns>如果模块之前未注册则返回 true，否则返回 false</returns>
    public bool TryAdd(FeishuModule module) => _registeredModules.Add(module);

    /// <summary>
    /// 检查模块是否已注册
    /// </summary>
    /// <param name="module">要检查的模块</param>
    /// <returns>如果模块已注册则返回 true</returns>
    public bool IsRegistered(FeishuModule module) => _registeredModules.Contains(module);

    /// <summary>
    /// 检查是否添加了任何服务
    /// </summary>
    /// <returns>是否添加了服务</returns>
    public bool HasAnyService() => _registeredModules.Count > 0;

    /// <summary>
    /// 获取所有已注册的模块
    /// </summary>
    /// <returns>已注册模块的只读集合</returns>
    public IReadOnlyCollection<FeishuModule> GetRegisteredModules() => _registeredModules;
}
