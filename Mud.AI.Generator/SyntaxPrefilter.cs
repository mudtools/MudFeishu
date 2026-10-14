// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mud.AI;

/// <summary>
/// 语法级保守预筛器（编译期性能修复 F-2a）：在昂贵的语义绑定（<c>GetDeclaredSymbol</c> /
/// 覆盖集合扫描）之前，用纯 token 文本扫描判断「编译中是否可能出现目标标记」。
/// </summary>
/// <remarks>
/// <para>
/// <b>保守不漏报纪律</b>：命中判定为「任一标识符 token 的文本<b>包含</b>目标标记（Ordinal 子串）」——
/// 别名（<c>using JSC = ...JsonSerializerContext;</c>）、完全限定名、省略 <c>Attribute</c> 后缀、
/// 生成文件、partial 分部声明均含目标标识符 token，不会漏；误触发（如自定义类型名恰好含标记）
/// 只是退回精确路径，无害。
/// </para>
/// <para>
/// <b>缓存粒度（评审修正）</b>：按 <see cref="SyntaxTree"/> 做 <see cref="ConditionalWeakTable{TKey, TValue}"/>
/// 缓存——语法树对象跨 <see cref="Compilation"/> 复用（文件未改动时树恒定），IDE 稳态下
/// 未改动文件的扫描结果真正命中；若按 Compilation 缓存，每次击键的新编译都会全量重扫
/// （正是本次修复要消灭的失效模式）。Compilation 层仅做各树结果的 O(树数) 聚合。
/// </para>
/// <para>
/// <b>适用边界</b>：只可用于「目标声明必然在本编译源文本中出现目标标识符」的判定
/// （如 [HttpClientApi] / [HttpJsonSerializable] / [SdkToolProfile] 标注面）。
/// <b>不可</b>用于「经引用程序集中转的继承链」判定（如 JsonSerializerContext 子类的传递基类），
/// 该场景须用语法结构预检（BaseList 存在性），见 AotDtoCoverageAnalyzer.ComputeLocalContexts。
/// </para>
/// </remarks>
internal static class SyntaxPrefilter
{
    /// <summary>
    /// 每语法树、每标记的扫描结果缓存（弱引用键，树被 GC 即随动回收）。
    /// 值为 <see cref="StrongBox{T}"/> 以容纳布尔（<see cref="ConditionalWeakTable{TKey, TValue}"/>
    /// 的值类型需装箱，与 <c>HasNet8OrGreaterDefine</c> 的既有做法一致）。
    /// </summary>
    private static readonly ConditionalWeakTable<SyntaxTree, ConcurrentDictionary<string, StrongBox<bool>>> ScanCache = new();

    /// <summary>
    /// 编译中是否<b>可能</b>存在携带目标标记的声明（保守判定：false 一定不存在，true 只是疑似）。
    /// </summary>
    /// <param name="compilation">编译单元。</param>
    /// <param name="marker1">目标标记（标识符 token 文本的 Ordinal 子串）。</param>
    /// <param name="marker2">可选的第二标记（任一命中即视为疑似）。</param>
    internal static bool MayContain(Compilation compilation, string marker1, string? marker2 = null)
    {
        if (compilation is null || string.IsNullOrEmpty(marker1))
            return true; // 无法判定 → 保守进入精确路径

        foreach (var tree in compilation.SyntaxTrees)
        {
            if (TreeMayContain(tree, marker1))
                return true;
            if (marker2 != null && TreeMayContain(tree, marker2))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 单个类型声明的特性名文本预检（F-2b）：属性名中不含目标标记的声明，
    /// 其符号绑定（<c>GetDeclaredSymbol</c>）可安全跳过。
    /// </summary>
    /// <remarks>
    /// 只查 <c>AttributeLists</c> 的名称 token：目标特性必然在某个 partial 分部的特性列表中
    /// 以标识符形式出现（省略 <c>Attribute</c> 后缀 / 完全限定 / 别名均含目标 token）。
    /// 误命中（属性名含标记但精确比较不通过）只是多付一次符号绑定，无害。
    /// </remarks>
    internal static bool MayHaveAttribute(TypeDeclarationSyntax typeDecl, string marker)
    {
        if (typeDecl is null || string.IsNullOrEmpty(marker))
            return true; // 无法判定 → 保守绑定

        foreach (var attributeList in typeDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                foreach (var token in attribute.Name.DescendantTokens())
                {
                    if (token.IsKind(SyntaxKind.IdentifierToken) &&
                        token.ValueText.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TreeMayContain(SyntaxTree tree, string marker)
    {
        var perTree = ScanCache.GetValue(
            tree,
            static _ => new ConcurrentDictionary<string, StrongBox<bool>>(StringComparer.Ordinal));

        // GetOrAdd 工厂在并发下可能重复执行，但扫描幂等，结果无害。
        return perTree.GetOrAdd(marker, m => new StrongBox<bool>(ScanTree(tree, m))).Value;
    }

    private static bool ScanTree(SyntaxTree tree, string marker)
    {
        // 仅活动代码的标识符 token 参与判定：#if 禁用块是 DisabledTextTrivia（非 token），
        // 其中的声明本就不会进入编译语义层，分析器同样看不见——预筛与精确路径口径一致。
        foreach (var token in tree.GetRoot().DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.IdentifierToken) &&
                token.ValueText.IndexOf(marker, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}