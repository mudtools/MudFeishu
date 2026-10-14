// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// <c>Source</c> 源挂钩归一与 SDK 成员解析（设计文档 §4.3：引擎经本单元把
/// 「<c>nameof</c> 或字符串」两种合法输入归一为 <c>(InterfaceName, MethodName)</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 泛化自上游 <c>Extractors.ResolveSourceMember</c> 与 <c>CuratedToolScanner</c> 的内联切分：
/// 命名空间前缀切分用剖面槽 <see cref="SdkToolProfileModel.SdkNamespaceRoot"/>
/// （替代硬编码 <c>"Mud.Feishu."</c>）。解析本身是固定两步（先全名命中，失败再全局按名兜底），
/// 两步逻辑平台无关（设计文档 §4.1 v2.1 注：不设 <c>SourceResolutionStrategy</c> 槽）。
/// </para>
/// <para>
/// <b>引擎不变量（不得被任何 profile 关闭）</b>：方法查找必须并入 <c>AllInterfaces</c>——
/// 令牌派生接口常为空接口、方法声明在基接口上，只查 <c>GetMembers(name)</c> 会恒返回空
/// （否则聚合路径恒产出空条目，这是上游评审纠正过的设计硬伤）。
/// </para>
/// </remarks>
internal static class SourceResolver
{
    /// <summary>
    /// 归一 <c>Source</c> 声明为 <c>(InterfaceName, MethodName)</c>：
    /// 先剥 <c>{SdkNamespaceRoot}.</c> 前缀（全限定字符串形态），再按最后一个 <c>.</c> 切分。
    /// 无 <c>.</c> 时方法名为空串（交由 <see cref="Resolve"/> 报「未找到方法」）。
    /// </summary>
    public static bool TryNormalize(string? source, SdkToolProfileModel profile, out string interfaceName, out string methodName)
    {
        interfaceName = string.Empty;
        methodName = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            var text = source!;
            var prefix = profile.SdkNamespaceRoot + ".";
            if (text.StartsWith(prefix, StringComparison.Ordinal))
            {
                text = text.Substring(prefix.Length);
            }

            var separatorIndex = text.LastIndexOf('.');
            if (separatorIndex > 0)
            {
                interfaceName = text.Substring(0, separatorIndex);
                methodName = text.Substring(separatorIndex + 1);
            }
            else
            {
                interfaceName = text;
            }

            return interfaceName.Length > 0;
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(SourceResolver), ex);
            interfaceName = string.Empty;
            methodName = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// 按「接口名.方法名」解析 SDK 成员（源挂钩的解析器）。
    /// </summary>
    /// <remarks>
    /// 重载选择：取参数最多者（canonical 形态）。
    /// </remarks>
    /// <param name="compilation">当前编译。</param>
    /// <param name="typeName">接口名（如 <c>IFeishuTenantV3User</c>）。</param>
    /// <param name="methodName">方法名（如 <c>GetBatchUsersAsync</c>）。</param>
    /// <param name="profile">当前剖面（提供 <see cref="SdkToolProfileModel.SdkNamespaceRoot"/>）。</param>
    /// <param name="failure">失败原因（成功时为 <see langword="null"/>）。</param>
    /// <returns>解析到的类型与方法；任一环节失败返回 <see langword="null"/>。</returns>
    public static (INamedTypeSymbol Type, IMethodSymbol Method)? Resolve(
        Compilation compilation,
        string typeName,
        string methodName,
        SdkToolProfileModel profile,
        out string? failure)
    {
        failure = null;
        try
        {
            var type = compilation.GetTypeByMetadataName(profile.SdkNamespaceRoot + "." + typeName)
                       ?? FindTypeByName(compilation.GlobalNamespace, typeName);
            if (type is null)
            {
                failure = $"未找到 SDK 接口 {typeName}";
                return null;
            }

            var method = type.GetMembers(methodName).OfType<IMethodSymbol>()
                .Concat(type.AllInterfaces.SelectMany(i => i.GetMembers(methodName).OfType<IMethodSymbol>()))
                .Where(static m => m.MethodKind == MethodKind.Ordinary)
                .OrderByDescending(static m => m.Parameters.Length)
                .FirstOrDefault();
            if (method is null)
            {
                failure = $"接口 {typeName} 上未找到方法 {methodName}（已含全部基接口）";
                return null;
            }

            return (type, method);
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(SourceResolver), ex);
            failure = "解析异常（已兜底）：" + ex.GetType().Name;
            return null;
        }
    }

    private static INamedTypeSymbol? FindTypeByName(INamespaceSymbol ns, string typeName)
    {
        foreach (var type in ns.GetTypeMembers(typeName))
        {
            return type;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = FindTypeByName(child, typeName);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}