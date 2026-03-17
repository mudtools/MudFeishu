// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.DataModels.WsEndpoint;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书认证服务实现
/// </summary>
/// <remarks>
/// 构建 <see cref="FeishuV3Authentication" /> 类的实例。
/// </remarks>
/// <param name="httpClient">FeishuHttpClient实例</param>
/// <param name="jsonSerializerOptions">Json序列化参数</param>
/// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
internal class FeishuV3Authentication(
    IEnhancedHttpClient httpClient,
    IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptions) : IFeishuAuthentication
{
    private readonly IEnhancedHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IOptionsMonitor<JsonSerializerOptions> _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
    private const string DefaultContentType = "application/json";

    // API端点常量定义
    private static class ApiEndpoints
    {
        public const string TenantAccessToken = "/open-apis/auth/v3/tenant_access_token/internal";
        public const string AppAccessToken = "/open-apis/auth/v3/app_access_token/internal";
        public const string OAuthToken = "/open-apis/authen/v2/oauth/token";
        public const string Authorize = "https://accounts.feishu.cn/open-apis/authen/v1/authorize";
        public const string UserInfo = "/open-apis/authen/v1/user_info";
        public const string WebSocketEndpoint = "/callback/ws/endpoint";
    }

    /// <summary>
    /// 从Content-Type字符串中提取媒体类型部分，去除字符集信息。
    /// </summary>
    private static string GetMediaType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return DefaultContentType;
        }

        int separatorIndex = contentType.IndexOf(';');
        return separatorIndex >= 0
            ? contentType.Substring(0, separatorIndex).Trim()
            : contentType.Trim();
    }

    /// <summary>
    /// 创建JSON HTTP请求
    /// </summary>
    private HttpRequestMessage CreateJsonRequest<T>(string endpoint, T data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        var jsonContent = JsonSerializer.Serialize(data, _jsonSerializerOptions.CurrentValue);
        request.Content = new StringContent(
            jsonContent,
            Encoding.UTF8,
            GetMediaType(DefaultContentType)
        );
        return request;
    }

    /// <summary>
    /// 获取租户访问令牌
    /// </summary>
    public async Task<TenantAppCredentialResult?> GetTenantAccessTokenAsync(
        AppCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(credentials, nameof(credentials));

        using var request = CreateJsonRequest(ApiEndpoints.TenantAccessToken, credentials);
        return await _httpClient.SendAsync<TenantAppCredentialResult>(request, cancellationToken);
    }

    /// <summary>
    /// 获取应用访问令牌
    /// </summary>
    public async Task<AppCredentialResult?> GetAppAccessTokenAsync(
        AppCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(credentials, nameof(credentials));

        using var request = CreateJsonRequest(ApiEndpoints.AppAccessToken, credentials);
        return await _httpClient.SendAsync<AppCredentialResult>(request, cancellationToken);
    }

    /// <summary>
    /// 获取OAuth访问令牌
    /// </summary>
    public async Task<OAuthCredentialsResult?> GetOAuthenAccessTokenAsync(
        OAuthTokenRequest credentials,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(credentials, nameof(credentials));

        using var request = CreateJsonRequest(ApiEndpoints.OAuthToken, credentials);
        return await _httpClient.SendAsync<OAuthCredentialsResult>(request, cancellationToken);
    }

    /// <summary>
    /// 刷新OAuth访问令牌
    /// </summary>
    public async Task<OAuthCredentialsResult?> GetOAuthenRefreshAccessTokenAsync(
        OAuthRefreshTokenRequest credentials,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(credentials, nameof(credentials));

        using var request = CreateJsonRequest(ApiEndpoints.OAuthToken, credentials);
        return await _httpClient.SendAsync<OAuthCredentialsResult>(request, cancellationToken);
    }

    /// <summary>
    /// 获取用户访问令牌授权URL
    /// </summary>
    public async Task<AuthorizeResult?> GetUserAccessTokenAsync(
        string clientId,
        string responseType,
        string redirectUri,
        string? scope = null,
        string? state = null,
        string? codeChallenge = null,
        string? codeChallengeMethod = null,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNullOrEmpty(clientId, nameof(clientId));
        ExceptionUtils.ThrowIfNullOrEmpty(responseType, nameof(responseType));
        ExceptionUtils.ThrowIfNullOrEmpty(redirectUri, nameof(redirectUri));

        // 构建查询参数
        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = clientId.Trim(),
            ["response_type"] = responseType.Trim(),
            ["redirect_uri"] = redirectUri.Trim()
        };

        AddOptionalQueryParam(queryParams, "scope", scope);
        AddOptionalQueryParam(queryParams, "state", state);
        AddOptionalQueryParam(queryParams, "code_challenge", codeChallenge);
        AddOptionalQueryParam(queryParams, "code_challenge_method", codeChallengeMethod);

        // 构建URL
        var urlBuilder = new UriBuilder(ApiEndpoints.Authorize)
        {
            Query = BuildQueryString(queryParams)
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
        return await _httpClient.SendAsync<AuthorizeResult>(request, cancellationToken);
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    public async Task<FeishuApiResult<GetUserDataResult>?> GetUserInfoAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNullOrEmpty(token, nameof(token));

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiEndpoints.UserInfo);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

        return await _httpClient.SendAsync<FeishuApiResult<GetUserDataResult>>(request, cancellationToken);
    }

    /// <summary>
    /// 获取WebSocket端点
    /// </summary>
    public async Task<FeishuApiResult<WsEndpointResult>?> GetWebSocketEndpointAsync(
        WsAppCredentials appCredentials,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(appCredentials, nameof(appCredentials));

        using var request = CreateJsonRequest(ApiEndpoints.WebSocketEndpoint, appCredentials);
        return await _httpClient.SendAsync<FeishuApiResult<WsEndpointResult>>(request, cancellationToken);
    }

    /// <summary>
    /// 添加可选查询参数
    /// </summary>
    private static void AddOptionalQueryParam(IDictionary<string, string?> parameters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value.Trim();
        }
    }

    /// <summary>
    /// 构建查询字符串
    /// </summary>
    private static string BuildQueryString(IDictionary<string, string?> parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in parameters)
        {
            if (!string.IsNullOrWhiteSpace(param.Value))
            {
                query[param.Key] = param.Value;
            }
        }
        return query.ToString() ?? string.Empty;
    }
}