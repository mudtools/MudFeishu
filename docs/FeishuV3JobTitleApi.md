# 职务管理

## 接口名称：IFeishuV3JobTitleApi

## 功能描述
职务是用户属性之一，通过职务 API 仅支持查询职务信息。该接口提供了获取租户下职务信息、用户职务信息以及根据ID获取特定职务详细信息的功能。支持使用租户访问令牌和用户访问令牌两种认证方式。

**接口详细文档**：[飞书开放平台文档](https://open.feishu.cn/document/contact-v3/job_title/job-title-resources-introduction)

## 函数列表

| 函数名称 | HTTP方法 | 请求路径 | 认证方式 | 功能描述 |
|---------|---------|---------|---------|---------|
| GetTenantJobTitlesListAsync | GET | /open-apis/contact/v3/job_titles | 租户访问令牌 | 获取当前租户下的职务信息 |
| GetUserJobTitlesListAsync | GET | /open-apis/contact/v3/job_titles | 用户访问令牌 | 获取当前登录用户下的职务信息 |
| GetTenantJobTitleByIdAsync | GET | /open-apis/contact/v3/job_titles/{job_title_id} | 租户访问令牌 | 获取指定职务的信息（租户视角） |
| GetUserJobTitleByIdAsync | GET | /open-apis/contact/v3/job_titles/{job_title_id} | 用户访问令牌 | 获取指定职务的信息（用户视角） |

## 函数详细内容

### 函数名称：GetTenantJobTitlesListAsync

**函数签名**：
```csharp
Task<FeishuApiListResult<JobTitle>> GetTenantJobTitlesListAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default)
```

**认证**：
- **必填**：tenant_access_token（租户访问凭证，用于身份鉴权）
- **认证类型**：Bearer Token（租户级访问权限）

**参数**：

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| tenant_access_token | string | 是 | - | 租户访问凭证，具有租户级权限 |
| page_size | int | 否 | 10 | 分页大小，本次请求返回的最大条目数 |
| page_token | string | 否 | null | 分页标记，第一次请求不填，表示从头开始遍历 |
| cancellationToken | CancellationToken | 否 | default | 取消操作令牌对象 |

**响应**：

**成功响应示例**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "job_title_id": "6729287797559891971",
        "name": "软件工程师",
        "i18n_name": {
          "zh_cn": "软件工程师",
          "en_us": "Software Engineer"
        },
        "status": {
          "is_enabled": true
        }
      }
    ],
    "page_token": "next_page_token_value",
    "has_more": true
  }
}
```

**错误响应示例**：
```json
{
  "code": 99991663,
  "msg": "authentication failed",
  "data": {}
}
```

**说明**：
- 使用租户访问令牌，可以获取整个租户下的职务信息
- 分页查询时，当还有更多数据时会返回新的 page_token 用于下一次请求
- job_title_id 是职务的唯一标识符
- 适用于需要管理或查看整个组织职务结构的应用场景

**代码示例**：
```csharp
// 获取租户下所有职务列表
var jobTitleApi = serviceProvider.GetService<IFeishuV3JobTitleApi>();
string tenantAccessToken = "your_tenant_access_token_here";

// 第一次请求获取职务列表
var result = await jobTitleApi.GetTenantJobTitlesListAsync(tenantAccessToken, page_size: 50);

if (result.Success)
{
    Console.WriteLine($"获取到 {result.Data.Items.Count} 个职务");
    
    // 遍历所有职务
    foreach (var jobTitle in result.Data.Items)
    {
        Console.WriteLine($"职务ID: {jobTitle.JobTitleId}");
        Console.WriteLine($"职务名称: {jobTitle.Name}");
        Console.WriteLine($"中文名称: {jobTitle.I18NName.ZhCn}");
        Console.WriteLine($"英文名称: {jobTitle.I18NName.EnUs}");
        Console.WriteLine($"启用状态: {jobTitle.Status.IsEnabled}");
        Console.WriteLine("---");
    }
    
    // 如果还有更多数据，继续获取
    if (result.Data.HasMore)
    {
        var nextPageResult = await jobTitleApi.GetTenantJobTitlesListAsync(
            tenantAccessToken, 
            page_size: 50, 
            page_token: result.Data.PageToken
        );
        // 处理下一页数据...
    }
}
else
{
    Console.WriteLine($"获取职务列表失败: {result.ErrorMsg}");
}
```

---

### 函数名称：GetUserJobTitlesListAsync

**函数签名**：
```csharp
Task<FeishuApiListResult<JobTitle>> GetUserJobTitlesListAsync(
    [Token(TokenType.UserAccessToken)][Header("Authorization")] string user_access_token,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default)
```

**认证**：
- **必填**：user_access_token（用户访问凭证，用于身份鉴权）
- **认证类型**：Bearer Token（用户级访问权限）

**参数**：

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| user_access_token | string | 是 | - | 用户访问凭证，基于用户授权 |
| page_size | int | 否 | 10 | 分页大小，本次请求返回的最大条目数 |
| page_token | string | 否 | null | 分页标记，第一次请求不填，表示从头开始遍历 |
| cancellationToken | CancellationToken | 否 | default | 取消操作令牌对象 |

**响应**：

**成功响应示例**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "job_title_id": "6729287797559891971",
        "name": "高级软件工程师",
        "i18n_name": {
          "zh_cn": "高级软件工程师",
          "en_us": "Senior Software Engineer"
        },
        "status": {
          "is_enabled": true
        }
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

**错误响应示例**：
```json
{
  "code": 99991400,
  "msg": "user access token invalid",
  "data": {}
}
```

**说明**：
- 使用用户访问令牌，只能获取当前登录用户可见的职务信息
- 返回的职务信息可能受到用户权限和可见性设置的限制
- 适用于个人用户查看可选职务的场景，如个人资料设置

**代码示例**：
```csharp
// 获取当前用户可见的职务列表（个人资料设置场景）
var jobTitleApi = serviceProvider.GetService<IFeishuV3JobTitleApi>();
string userAccessToken = "user_access_token_from_oauth";

try
{
    var result = await jobTitleApi.GetUserJobTitlesListAsync(userAccessToken, page_size: 100);
    
    if (result.Success)
    {
        Console.WriteLine("您可以选择的职务列表：");
        
        // 为用户创建职务选择列表
        var jobTitleOptions = new List<(string id, string name)>();
        
        foreach (var jobTitle in result.Data.Items)
        {
            if (jobTitle.Status.IsEnabled)
            {
                string displayName = jobTitle.I18NName?.ZhCn ?? jobTitle.Name;
                jobTitleOptions.Add((jobTitle.JobTitleId, displayName));
                Console.WriteLine($"{jobTitleOptions.Count}. {displayName}");
            }
        }
        
        // 用户可以从中选择职务更新个人资料
        Console.WriteLine($"共找到 {jobTitleOptions.Count} 个可选职务");
    }
    else
    {
        Console.WriteLine($"获取您的职务列表失败: {result.ErrorMsg}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"获取职务信息时发生异常: {ex.Message}");
}
```

---

### 函数名称：GetTenantJobTitleByIdAsync

**函数签名**：
```csharp
Task<FeishuApiResult<JobTitleResult>> GetTenantJobTitleByIdAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Path] string job_title_id,
    CancellationToken cancellationToken = default)
```

**认证**：
- **必填**：tenant_access_token（租户访问凭证，用于身份鉴权）
- **认证类型**：Bearer Token（租户级访问权限）

**参数**：

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| tenant_access_token | string | 是 | - | 租户访问凭证，具有租户级权限 |
| job_title_id | string | 是 | - | 职务ID（路径参数） |
| cancellationToken | CancellationToken | 否 | default | 取消操作令牌对象 |

**响应**：

**成功响应示例**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_title": {
      "job_title_id": "6729287797559891971",
      "name": "产品经理",
      "i18n_name": {
        "zh_cn": "产品经理",
        "en_us": "Product Manager"
      },
      "status": {
        "is_enabled": true
      }
    }
  }
}
```

**错误响应示例**：
```json
{
  "code": 1244006,
  "msg": "job title not exist",
  "data": {}
}
```

**说明**：
- job_title_id 参数需要从之前获取的职务列表中获得，或使用已知的职务ID
- 如果指定的职务ID不存在，将返回错误代码 1244006
- 返回的数据结构中包含完整的职务信息，包括多语言名称
- 使用租户访问令牌可以获取租户内任何职务的详细信息

**代码示例**：
```csharp
// 根据ID获取特定职务详细信息（组织管理场景）
var jobTitleApi = serviceProvider.GetService<IFeishuV3JobTitleApi>();
string tenantAccessToken = "your_tenant_access_token_here";
string jobTitleId = "6729287797559891971"; // 产品经理的职务ID

try
{
    var result = await jobTitleApi.GetTenantJobTitleByIdAsync(tenantAccessToken, jobTitleId);
    
    if (result.Success && result.Data.JobTitle != null)
    {
        var jobTitle = result.Data.JobTitle;
        Console.WriteLine("职务详细信息：");
        Console.WriteLine($"职务ID: {jobTitle.JobTitleId}");
        Console.WriteLine($"职务名称: {jobTitle.Name}");
        Console.WriteLine($"中文名称: {jobTitle.I18NName?.ZhCn}");
        Console.WriteLine($"英文名称: {jobTitle.I18NName?.EnUs}");
        Console.WriteLine($"启用状态: {(jobTitle.Status.IsEnabled ? "启用" : "禁用")}");
        
        // 在组织管理中使用该信息
        if (jobTitle.Status.IsEnabled)
        {
            Console.WriteLine("该职务当前启用，可以分配给员工");
        }
        else
        {
            Console.WriteLine("该职务已禁用，需要先启用才能分配");
        }
    }
    else
    {
        Console.WriteLine($"获取职务详情失败: {result.ErrorMsg}");
        Console.WriteLine($"错误代码: {result.Code}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"调用API时发生异常: {ex.Message}");
}
```

---

### 函数名称：GetUserJobTitleByIdAsync

**函数签名**：
```csharp
Task<FeishuApiResult<JobTitleResult>> GetUserJobTitleByIdAsync(
    [Token(TokenType.UserAccessToken)][Header("Authorization")] string user_access_token,
    [Path] string job_title_id,
    CancellationToken cancellationToken = default)
```

**认证**：
- **必填**：user_access_token（用户访问凭证，用于身份鉴权）
- **认证类型**：Bearer Token（用户级访问权限）

**参数**：

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| user_access_token | string | 是 | - | 用户访问凭证，基于用户授权 |
| job_title_id | string | 是 | - | 职务ID（路径参数） |
| cancellationToken | CancellationToken | 否 | default | 取消操作令牌对象 |

**响应**：

**成功响应示例**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_title": {
      "job_title_id": "6729287797559891971",
      "name": "UI设计师",
      "i18n_name": {
        "zh_cn": "UI设计师",
        "en_us": "UI Designer"
      },
      "status": {
        "is_enabled": true
      }
    }
  }
}
```

**错误响应示例**：
```json
{
  "code": 1244006,
  "msg": "job title not exist or no permission",
  "data": {}
}
```

**说明**：
- 使用用户访问令牌只能获取当前用户有权限查看的职务信息
- 如果职务不存在或用户没有权限访问，将返回相应的错误
- 适用于用户验证自己选择的职务信息或查看特定职务详情的场景

**代码示例**：
```csharp
// 用户验证所选职务信息的场景
public class UserProfileService
{
    private readonly IFeishuV3JobTitleApi _jobTitleApi;
    private readonly string _userAccessToken;
    
    public UserProfileService(IFeishuV3JobTitleApi jobTitleApi, string userAccessToken)
    {
        _jobTitleApi = jobTitleApi;
        _userAccessToken = userAccessToken;
    }
    
    public async Task<bool> ValidateJobTitleSelectionAsync(string jobTitleId)
    {
        if (string.IsNullOrEmpty(jobTitleId))
            return false;
            
        try
        {
            var result = await _jobTitleApi.GetUserJobTitleByIdAsync(_userAccessToken, jobTitleId);
            
            if (result.Success && result.Data.JobTitle != null)
            {
                var jobTitle = result.Data.JobTitle;
                Console.WriteLine($"验证职务: {jobTitle.I18NName?.ZhCn ?? jobTitle.Name}");
                
                // 检查职务是否启用
                return jobTitle.Status.IsEnabled;
            }
            else
            {
                Console.WriteLine($"职务验证失败: {result.ErrorMsg}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"验证职务时发生异常: {ex.Message}");
            return false;
        }
    }
    
    public async Task<string> GetJobTitleDisplayNameAsync(string jobTitleId)
    {
        if (string.IsNullOrEmpty(jobTitleId))
            return "未设置";
            
        var result = await _jobTitleApi.GetUserJobTitleByIdAsync(_userAccessToken, jobTitleId);
        
        if (result.Success && result.Data.JobTitle != null)
        {
            // 优先显示中文名称
            return result.Data.JobTitle.I18NName?.ZhCn ?? result.Data.JobTitle.Name;
        }
        
        return "未知职务";
    }
}

// 使用示例
var userProfileService = new UserProfileService(jobTitleApi, userAccessToken);
string selectedJobTitleId = "6729287797559891971";

// 验证用户选择的职务
bool isValid = await userProfileService.ValidateJobTitleSelectionAsync(selectedJobTitleId);
if (isValid)
{
    string displayName = await userProfileService.GetJobTitleDisplayNameAsync(selectedJobTitleId);
    Console.WriteLine($"您选择的职务是: {displayName}");
}
else
{
    Console.WriteLine("所选职务无效或不可用");
}
```

## 应用场景对比

### 租户级API vs 用户级API

| 场景 | 推荐API | 说明 |
|------|---------|------|
| 组织管理后台 | GetTenantJobTitlesListAsync | 需要查看和管理整个组织的职务结构 |
| 员工信息管理 | GetTenantJobTitleByIdAsync | 管理员查看员工职务详情 |
| 个人资料设置 | GetUserJobTitlesListAsync | 用户选择自己的职务 |
| 职务验证 | GetUserJobTitleByIdAsync | 验证用户选择的职务有效性 |

## 版本记录

| 版本 | 日期 | 更新内容 |
|------|------|---------|
| v1.0 | 2025-11-20 | 初始版本，包含职务查询相关接口文档 |

## 注意事项

1. **权限差异**：租户级API可以访问整个租户的职务信息，用户级API只能访问当前用户可见的职务
2. **认证选择**：
   - 管理应用使用 `tenant_access_token`
   - 用户个人应用使用 `user_access_token`
3. **频率限制**：请遵循飞书API的调用频率限制，避免过于频繁的请求
4. **错误处理**：建议在生产环境中对API调用进行完整的错误处理和重试机制
5. **Token管理**：访问令牌具有有效期，需要定期刷新或使用TokenManager进行管理
6. **数据缓存**：职务信息相对稳定，建议在应用启动时缓存以提高性能
7. **多语言支持**：返回的职务信息包含多语言名称，建议根据用户语言偏好显示相应名称