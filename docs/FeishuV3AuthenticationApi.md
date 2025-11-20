# 飞书认证授权 API 文档

## 概述

本文档详细描述了飞书认证授权相关的 API 接口，包括获取访问令牌、OAuth 授权等功能。这些接口是使用其他飞书 API 的基础，所有业务 API 调用都需要先进行身份认证。

---

## 获取租户访问令牌

### 接口名称
获取自建应用 tenant_access_token

### 飞书接口URL
```
https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal
```

### 方法
POST

### 认证
无需认证（使用应用凭证进行身份验证）

### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| app_id | string | 是 | 应用唯一标识，创建应用后获得 | "cli_slkdjalasdkjasd" |
| app_secret | string | 是 | 应用密钥，创建应用后获得 | "dskLLdkasdjlasdKK" |

**请求参数结构：**
```json
{
  "app_id": "cli_slkdjalasdkjasd",
  "app_secret": "dskLLdkasdjlasdKK"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tenant_access_token": "t-caa7fc1b5b3b3d3e3b3b3b3b3b3b3b3b",
    "expire": 7200
  }
}
```

#### 失败响应
```json
{
  "code": 99991400,
  "msg": "app secret invalid"
}
```

**响应字段说明：**

| 字段名 | 类型 | 说明 |
|--------|------|------|
| code | int | 错误码，0 表示成功，非 0 表示失败 |
| msg | string | 错误描述信息 |
| tenant_access_token | string | 租户访问凭证 |
| expire | int | token 的过期时间，单位为秒 |

### 说明

1. **Token 有效期**：tenant_access_token 的最大有效期为 2 小时（7200 秒）
2. **Token 复用**：
   - 剩余有效期小于 30 分钟时，调用本接口会返回一个新的 tenant_access_token
   - 剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 tenant_access_token
3. **并发处理**：系统可能同时存在两个有效的 tenant_access_token，这是正常现象
4. **应用场景**：用于调用需要租户权限的 API，如用户管理、部门管理等

**使用示例：**
```csharp
// C# 调用示例
var credentials = new AppCredentials
{
    AppId = "cli_slkdjalasdkjasd",
    AppSecret = "dskLLdkasdjlasdKK"
};

var result = await _authApi.GetTenantAccessTokenAsync(credentials);
if (result.Code == 0)
{
    var token = result.TenantAccessToken;
    // 使用 token 调用其他 API
}
```

---

## 获取应用访问令牌

### 接口名称
获取自建应用 app_access_token

### 飞书接口URL
```
https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal
```

### 方法
POST

### 认证
无需认证（使用应用凭证进行身份验证）

### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| app_id | string | 是 | 应用唯一标识，创建应用后获得 | "cli_slkdjalasdkjasd" |
| app_secret | string | 是 | 应用密钥，创建应用后获得 | "dskLLdkasdjlasdKK" |

**请求参数结构：**
```json
{
  "app_id": "cli_slkdjalasdkjasd",
  "app_secret": "dskLLdkasdjlasdKK"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "app_access_token": "t-app.1ca5b5a5b5b3d3e3b3b3b3b3b3b3b3b",
    "tenant_access_token": "t-caa7fc1b5b3b3d3e3b3b3b3b3b3b3b3b",
    "expire": 7200
  }
}
```

#### 失败响应
```json
{
  "code": 99991400,
  "msg": "app secret invalid"
}
```

**响应字段说明：**

| 字段名 | 类型 | 说明 |
|--------|------|------|
| code | int | 错误码，0 表示成功，非 0 表示失败 |
| msg | string | 错误描述信息 |
| app_access_token | string | 应用访问凭证 |
| tenant_access_token | string | 租户访问凭证（同时返回） |
| expire | int | token 的过期时间，单位为秒 |

### 说明

1. **双重 Token**：该接口同时返回 app_access_token 和 tenant_access_token
2. **使用场景**：
   - app_access_token：用于应用级别的操作，如获取应用信息、配置等
   - tenant_access_token：用于租户级别的操作，如管理用户、部门等
3. **有效期**：两种 token 的最大有效期为 2 小时
4. **复用规则**：与 tenant_access_token 的复用规则相同

**使用示例：**
```csharp
// C# 调用示例
var credentials = new AppCredentials
{
    AppId = "cli_slkdjalasdkjasd",
    AppSecret = "dskLLdkasdjlasdKK"
};

var result = await _authApi.GetAppAccessTokenAsync(credentials);
if (result.Code == 0)
{
    var appToken = result.AppAccessToken;
    var tenantToken = result.TenantAccessToken;
    // 根据需要使用不同的 token
}
```

---

## 获取用户访问令牌

### 接口名称
获取 user_access_token（OAuth 令牌接口）

### 飞书接口URL
```
https://open.feishu.cn/open-apis/authen/v2/oauth/token
```

### 方法
POST

### 认证
无需认证（使用应用凭证和授权码进行身份验证）

### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| grant_type | string | 是 | 授权类型，固定值：authorization_code | "authorization_code" |
| client_id | string | 是 | 应用的 App ID | "cli_a5ca35a685b0x26e" |
| client_secret | string | 是 | 应用的 App Secret | "baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy" |
| code | string | 是 | 授权码，通过用户授权后获得 | "a61hb967bd094dge949h79bbexd16dfe" |
| redirect_uri | string | 否 | 应用回调地址，需与授权时一致 | "https://example.com/callback" |
| scope | string | 否 | 权限范围，用于缩减 token 权限 | "contact:user.base:readonly" |
| code_verifier | string | 否 | PKCE 流程的验证码 | "TxYmzM4PHLBlqm5NtnCmwxMH8mFlRWl_ipie3O0aVzo" |

**请求参数结构：**
```json
{
  "grant_type": "authorization_code",
  "client_id": "cli_a5ca35a685b0x26e",
  "client_secret": "baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy",
  "code": "a61hb967bd094dge949h79bbexd16dfe",
  "redirect_uri": "https://example.com/callback",
  "code_verifier": "TxYmzM4PHLBlqm5NtnCmwxMH8mFlRWl_ipie3O0aVzo"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "access_token": "u-7d1b7c5c8e9f0a1b2c3d4e5f6a7b8c9",
    "token_type": "Bearer",
    "expires_in": 3600,
    "refresh_token": "r-9e8d7c6b5a4f3e2d1c0b9a8f7e6d5c4b",
    "refresh_token_expires_in": 2592000,
    "scope": "contact:user.base:readonly contact:user.email:readonly"
  }
}
```

#### 失败响应
```json
{
  "code": 99991401,
  "msg": "authorization code expired",
  "data": {
    "error": "invalid_grant",
    "error_description": "authorization code expired"
  }
}
```

**响应字段说明：**

| 字段名 | 类型 | 说明 |
|--------|------|------|
| code | int | 错误码，0 表示成功，非 0 表示失败 |
| msg | string | 错误描述信息 |
| access_token | string | 用户访问凭证 |
| token_type | string | Token 类型，固定值：Bearer |
| expires_in | int | access_token 的有效期，单位为秒 |
| refresh_token | string | 刷新凭证，用于获取新的 access_token |
| refresh_token_expires_in | int | refresh_token 的有效期，单位为秒 |
| scope | string | 本次获得的权限列表 |
| error | string | 错误类型（仅在失败时返回） |
| error_description | string | 具体错误信息（仅在失败时返回） |

### 说明

1. **用户级别权限**：user_access_token 代表用户身份，可以以用户身份调用 API
2. **权限控制**：token 只能访问用户已授权的权限范围
3. **Token 刷新**：使用 refresh_token 可以获取新的 access_token，无需用户重新授权
4. **PKCE 支持**：支持 PKCE（Proof Key for Code Exchange）流程增强安全性
5. **授权码有效期**：授权码的有效期为 5 分钟，且只能使用一次

**使用示例：**
```csharp
// C# 调用示例
var oauthRequest = new OAuthTokenRequest
{
    GrantType = "authorization_code",
    ClientId = "cli_a5ca35a685b0x26e",
    ClientSecret = "baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy",
    Code = "a61hb967bd094dge949h79bbexd16dfe",
    RedirectUri = "https://example.com/callback"
};

var result = await _authApi.GetOAuthenAccessTokenAsync(oauthRequest);
if (result.Code == 0)
{
    var userToken = result.AccessToken;
    var refreshToken = result.RefreshToken;
    // 保存 token 用于后续调用
}
```

---

## 刷新用户访问令牌

### 接口名称
刷新 user_access_token

### 飞书接口URL
```
https://open.feishu.cn/open-apis/authen/v2/oauth/token
```

### 方法
POST

### 认证
无需认证（使用应用凭证和刷新令牌进行身份验证）

### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| grant_type | string | 是 | 授权类型，固定值：authorization_code | "authorization_code" |
| client_id | string | 是 | 应用的 App ID | "cli_a5ca35a685b0x26e" |
| client_secret | string | 是 | 应用的 App Secret | "baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy" |
| refresh_token | string | 是 | 刷新凭证 | "r-9e8d7c6b5a4f3e2d1c0b9a8f7e6d5c4b" |

**请求参数结构：**
```json
{
  "grant_type": "authorization_code",
  "client_id": "cli_a5ca35a685b0x26e",
  "client_secret": "baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy",
  "refresh_token": "r-9e8d7c6b5a4f3e2d1c0b9a8f7e6d5c4b"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "access_token": "u-7d1b7c5c8e9f0a1b2c3d4e5f6a7b8c9",
    "token_type": "Bearer",
    "expires_in": 3600,
    "refresh_token": "r-9e8d7c6b5a4f3e2d1c0b9a8f7e6d5c4b",
    "refresh_token_expires_in": 2592000,
    "scope": "contact:user.base:readonly contact:user.email:readonly"
  }
}
```

#### 失败响应
```json
{
  "code": 99991401,
  "msg": "refresh token expired",
  "data": {
    "error": "invalid_grant",
    "error_description": "refresh token expired"
  }
}
```

### 说明

1. **静默刷新**：使用此接口可以在用户无感知的情况下获取新的 access_token
2. **权限一致性**：刷新后的 token 保持原有的权限范围
3. **refresh_token 更新**：成功刷新后，会返回新的 refresh_token
4. **有效期**：refresh_token 的有效期为 30 天，建议在过期前主动刷新

**使用示例：**
```csharp
// C# 调用示例
var refreshRequest = new OAuthRefreshTokenRequest
{
    GrantType = "authorization_code",
    ClientId = "cli_a5ca35a685b0x26e",
    ClientSecret = "baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy",
    RefreshToken = "r-9e8d7c6b5a4f3e2d1c0b9a8f7e6d5c4b"
};

var result = await _authApi.GetOAuthenRefreshAccessTokenAsync(refreshRequest);
if (result.Code == 0)
{
    var newAccessToken = result.AccessToken;
    var newRefreshToken = result.RefreshToken;
    // 更新保存的 token
}
```

---

## 发起用户授权

### 接口名称
发起用户授权

### 飞书接口URL
```
https://accounts.feishu.cn/open-apis/authen/v1/authorize
```

### 方法
GET

### 认证
无需认证（重定向到飞书授权页面）

### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|--------|------|------|------|--------|
| client_id | string | 是 | 应用的 App ID | "cli_a5ca35a685b0x26e" |
| response_type | string | 是 | 授权类型，固定值：code | "code" |
| redirect_uri | string | 是 | 应用重定向地址 | "https://example.com/callback" |
| scope | string | 否 | 用户需要授予的权限 | "contact:user.base:readonly" |
| state | string | 否 | 状态参数，用于防止 CSRF 攻击 | "random_state_string" |
| code_challenge | string | 否 | PKCE 流程的 code_challenge | "TxYmzM4PHLBlqm5NtnCmwxMH8mFlRWl" |
| code_challenge_method | string | 否 | 生成 code_challenge 的方法 | "S256" |

**请求 URL 示例：**
```
https://accounts.feishu.cn/open-apis/authen/v1/authorize?client_id=cli_a5ca35a685b0x26e&response_type=code&redirect_uri=https://example.com/callback&scope=contact:user.base:readonly&state=random_state_string
```

### 响应

**重定向到指定地址，附带授权码：**
```
https://example.com/callback?code=a61hb967bd094dge949h79bbexd16dfe&state=random_state_string
```

**用户拒绝授权时：**
```
https://example.com/callback?error=access_denied&error_description=user+denied+the+request&state=random_state_string
```

### 说明

1. **用户交互**：此接口会将用户重定向到飞书的授权页面
2. **授权流程**：
   - 用户点击授权链接 → 飞书授权页面 → 用户确认授权 → 重定向回调地址
3. **安全性**：
   - 使用 state 参数防止 CSRF 攻击
   - 支持的 PKCE 流程增强安全性
   - 必须验证回调的 state 参数与传入的一致
4. **授权码有效期**：授权码的有效期为 5 分钟，且只能使用一次
5. **重定向地址**：需要在飞书开发者后台预先配置

**使用示例：**
```csharp
// C# 构建授权 URL
var clientId = "cli_a5ca35a685b0x26e";
var redirectUri = "https://example.com/callback";
var scope = "contact:user.base:readonly";
var state = GenerateRandomState(); // 生成随机字符串

var authorizeUrl = $"https://accounts.feishu.cn/open-apis/authen/v1/authorize" +
    $"?client_id={clientId}" +
    $"&response_type=code" +
    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
    $"&scope={scope}" +
    $"&state={state}";

// 重定向用户到授权页面
return Redirect(authorizeUrl);

// 在回调处理中验证 state 并获取授权码
[HttpGet("callback")]
public async Task<IActionResult> Callback(string code, string state)
{
    // 验证 state 参数
    if (!ValidateState(state))
    {
        return BadRequest("Invalid state");
    }
    
    // 使用授权码获取 access_token
    var oauthRequest = new OAuthTokenRequest
    {
        GrantType = "authorization_code",
        ClientId = clientId,
        ClientSecret = clientSecret,
        Code = code,
        RedirectUri = redirectUri
    };
    
    var result = await _authApi.GetOAuthenAccessTokenAsync(oauthRequest);
    // 处理授权结果...
}
```

---

## 错误码说明

### 通用错误码

| 错误码 | 错误描述 | 说明 | 解决方案 |
|--------|----------|------|----------|
| 0 | success | 请求成功 | - |
| 99991400 | app secret invalid | 应用密钥无效 | 检查 app_secret 是否正确 |
| 99991401 | app not exist | 应用不存在 | 检查 app_id 是否正确 |
| 99991402 | app disabled | 应用已禁用 | 联系飞书管理员启用应用 |
| 99991403 | no permission | 无权限使用该接口 | 检查应用是否开通对应权限 |
| 99991663 | app rate limit | 应用调用频率超限 | 降低调用频率，使用缓存 |
| 99991668 | tenant not exist | 租户不存在 | 检查租户信息是否正确 |

### OAuth 相关错误码

| 错误码 | 错误描述 | 说明 | 解决方案 |
|--------|----------|------|----------|
| 99991401 | authorization code expired | 授权码已过期 | 重新发起用户授权 |
| 99991402 | authorization code used | 授权码已使用 | 重新发起用户授权 |
| 99991403 | redirect uri mismatch | 回调地址不匹配 | 检查 redirect_uri 参数 |
| 99991404 | scope invalid | 权限范围无效 | 检查 scope 参数是否正确 |
| 99991405 | user denied | 用户拒绝授权 | 提示用户重新授权 |
| 99991406 | refresh token expired | 刷新令牌已过期 | 重新发起用户授权 |

---

## 最佳实践

### 1. Token 管理策略

```csharp
// Token 管理最佳实践示例
public class TokenManager
{
    private readonly IMemoryCache _cache;
    private readonly IFeishuV3AuthenticationApi _authApi;
    
    public async Task<string> GetTenantTokenAsync(string appId, string appSecret)
    {
        var cacheKey = $"tenant_token:{appId}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedToken))
        {
            return cachedToken!;
        }
        
        var credentials = new AppCredentials
        {
            AppId = appId,
            AppSecret = appSecret
        };
        
        var result = await _authApi.GetTenantAccessTokenAsync(credentials);
        if (result.Code == 0)
        {
            // 提前 5 分钟刷新，避免 token 过期
            var expiration = TimeSpan.FromSeconds(result.Expire - 300);
            _cache.Set(cacheKey, result.TenantAccessToken, expiration);
            
            return result.TenantAccessToken!;
        }
        
        throw new Exception($"获取 token 失败: {result.Msg}");
    }
}
```

### 2. 错误处理和重试

```csharp
// 错误处理和重试机制
public async Task<T> CallWithRetryAsync<T>(Func<Task<T>> apiCall, int maxRetries = 3)
{
    int retryCount = 0;
    
    while (true)
    {
        try
        {
            return await apiCall();
        }
        catch (Exception ex) when (IsRetryableError(ex) && retryCount < maxRetries)
        {
            retryCount++;
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // 指数退避
            await Task.Delay(delay);
        }
    }
}

private bool IsRetryableError(Exception ex)
{
    // 网络错误、限流等可重试错误
    return ex.Message.Contains("timeout") || 
           ex.Message.Contains("rate limit") ||
           ex.Message.Contains("network");
}
```

### 3. 安全性建议

1. **凭证安全**：
   - 不要在客户端代码中硬编码 app_secret
   - 使用环境变量或密钥管理服务存储敏感信息
   - 定期轮换应用密钥

2. **Token 安全**：
   - 使用 HTTPS 存储和传输 token
   - 设置适当的 token 过期时间
   - 在服务器端管理 token，避免暴露给客户端

3. **授权安全**：
   - 始终验证 state 参数
   - 使用 PKCE 流程增强安全性
   - 限制权限范围，遵循最小权限原则

---

## 版本更新记录

| 版本 | 更新日期 | 更新内容 |
|------|----------|----------|
| v1.0.0 | 2025-11-20 | 初始版本，支持基础的认证授权功能 |

---

## 相关资源

- [飞书开放平台官方文档](https://open.feishu.cn/document/)
- [OAuth 2.0 规范](https://tools.ietf.org/html/rfc6749)
- [PKCE 规范](https://tools.ietf.org/html/rfc7636)
- [Mud.Feishu GitHub 仓库](https://github.com/mudtools/MudFeishu)

---

*本文档最后更新时间：2025年11月20日*