// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Serialization.Metadata;
using Mud.Feishu.Abstractions.Utilities;

// DataModels 项目的 internal JsonContext，通过 InternalsVisibleTo 暴露
// 注意：这些 Context 有 #if NET8_0_OR_GREATER 条件编译指令，只在 AOT 目标框架下可用

namespace Mud.Feishu.Extensions;

/// <summary>
/// Feishu JSON 解析器扩展，用于配置 DataModels 源生成上下文
/// </summary>
public static class FeishuJsonResolverExtensions
{
    /// <summary>
    /// 配置 DataModels 的 JSON 解析器
    /// 将 20 个已生成的 DataModels Context 合并为一个解析器并注入到 FeishuJsonDefaults
    /// </summary>
    public static void ConfigureDataModelsResolver()
    {
        // TODO: 下一阶段实现 - 通过条件编译仅在 net8.0+ 下合并 DataModels Context
        // 当前阶段：提供一个基础的 DataModels resolver 来验证机制
        
        throw new NotImplementedException(
            "ConfigureDataModelsResolver 尚未在 net8.0+ 条件下实现。" + 
            "下一阶段将使用 #if NET8_0_OR_GREATER 条件编译合并 DataModels Context。"
        );
    }
}