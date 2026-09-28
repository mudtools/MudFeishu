// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;

namespace Mud.Feishu.AI.Tools.Emit;

/// <summary>
/// 生成代码标记文本（单一真相源）：产物类型与可执行成员上标注的
/// <c>[global::System.CodeDom.Compiler.GeneratedCode(...)]</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：产物文件头的 <c>&lt;auto-generated&gt;</c> 只对<b>逐文件</b>判定"是否生成代码"的
/// 工具（部分分析器、diff 忽略规则）有效；<c>[GeneratedCode]</c> 是<b>符号级</b>标记，覆盖"消费方按符号
/// 而非按文件过滤"的场景（Roslyn 分析器、CA/Sonar 规则抑制、调试器单步跳过、代码覆盖率排除）。
/// </para>
/// <para>
/// <b>标注粒度</b>：每个产物<b>类型</b>都标注；类型内的<b>可执行成员</b>（方法 / 属性）另行标注——
/// 字段与常量由类型级标注覆盖，逐个标注 20+ 个 <c>const</c> 只会把产物撑成噪声。
/// </para>
/// <para>
/// <b>版本号</b>取自生成器程序集自身的 <see cref="Version"/>（major.minor.build），与 Mud.HttpUtils
/// 生成器同一体例：消费方据此反查"这段产物出自哪个版本的生成器"，无需另设版本常量维护。
/// </para>
/// </remarks>
internal static class GeneratedCodeMarker
{
    /// <summary>生成器标识（工具链展示 / 过滤用；与分析器程序集内的生成器类型一一对应）。</summary>
    public const string GeneratorName = "Mud.Feishu.AI.Tools.FeishuToolSchemaGenerator";

    private static readonly Lazy<string> s_version = new(ResolveVersion);

    private static readonly Lazy<string> s_attribute = new(
        () => $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{s_version.Value}\")]");

    /// <summary>生成器版本（major.minor.build）。</summary>
    public static string GeneratorVersion => s_version.Value;

    /// <summary>特性文本（含首尾方括号），可直接写入产物并独占一行。</summary>
    public static string Attribute => s_attribute.Value;

    private static string ResolveVersion()
    {
        try
        {
            // 分析器宿主下程序集版本恒可用；仍以 try 包裹，避免反射受限宿主把生成整轮打断。
            var version = typeof(GeneratedCodeMarker).Assembly.GetName().Version ?? new Version(1, 0, 0);
            return version.Build < 0
                ? $"{version.Major}.{version.Minor}.0"
                : $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch (Exception)
        {
            return "1.0.0";
        }
    }
}
