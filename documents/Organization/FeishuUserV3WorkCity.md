# 工作城市 API（用户级）

## 接口名称
**飞书工作城市 API -（IFeishuUserV3WorkCity）**

## 功能描述
工作城市是用户属性之一，通过工作城市 API 仅支持查询工作城市信息。
当前接口使用用户令牌访问，适应于用户应用场景。

## 参考文档
- [飞书官方文档 - 工作城市资源介绍](https://open.feishu.cn/document/contact-v3/work_city/work-city-resources-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetWorkCitesListAsync | 获取工作城市列表 | 用户令牌 | GET |
| GetWorkCityByIdAsync | 获取指定工作城市信息 | 用户令牌 | GET |

---

## 函数详细内容

### 获取工作城市列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<WorkCity>?> GetWorkCitesListAsync(
     [Query("page_size")] int? page_size = 10,
     [Query("page_token")] string? page_token = null,
     CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| page_size | int? | ⚪ | 分页大小，即本次请求所返回的信息列表内的最大条目数，默认值：10 |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 |

**说明**：获取当前租户下所有工作城市信息，包括工作城市的 ID、名称、多语言名称以及启用状态。

---

### 获取指定工作城市信息

**函数签名**：
```csharp
Task<FeishuApiResult<WorkCityResult>?> GetWorkCityByIdAsync(
   [Path] string work_city_id,
   CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| work_city_id | string | ✅ | 工作城市 ID |

**说明**：获取指定工作城市的信息，包括工作城市的 ID、名称、多语言名称以及启用状态。
