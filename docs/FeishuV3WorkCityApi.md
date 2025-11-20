# 工作城市管理

## 接口名称：IFeishuV3WorkCityApi

## 功能描述
工作城市是用户属性之一，通过工作城市 API 仅支持查询工作城市信息。该接口提供了获取租户下所有工作城市列表以及根据ID获取特定工作城市详细信息的功能。

**接口详细文档**：[飞书开放平台文档](https://open.feishu.cn/document/contact-v3/work_city/work-city-resources-introduction)

## 函数列表

| 函数名称 | HTTP方法 | 请求路径 | 功能描述 |
|---------|---------|---------|---------|
| GetWorkCitesListAsync | GET | /open-apis/contact/v3/work_cities | 获取当前租户下所有工作城市信息 |
| GetWorkCityByIdAsync | GET | /open-apis/contact/v3/work_cities/{work_city_id} | 获取指定工作城市的详细信息 |

## 函数详细内容

### 函数名称：GetWorkCitesListAsync

**函数签名**：
```csharp
Task<FeishuApiListResult<WorkCity>> GetWorkCitesListAsync(
    [Token(TokenType.Both)][Header("Authorization")] string access_token,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default)
```

**认证**：
- **必填**：access_token（访问凭证，用于身份鉴权）
- **认证类型**：Bearer Token

**参数**：

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| access_token | string | 是 | - | 应用调用API时的访问凭证 |
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
        "work_city_id": "6729287797559891971",
        "name": "北京",
        "i18n_name": {
          "zh_cn": "北京",
          "en_us": "Beijing"
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
- 分页查询时，当还有更多数据时会返回新的 page_token 用于下一次请求
- work_city_id 是工作城市的唯一标识符
- status.is_enabled 表示该工作城市是否处于启用状态

**代码示例**：
```csharp
// 获取所有工作城市列表
var workCityApi = serviceProvider.GetService<IFeishuV3WorkCityApi>();
string accessToken = "your_access_token_here";

// 第一次请求获取工作城市列表
var result = await workCityApi.GetWorkCitesListAsync(accessToken, page_size: 50);

if (result.Success)
{
    Console.WriteLine($"获取到 {result.Data.Items.Count} 个工作城市");
    
    // 遍历所有工作城市
    foreach (var city in result.Data.Items)
    {
        Console.WriteLine($"城市ID: {city.WorkCityId}");
        Console.WriteLine($"城市名称: {city.Name}");
        Console.WriteLine($"启用状态: {city.Status.IsEnabled}");
        Console.WriteLine("---");
    }
    
    // 如果还有更多数据，继续获取
    if (result.Data.HasMore)
    {
        var nextPageResult = await workCityApi.GetWorkCitesListAsync(
            accessToken, 
            page_size: 50, 
            page_token: result.Data.PageToken
        );
        // 处理下一页数据...
    }
}
else
{
    Console.WriteLine($"获取工作城市失败: {result.ErrorMsg}");
}
```

---

### 函数名称：GetWorkCityByIdAsync

**函数签名**：
```csharp
Task<FeishuApiResult<WorkCityResult>> GetWorkCityByIdAsync(
    [Token(TokenType.Both)][Header("Authorization")] string access_token,
    [Path] string work_city_id,
    CancellationToken cancellationToken = default)
```

**认证**：
- **必填**：access_token（访问凭证，用于身份鉴权）
- **认证类型**：Bearer Token

**参数**：

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| access_token | string | 是 | - | 应用调用API时的访问凭证 |
| work_city_id | string | 是 | - | 工作城市ID（路径参数） |
| cancellationToken | CancellationToken | 否 | default | 取消操作令牌对象 |

**响应**：

**成功响应示例**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "work_city": {
      "work_city_id": "6729287797559891971",
      "name": "北京",
      "i18n_name": {
        "zh_cn": "北京",
        "en_us": "Beijing"
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
  "msg": "work city not exist",
  "data": {}
}
```

**说明**：
- work_city_id 参数需要从之前获取的工作城市列表中获得，或使用已知的城市ID
- 如果指定的工作城市ID不存在，将返回错误代码 1244006
- 返回的数据结构中包含完整的工作城市信息，包括多语言名称

**代码示例**：
```csharp
// 根据ID获取特定工作城市信息
var workCityApi = serviceProvider.GetService<IFeishuV3WorkCityApi>();
string accessToken = "your_access_token_here";
string cityId = "6729287797559891971"; // 北京的工作城市ID

try
{
    var result = await workCityApi.GetWorkCityByIdAsync(accessToken, cityId);
    
    if (result.Success)
    {
        var city = result.Data.WorkCity;
        Console.WriteLine($"城市ID: {city.WorkCityId}");
        Console.WriteLine($"中文名称: {city.I18NName.ZhCn}");
        Console.WriteLine($"英文名称: {city.I18NName.EnUs}");
        Console.WriteLine($"启用状态: {(city.Status.IsEnabled ? "启用" : "禁用")}");
    }
    else
    {
        Console.WriteLine($"获取工作城市详情失败: {result.ErrorMsg}");
        Console.WriteLine($"错误代码: {result.Code}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"调用API时发生异常: {ex.Message}");
}
```

**应用场景示例**：
```csharp
// 实际应用场景：用户管理系统中显示用户的工作城市信息
public class UserProfileService
{
    private readonly IFeishuV3WorkCityApi _workCityApi;
    private readonly string _accessToken;
    
    public UserProfileService(IFeishuV3WorkCityApi workCityApi, string accessToken)
    {
        _workCityApi = workCityApi;
        _accessToken = accessToken;
    }
    
    public async Task<string> GetWorkCityNameAsync(string cityId)
    {
        if (string.IsNullOrEmpty(cityId))
            return "未设置";
            
        var result = await _workCityApi.GetWorkCityByIdAsync(_accessToken, cityId);
        
        if (result.Success && result.Data.WorkCity != null)
        {
            // 优先显示中文名称
            return result.Data.WorkCity.I18NName?.ZhCn ?? result.Data.WorkCity.Name;
        }
        
        return "未知城市";
    }
    
    public async Task<Dictionary<string, string>> GetAllWorkCitiesAsync()
    {
        var cityDict = new Dictionary<string, string>();
        var result = await _workCityApi.GetWorkCitesListAsync(_accessToken, page_size: 100);
        
        if (result.Success)
        {
            foreach (var city in result.Data.Items)
            {
                cityDict[city.WorkCityId] = city.Name;
            }
        }
        
        return cityDict;
    }
}
```

## 版本记录

| 版本 | 日期 | 更新内容 |
|------|------|---------|
| v1.0 | 2025-11-20 | 初始版本，包含工作城市查询相关接口文档 |

