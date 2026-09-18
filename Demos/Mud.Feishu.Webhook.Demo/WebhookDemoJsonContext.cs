// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Webhook.Demo;

/// <summary>
/// Webhook Demo 的 Minimal API JSON 源生成上下文。
/// </summary>
/// <remarks>
/// Native AOT 下反射序列化不可用，Demo 的全部匿名响应类型已改为具名 DTO，
/// 并在此注册；Minimal API 通过 <c>ConfigureHttpJsonOptions</c> 将本上下文追加到
/// <c>TypeInfoResolverChain</c>，请求体反序列化与响应序列化均走源生成代码。
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// 诊断端点（DiagnosticsEndpoint.cs）
[JsonSerializable(typeof(HandlersDiagnosticsResponse))]
[JsonSerializable(typeof(HandlerInfo))]
[JsonSerializable(typeof(HandlersSummary))]
// 多应用端点（MultiAppEndpoint.cs）
[JsonSerializable(typeof(MultiAppInfoResponse))]
[JsonSerializable(typeof(MultiAppInfoExamples))]
[JsonSerializable(typeof(WebhookEndpointsInfo))]
[JsonSerializable(typeof(MultiAppRoutesResponse))]
[JsonSerializable(typeof(AppRouteInfo))]
// 测试端点（TestEndpoint.cs）
[JsonSerializable(typeof(CaptureResponse))]
[JsonSerializable(typeof(CapturedRequestsResponse))]
[JsonSerializable(typeof(CapturedRequestSummary))]
[JsonSerializable(typeof(ClearResponse))]
[JsonSerializable(typeof(MockFeishuEventResponse))]
[JsonSerializable(typeof(MockFeishuRequest))]
[JsonSerializable(typeof(CapturedRequest))]
[JsonSerializable(typeof(MockFeishuEvent))]
[JsonSerializable(typeof(Dictionary<string, string>))]
// Results.Problem / Results.NotFound 的默认响应体（AOT 下同样需要显式注册）
[JsonSerializable(typeof(ProblemDetails))]
internal partial class WebhookDemoJsonContext : JsonSerializerContext
{
}
