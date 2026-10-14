// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.AI.ToolSurface.Emit;

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
/// <b>泛化点</b>（上游 <c>GeneratorName = "Mud.Feishu.AI.Tools.FeishuToolSchemaGenerator"</c> 硬编码）：
/// 生成器标识改由剖面驱动——<c>{ProductPrefix}SchemaGenerator</c>（Feishu 剖面即
/// <c>FeishuToolSchemaGenerator</c>，与上游产物文件头注释逐字节一致）。§4.2 十七组槽无"生成器标识"槽，
/// 该串只进产物注释与特性实参（不进 golden、不被代码引用），由 <c>ProductPrefix</c> 派生是零漂移口径。
/// </para>
/// <para>
/// <b>版本号</b>取自生成器程序集自身的 <see cref="Version"/>（major.minor.build），与 Mud.AI
/// 生成器同一体例：消费方据此反查"这段产物出自哪个版本的生成器"，无需另设版本常量维护。
/// </para>
/// </remarks>
internal static class GeneratedCodeMarker
{
    /// <summary>按剖面名缓存的特性文本（同编译内剖面名唯一，见 <c>SdkToolProfileModel.Name</c>）。</summary>
    private static readonly ConcurrentDictionary<string, string> s_attributeCache = new(StringComparer.Ordinal);

    private static readonly Lazy<string> s_version = new(ResolveVersion);

    /// <summary>
    /// 生成器标识短名（产物文件头注释用；由剖面 <c>ProductPrefix</c> 派生，
    /// Feishu 剖面即上游注释原文 <c>FeishuToolSchemaGenerator</c>）。
    /// </summary>
    public static string GeneratorName(SdkToolProfileModel profile)
        => profile.ProductPrefix + "SchemaGenerator";

    /// <summary>
    /// 生成器标识全名（<c>GeneratedCode</c> 特性实参用；上游为
    /// <c>"Mud.Feishu.AI.Tools.FeishuToolSchemaGenerator"</c>，即生成器所在命名空间 + 短名。
    /// 十七组槽无"生成器命名空间"槽，生成器与工具特性同程序集同命名空间根，
    /// 故以 <c>ToolAttributeNamespace</c> 拼接——对 Feishu 剖面逐字节等于上游）。
    /// </summary>
    public static string FullGeneratorName(SdkToolProfileModel profile)
        => profile.ToolAttributeNamespace + "." + GeneratorName(profile);

    /// <summary>生成器版本（major.minor.build）。</summary>
    public static string GeneratorVersion => s_version.Value;

    /// <summary>特性文本（含首尾方括号），可直接写入产物并独占一行。</summary>
    public static string Attribute(SdkToolProfileModel profile)
    {
        try
        {
            return s_attributeCache.GetOrAdd(
                profile.Name,
                _ => $"[global::System.CodeDom.Compiler.GeneratedCode(\"{FullGeneratorName(profile)}\", \"{GeneratorVersion}\")]");
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(GeneratedCodeMarker), ex);
            // 兜底降级为 CompilerGenerated（符号级标记仍在，仅丢生成器身份文本）。
            return "[global::System.Runtime.CompilerServices.CompilerGenerated]";
        }
    }

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

/// <summary>
/// 发射文本的行纪律扩展（FIX-16，对齐 <c>TransitiveCodeGenerator.GenerateFileHeader</c>）：
/// 产物代码文本的换行符<b>固定 <c>\n</c></b>，禁用 <c>StringBuilder.AppendLine</c>——
/// 后者取 <c>Environment.NewLine</c>，Windows 宿主产出 <c>\r\n</c>，产物与 golden 的
/// 逐字节比对会跨平台漂移。
/// </summary>
internal static class EmitStringBuilderExtensions
{
    /// <summary>追加一行（含行尾 <c>\n</c>）。</summary>
    public static void Line(this StringBuilder source, string text) => source.Append(text).Append('\n');

    /// <summary>追加空行。</summary>
    public static void Line(this StringBuilder source) => source.Append('\n');
}