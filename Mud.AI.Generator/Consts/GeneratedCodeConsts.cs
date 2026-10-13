// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.AI;

/// <summary>
/// 生成代码上标注的常量文本（<c>[CompilerGenerated]</c> / <c>[GeneratedCode]</c>）。
/// </summary>
/// <remarks>
/// Mud.AI.Generator 只承载工具面竖井（v2.3 自组件仓 Mud.HttpUtils.Generator 迁入），
/// 故仅保留生成器基座需要的两个标记常量；HTTP 生成族的 using 清单与特性文本随竖井外部分留组件仓。
/// </remarks>
internal static class GeneratedCodeConsts
{
    /// <summary>生成代码上标注的 <c>[CompilerGenerated]</c> 特性文本。</summary>
    public const string CompilerGeneratedAttribute = "[global::System.Runtime.CompilerServices.CompilerGenerated]";

    private static readonly Lazy<string> s_cachedVersion = new(ResolveVersion);

    /// <summary>生成代码上标注的 <c>[GeneratedCode]</c> 特性文本（标识 = 本生成器）。</summary>
    public static string GeneratedCodeAttribute => $"[global::System.CodeDom.Compiler.GeneratedCode(\"Mud.AI.Generator\", \"{s_cachedVersion.Value}\")]";

    /// <summary>生成器自身版本号（major.minor.build；用于增量 salt）。</summary>
    internal static string GeneratorVersion => s_cachedVersion.Value;

    private static string ResolveVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version ?? new Version(1, 0, 0);
            return version.Build < 0
                ? $"{version.Major}.{version.Minor}.0"
                : $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            return "1.0.0";
        }
    }
}
