// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// FeishuApiResult 系列泛型响应包装的 JSON 源生成上下文。
/// 手工兜底实现，待 Scaffolder 工具扩展后替换为自动生成版本。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
// 注册基本的 FeishuApiResult 类型
[JsonSerializable(typeof(FeishuApiResult))]
[JsonSerializable(typeof(FeishuApiResult<object>))]
[JsonSerializable(typeof(FeishuNullDataApiResult))]
internal partial class FeishuApiResultJsonContext : JsonSerializerContext { }
#endif