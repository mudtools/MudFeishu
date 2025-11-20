# IFeishuV3JobTitleApi API 文档

## 概述

`IFeishuV3JobTitleApi` 接口提供了飞书职务管理的功能。职务是用户属性之一，通过职务 API 仅支持查询职务信息，包括职务的 ID、名称、多语言名称以及启用状态。

**接口详细文档**：[飞书职务资源介绍](https://open.feishu.cn/document/contact-v3/job_title/job-title-resources-introduction)

---

## 1. 获取当前租户下的职务信息

### 接口名称
获取租户职务列表

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_titles
```

### 方法
GET

### 认证
**Tenant Access Token** (租户访问令牌)

### 参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| tenant_access_token | string | 是 | - | 应用访问凭证，用于身份鉴权 |
| page_size | int | 否 | 10 | 分页大小，本次请求返回的最大条目数 |
| page_token | string | 否 | null | 分页标记，首次请求不填 |

### 响应

#### 成功响应示例
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "job_title_id": "6983456743213456789",
        "name": "软件工程师",
        "i18n_name": [
          {
            "locale": "zh_cn",
            "text": "软件工程师"
          },
          {
            "locale": "en_us",
            "text": "Software Engineer"
          }
        ],
        "status": true
      },
      {
        "job_title_id": "6983456743213456790",
        "name": "产品经理",
        "i18n_name": [
          {
            "locale": "zh_cn",
            "text": "产品经理"
          },
          {
            "locale": "en_us",
            "text": "Product Manager"
          }
        ],
        "status": true
      }
    ],
    "page_token": "next_page_token_value",
    "has_more": true
  }
}
```

#### 错误响应示例
```json
{
  "code": 99991663,
  "msg": "token not found",
  "data": {}
}
```

### 说明
- 该接口使用租户访问令牌进行身份验证
- 支持分页查询，通过 `page_token` 实现分页遍历
- 只有启用状态的职务才能分配给用户
- 响应中的 `i18n_name` 字段支持多语言显示

---

## 2. 获取当前用户下的职务信息

### 接口名称
获取用户职务列表

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_titles
```

### 方法
GET

### 认证
**User Access Token** (用户访问令牌)

### 参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| user_access_token | string | 是 | - | 用户访问凭证，用于身份鉴权 |
| page_size | int | 否 | 10 | 分页大小，本次请求返回的最大条目数 |
| page_token | string | 否 | null | 分页标记，首次请求不填 |

### 响应

#### 成功响应示例
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "job_title_id": "6983456743213456789",
        "name": "高级工程师",
        "i18n_name": [
          {
            "locale": "zh_cn",
            "text": "高级工程师"
          },
          {
            "locale": "en_us",
            "text": "Senior Engineer"
          }
        ],
        "status": true
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

### 说明
- 该接口使用用户访问令牌进行身份验证
- 获取的是当前登录用户可访问的职务信息
- 与租户接口的区别在于认证方式和数据范围不同

---

## 3. 获取指定职务信息（租户认证）

### 接口名称
根据ID获取租户职务信息

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_titles/{job_title_id}
```

### 方法
GET

### 认证
**Tenant Access Token** (租户访问令牌)

### 参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| tenant_access_token | string | 是 | - | 应用访问凭证，用于身份鉴权 |
| job_title_id | string | 是 | - | 职务ID，路径参数 |

### 响应

#### 成功响应示例
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_title": {
      "job_title_id": "6983456743213456789",
      "name": "技术总监",
      "i18n_name": [
        {
          "locale": "zh_cn",
          "text": "技术总监"
        },
        {
          "locale": "en_us",
          "text": "CTO"
        }
      ],
      "status": true
    }
  }
}
```

#### 错误响应示例
```json
{
  "code": 2100059,
  "msg": "职务不存在",
  "data": {}
}
```

### 说明
- `job_title_id` 为路径参数，需要在URL中直接替换
- 如果职务ID不存在或已被删除，会返回相应错误码
- 响应数据被包装在 `job_title` 对象中

---

## 4. 获取指定职务信息（用户认证）

### 接口名称
根据ID获取用户职务信息

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_titles/{job_title_id}
```

### 方法
GET

### 认证
**User Access Token** (用户访问令牌)

### 参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| user_access_token | string | 是 | - | 用户访问凭证，用于身份鉴权 |
| job_title_id | string | 是 | - | 职务ID，路径参数 |

### 响应

#### 成功响应示例
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_title": {
      "job_title_id": "6983456743213456789",
      "name": "前端开发工程师",
      "i18n_name": [
        {
          "locale": "zh_cn",
          "text": "前端开发工程师"
        },
        {
          "locale": "en_us",
          "text": "Frontend Developer"
        }
      ],
      "status": true
    }
  }
}
```

### 说明
- 使用用户访问令牌进行身份验证
- 只能获取当前用户有权限访问的职务信息
- 与租户接口的主要区别在于认证方式和权限范围

---

## 数据模型

### JobTitle（职务信息模型）

| 属性 | 类型 | 说明 |
|------|------|------|
| job_title_id | string | 职务的唯一标识符 |
| name | string | 职务的显示名称（主要语言） |
| i18n_name | List&lt;I18nContent&gt; | 职务的多语言名称列表 |
| status | boolean | 职务启用状态，true=启用，false=禁用 |

### JobTitleResult（职务结果包装类）

| 属性 | 类型 | 说明 |
|------|------|------|
| job_title | JobTitle | 职务信息对象 |

---

## 常见错误码

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 99991663 | token not found | 检查访问令牌是否正确或已过期 |
| 2100059 | 职务不存在 | 确认职务ID是否正确，职务是否已被删除 |
| 99991400 | 参数错误 | 检查请求参数格式和必填参数 |
| 99991668 | 无权限访问 | 检查应用权限配置和访问令牌权限范围 |

---

## 最佳实践

### 1. 分页查询优化
```csharp
// 推荐的分页查询方式
var pageSize = 50;
var pageToken = "";
do {
    var response = await feishuApi.GetTenantJobTitlesListAsync(
        tenant_access_token, 
        page_size: pageSize, 
        page_token: string.IsNullOrEmpty(pageToken) ? null : pageToken
    );
    
    // 处理返回的职务数据
    foreach (var jobTitle in response.Data.Items) {
        // 业务逻辑处理
    }
    
    pageToken = response.Data.PageToken;
} while (!string.IsNullOrEmpty(pageToken) && response.Data.HasMore);
```

### 2. 多语言处理
```csharp
// 智能多语言显示
string GetDisplayName(JobTitle jobTitle, string preferredLocale = "zh_cn") {
    var localized = jobTitle.I18nName.FirstOrDefault(x => x.Locale == preferredLocale);
    return localized?.Text ?? jobTitle.Name ?? "Unknown";
}
```

### 3. 错误处理
```csharp
try {
    var result = await feishuApi.GetTenantJobTitleByIdAsync(token, jobTitleId);
    if (result.Code == 0) {
        // 处理成功响应
        return result.Data.JobTitle;
    } else {
        // 处理业务错误
        throw new FeishuApiException(result.Code, result.Msg);
    }
} catch (HttpRequestException ex) {
    // 处理网络异常
    throw new FeishuNetworkException("网络请求失败", ex);
}
```

### 4. 缓存策略
- 职务信息变化频率较低，建议实现客户端缓存
- 缓存时间建议设置为 1-6 小时
- 在职务信息更新时主动清除缓存

---

## 更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0.0 | 2025-11-20 | 初始版本，包含职务查询相关接口文档 |

---

## 相关文档

- [飞书职务 API 官方文档](https://open.feishu.cn/document/contact-v3/job_title/job-title-resources-introduction)
- [认证和权限管理文档](../Authentication-API-Documentation.md)
- [员工管理 API 文档](../IFeishuV1EmployeesApi.md)
- [部门管理 API 文档](../IFeishuV3DepartmentsApi.md)