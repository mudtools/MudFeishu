# 认证授权

## 功能描述
该接口提供了飞书认证授权相关的完整功能，包括获取租户访问凭证、应用访问凭证、用户访问凭证以及OAuth授权流程等。接口支持多种token类型，满足不同场景下的认证需求，是企业应用与飞书平台集成的核心认证接口。

接口详细文档请参见：[飞书官方文档](https://open.feishu.cn/document/server-docs/authentication-management/access-token/tenant_access_token_internal)

## 函数列表

| 函数名称 | HTTP方法 | 功能描述 |
|---------|---------|---------|
| GetTenantAccessTokenAsync | POST | 获取自建应用租户访问凭证 |
| GetAppAccessTokenAsync | POST | 获取自建应用访问凭证 |
| GetOAuthenAccessTokenAsync | POST | 获取用户访问凭证 |
| GetOAuthenRefreshAccessTokenAsync | POST | 刷新用户访问凭证 |
| GetAuthorizeAsync | GET | 发起用户授权 |

## GetTenantAccessTokenAsync
```csharp
Task<TenantAppCredentialResult> GetTenantAccessTokenAsync(
    [Body] AppCredentials credentials,
    CancellationToken cancellationToken = default);
```

**认证**：无需额外认证，使用应用的app_id和app_secret

**参数**：
- `credentials` (必填): 应用唯一标识及应用秘钥信息
  - `app_id`: 应用唯一标识，创建应用后获得
  - `app_secret`: 应用秘钥，创建应用后获得
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "tenant_access_token": "t-xxx",
  "expire": 7200
}

// 错误响应示例
{
  "code": 99991663,
  "msg": "app_id or app_secret invalid"
}
```

**说明**：
- tenant_access_token的最大有效期是2小时
- 剩余有效期小于30分钟时，调用本接口会返回一个新的tenant_access_token，这会同时存在两个有效的token
- 剩余有效期大于等于30分钟时，调用本接口会返回原有的tenant_access_token

**代码示例**：
```csharp
// 获取租户访问令牌示例
public async Task<string> GetTenantAccessToken()
{
    try
    {
        var credentials = new AppCredentials
        {
            AppId = "cli_your_app_id",
            AppSecret = "your_app_secret"
        };

        var result = await _authApi.GetTenantAccessTokenAsync(credentials);
        
        if (result.Code == 0 && !string.IsNullOrEmpty(result.TenantAccessToken))
        {
            Console.WriteLine($"获取租户令牌成功，有效期: {result.Expire}秒");
            return result.TenantAccessToken;
        }
        else
        {
            Console.WriteLine($"获取租户令牌失败: {result.Msg}");
            return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取租户令牌异常: {ex.Message}");
        return null;
    }
}
```

---

## GetAppAccessTokenAsync
```csharp
Task<AppCredentialResult> GetAppAccessTokenAsync(
    [Body] AppCredentials credentials,
    CancellationToken cancellationToken = default);
```

**认证**：无需额外认证，使用应用的app_id和app_secret

**参数**：
- `credentials` (必填): 应用唯一标识及应用秘钥信息
  - `app_id`: 应用唯一标识，创建应用后获得
  - `app_secret`: 应用秘钥，创建应用后获得
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "app_access_token": "app-xxx",
  "tenant_access_token": "t-xxx",
  "expire": 7200
}

// 错误响应示例
{
  "code": 99991663,
  "msg": "app_id or app_secret invalid"
}
```

**说明**：
- app_access_token的最大有效期是2小时
- 剩余有效期小于30分钟时，调用本接口会返回一个新的app_access_token，这会同时存在两个有效的token
- 剩余有效期大于等于30分钟时，调用本接口会返回原有的app_access_token

**代码示例**：
```csharp
// 获取应用访问令牌示例
public async Task<AppCredentialResult> GetAppAccessToken()
{
    try
    {
        var credentials = new AppCredentials
        {
            AppId = "cli_your_app_id",
            AppSecret = "your_app_secret"
        };

        var result = await _authApi.GetAppAccessTokenAsync(credentials);
        
        if (result.Code == 0)
        {
            Console.WriteLine($"获取应用令牌成功");
            Console.WriteLine($"App Token: {result.AppAccessToken}");
            Console.WriteLine($"Tenant Token: {result.TenantAccessToken}");
            Console.WriteLine($"有效期: {result.Expire}秒");
            
            // 缓存token，避免频繁请求
            await CacheTokenAsync(result);
        }
        else
        {
            Console.WriteLine($"获取应用令牌失败: {result.Msg}");
        }
        
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取应用令牌异常: {ex.Message}");
        return null;
    }
}

private async Task CacheTokenAsync(AppCredentialResult result)
{
    // 实现token缓存逻辑，例如存储到Redis或内存中
    // 缓存键可以设置为 "feishu_app_token"，过期时间设置为expire秒
    await Task.CompletedTask;
}
```

---

## GetOAuthenAccessTokenAsync
```csharp
Task<OAuthCredentialsResult> GetOAuthenAccessTokenAsync(
    [Body] OAuthTokenRequest credentials,
    CancellationToken cancellationToken = default);
```

**认证**：无需额外认证，使用授权码换取用户访问凭证

**参数**：
- `credentials` (必填): 获取user_access_token的请求参数
  - `grant_type`: 授权类型，固定值"authorization_code"
  - `client_id`: 应用的App ID
  - `client_secret`: 应用的App Secret
  - `code`: 授权码（从授权回调中获得）
  - `redirect_uri`: 应用回调地址（可选）
  - `scope`: 权限范围（可选）
  - `code_verifier`: PKCE验证码（可选）
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "access_token": "u-xxx",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "r-xxx",
  "refresh_token_expires_in": 2592000,
  "scope": "contact:base calendar:calendar"
}

// 错误响应示例
{
  "code": 400,
  "msg": "invalid authorization code",
  "error": "invalid_grant",
  "error_description": "invalid authorization code"
}
```

**说明**：
- user_access_token为用户访问凭证，使用该凭证可以以用户身份调用OpenAPI
- refresh_token为刷新凭证，可以用来获取新的user_access_token
- 当用户授予offline_access权限时，才会返回refresh_token

**代码示例**：
```csharp
// 通过授权码获取用户访问令牌示例
public async Task<OAuthCredentialsResult> GetUserAccessToken(string authCode)
{
    try
    {
        var tokenRequest = new OAuthTokenRequest
        {
            GrantType = "authorization_code",
            ClientId = "cli_your_app_id",
            ClientSecret = "your_app_secret",
            Code = authCode,
            RedirectUri = "https://your-domain.com/callback",
            Scope = "contact:base calendar:calendar"
        };

        var result = await _authApi.GetOAuthenAccessTokenAsync(tokenRequest);
        
        if (result.Code == 0 && !string.IsNullOrEmpty(result.AccessToken))
        {
            Console.WriteLine($"获取用户令牌成功");
            Console.WriteLine($"Access Token: {result.AccessToken}");
            Console.WriteLine($"Refresh Token: {result.RefreshToken}");
            Console.WriteLine($"有效期: {result.ExpiresIn}秒");
            
            // 保存用户令牌到数据库或会话中
            await SaveUserTokenAsync(result);
        }
        else
        {
            Console.WriteLine($"获取用户令牌失败: {result.ErrorDescription}");
        }
        
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取用户令牌异常: {ex.Message}");
        return null;
    }
}

private async Task SaveUserTokenAsync(OAuthCredentialsResult result)
{
    // 将用户令牌保存到数据库或会话中
    // 可以根据refresh_token_expires_in设置合理的过期时间
    await Task.CompletedTask;
}
```

---

## GetOAuthenRefreshAccessTokenAsync
```csharp
Task<OAuthCredentialsResult> GetOAuthenRefreshAccessTokenAsync(
    [Body] OAuthRefreshTokenRequest credentials,
    CancellationToken cancellationToken = default);
```

**认证**：无需额外认证，使用refresh_token刷新用户访问凭证

**参数**：
- `credentials` (必填): OAuth令牌刷新请求参数
  - `grant_type`: 授权类型，固定值"refresh_token"
  - `client_id`: 应用的App ID
  - `client_secret`: 应用的App Secret
  - `refresh_token`: 刷新令牌
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "access_token": "u-new-xxx",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "r-new-xxx",
  "refresh_token_expires_in": 2592000,
  "scope": "contact:base calendar:calendar"
}

// 错误响应示例
{
  "code": 400,
  "msg": "invalid refresh token",
  "error": "invalid_grant",
  "error_description": "invalid refresh token"
}
```

**说明**：用于刷新user_access_token，同时获取新的refresh_token。当refresh_token过期时，需要重新进行OAuth授权流程。

**代码示例**：
```csharp
// 刷新用户访问令牌示例
public async Task<OAuthCredentialsResult> RefreshUserToken(string refreshToken)
{
    try
    {
        var refreshRequest = new OAuthRefreshTokenRequest
        {
            GrantType = "refresh_token",
            ClientId = "cli_your_app_id",
            ClientSecret = "your_app_secret",
            RefreshToken = refreshToken
        };

        var result = await _authApi.GetOAuthenRefreshAccessTokenAsync(refreshRequest);
        
        if (result.Code == 0 && !string.IsNullOrEmpty(result.AccessToken))
        {
            Console.WriteLine($"刷新用户令牌成功");
            Console.WriteLine($"新Access Token: {result.AccessToken}");
            Console.WriteLine($"新Refresh Token: {result.RefreshToken}");
            
            // 更新数据库中的用户令牌
            await UpdateUserTokenAsync(result);
        }
        else
        {
            Console.WriteLine($"刷新用户令牌失败: {result.ErrorDescription}");
            // 刷新失败，可能需要重新授权
            await HandleRefreshFailure();
        }
        
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"刷新用户令牌异常: {ex.Message}");
        return null;
    }
}

private async Task UpdateUserTokenAsync(OAuthCredentialsResult result)
{
    // 更新数据库中的用户令牌信息
    await Task.CompletedTask;
}

private async Task HandleRefreshFailure()
{
    // 处理刷新失败的情况，比如清除用户会话，要求重新授权
    Console.WriteLine("用户令牌已过期，需要重新授权");
    await Task.CompletedTask;
}
```

---

## GetAuthorizeAsync
```csharp
Task<AuthorizeResult> GetAuthorizeAsync(
   [Query] string client_id,
   [Query] string response_type,
   [Query] string redirect_uri,
   [Query] string? scope = null,
   [Query] string? state = null,
   [Query] string? code_challenge = null,
   [Query] string? code_challenge_method = null,
   CancellationToken cancellationToken = default);
```

**认证**：无需认证，用于发起用户授权

**参数**：
- `client_id` (必填): 应用的App ID，可以在开发者后台查看
- `response_type` (必填): 授权类型，固定值"code"
- `redirect_uri` (必填): 应用重定向地址，用户授权成功后会跳转至该地址
- `scope` (可选): 用户需要增量授予应用的权限
- `state` (可选): 用于维护请求和回调状态的附加字符串，建议用于防止CSRF攻击
- `code_challenge` (可选): PKCE流程的安全验证码
- `code_challenge_method` (可选): 生成code_challenge的方法
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例（重定向到redirect_uri）
{
  "code": "a61hb967bd094dge949h79bbexd16dfe",
  "state": "custom_state_value"
}

// 使用方式：
// 浏览器重定向到：
// https://accounts.feishu.cn/open-apis/authen/v1/authorize?client_id=cli_xxx&response_type=code&redirect_uri=https://example.com/callback&scope=contact:base&state=random_string
```

**说明**：
- 本接口用于发起用户授权，应用在用户同意授权后将获得授权码code
- 授权码的有效期为5分钟，且只能被使用一次
- state参数会原样回传，强烈建议校验state参数前后是否一致以防止CSRF攻击
- 支持PKCE流程增强授权码的安全性

**代码示例**：
```csharp
// 发起用户授权示例
public string BuildAuthorizationUrl()
{
    try
    {
        var clientId = "cli_your_app_id";
        var redirectUri = "https://your-domain.com/auth/callback";
        var scope = "contact:base calendar:calendar"; // 根据应用所需权限设置
        var state = GenerateSecureState(); // 生成安全的随机字符串
        
        // 构建授权URL
        var authUrl = $"https://accounts.feishu.cn/open-apis/authen/v1/authorize?" +
                     $"client_id={Uri.EscapeDataString(clientId)}&" +
                     $"response_type=code&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"scope={Uri.EscapeDataString(scope)}&" +
                     $"state={Uri.EscapeDataString(state)}";
        
        // 将state保存到会话中，用于后续验证
        SaveStateToSession(state);
        
        Console.WriteLine($"授权URL: {authUrl}");
        return authUrl;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"构建授权URL异常: {ex.Message}");
        return null;
    }
}

private string GenerateSecureState()
{
    // 生成安全的随机字符串，用于防止CSRF攻击
    var bytes = new byte[32];
    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
    {
        rng.GetBytes(bytes);
    }
    return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
}

private void SaveStateToSession(string state)
{
    // 将state保存到会话中，有效期5分钟
    // 可以使用Redis或内存缓存
    Console.WriteLine($"保存授权状态: {state}");
}

// 处理授权回调示例
public async Task<OAuthCredentialsResult> HandleAuthCallback(string code, string state)
{
    try
    {
        // 验证state参数，防止CSRF攻击
        var savedState = GetStateFromSession();
        if (string.IsNullOrEmpty(savedState) || savedState != state)
        {
            Console.WriteLine("状态验证失败，可能存在CSRF攻击");
            return null;
        }
        
        // 使用授权码获取用户令牌
        return await GetUserAccessToken(code);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"处理授权回调异常: {ex.Message}");
        return null;
    }
}
```

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2025-11-20 | 初始版本，包含完整的认证授权功能 |
