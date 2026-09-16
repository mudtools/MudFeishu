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
using Mud.Feishu.DataModels.WsEndpoint;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// FeishuApiResult 系列泛型响应包装的 JSON 源生成上下文。
/// 覆盖 Abstractions 程序集中定义的请求/响应 DTO 类型（认证接口等）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
// FeishuApiResult 基础类型
[JsonSerializable(typeof(FeishuApiResult))]
[JsonSerializable(typeof(FeishuApiResult<object>))]
[JsonSerializable(typeof(FeishuNullDataApiResult))]
// 认证接口请求 DTO
[JsonSerializable(typeof(AppCredentials))]
[JsonSerializable(typeof(OAuthTokenBaseRequest))]
[JsonSerializable(typeof(OAuthTokenRequest))]
[JsonSerializable(typeof(OAuthRefreshTokenRequest))]
[JsonSerializable(typeof(WsAppCredentials))]
// 认证接口响应 DTO
[JsonSerializable(typeof(TenantAppCredentialResult))]
[JsonSerializable(typeof(AppCredentialResult))]
[JsonSerializable(typeof(OAuthCredentialsResult))]
[JsonSerializable(typeof(AuthorizeResult))]
[JsonSerializable(typeof(GetUserDataResult))]
[JsonSerializable(typeof(WsEndpointResult))]
// FeishuApiResult<T> 闭合泛型（认证接口返回的包装类型）
[JsonSerializable(typeof(FeishuApiResult<GetUserDataResult>))]
[JsonSerializable(typeof(FeishuApiResult<WsEndpointResult>))]
public partial class FeishuApiResultJsonContext : JsonSerializerContext { }
#endif
