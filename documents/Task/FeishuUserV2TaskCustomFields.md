# 任务自定义字段 V2 - 用户权限

## 接口名称
**任务自定义字段 V2 -（IFeishuUserV2TaskCustomFields）**

## 功能描述
任务功能支持在任务中扩充自定义字段，更清晰地添加任务关键信息，高效管理任务，辅助协作推进。

任务的使用者可以在使用"任务截止时间"、"任务负责人"等系统字段之外，自行定义如"优先级"、"项目发布日期"、"价格"等和使用场景密切相关的字段。

本接口提供以当前登录用户身份管理任务自定义字段的能力，与租户权限接口功能一致，但使用用户令牌进行认证。

## 参考文档
- [飞书开放平台 - 任务自定义字段概述](https://open.feishu.cn/document/task-v2/custom_field/custom-field-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCustomFieldsAsync | 创建自定义字段 | 用户令牌 | POST |
| UpdateCustomFieldsAsync | 更新自定义字段 | 用户令牌 | PATCH |
| GetCustomFieldsByIdAsync | 获取自定义字段详情 | 用户令牌 | GET |
| GetCustomFieldsPageListAsync | 列取自定义字段列表 | 用户令牌 | GET |
| AddCustomFieldsByIdAsync | 将自定义字段加入资源 | 用户令牌 | POST |
| RemoveCustomFieldsByIdAsync | 将自定义字段从资源移出 | 用户令牌 | POST |
| CreateCustomFieldsOptionsAsync | 创建自定义字段选项 | 用户令牌 | POST |
| UpdateCustomFieldsOptionsAsync | 更新自定义字段选项 | 用户令牌 | POST |

---

## 函数详细内容

### 创建自定义字段

**函数名称**：创建自定义字段

**函数签名**：
```csharp
Task<FeishuApiResult<CustomFieldsResult>?> CreateCustomFieldsAsync(
    [Body] CreateCustomFieldsRequest createCustomFieldsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| createCustomFieldsRequest | ✅ | CreateCustomFieldsRequest | 创建自定义字段请求体 |
| └ name | ✅ | string | 字段名称，最大50个字符 |
| └ resource_type | ✅ | string | 归属资源类型，支持"tasklist" |
| └ resource_id | ✅ | string | 归属资源ID（清单GUID） |
| └ type | ✅ | string | 字段类型：number、datetime、member、single_select、multi_select、text |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**字段类型说明**：

| 类型 | 说明 |
|------|------|
| number | 数字类型，可设置格式化方式 |
| datetime | 日期时间类型，可设置是否包含时间 |
| member | 成员类型，用于选择用户 |
| single_select | 单选类型，需配置选项列表 |
| multi_select | 多选类型，需配置选项列表 |
| text | 文本类型，纯文本输入 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "custom_field": {
      "guid": "5ffbe0ca-6600-41e0-a634-2b38cbcf13b8",
      "name": "优先级",
      "type": "single_select",
      "resources": [
        {
          "type": "tasklist",
          "id": "ec5ed63d-a4a9-44de-a935-7ba243471c0a"
        }
      ],
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**说明**：
- 创建一个自定义字段，并将其加入一个资源上（目前资源只支持清单）
- 创建自定义字段必须提供字段名称、类型和相应类型的设置

**代码示例**：
```csharp
// 使用用户权限创建自定义字段
public class UserCustomFieldService
{
    private readonly IFeishuUserV2TaskCustomFields _customFieldClient;

    public UserCustomFieldService(IFeishuUserV2TaskCustomFields customFieldClient)
    {
        _customFieldClient = customFieldClient;
    }

    // 创建任务状态单选字段
    public async Task CreateStatusFieldAsync(string tasklistGuid)
    {
        var request = new CreateCustomFieldsRequest
        {
            Name = "任务状态",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid,
            Type = "single_select",
            SingleSelectSetting = new SelectSettingData
            {
                Options = new[]
                {
                    new SelectOption { Name = "待启动", Color = "gray" },
                    new SelectOption { Name = "进行中", Color = "blue" },
                    new SelectOption { Name = "待审核", Color = "orange" },
                    new SelectOption { Name = "已完成", Color = "green" }
                }
            }
        };

        var result = await _customFieldClient.CreateCustomFieldsAsync(request);
        if (result?.Data?.CustomField != null)
        {
            Console.WriteLine($"自定义字段创建成功，GUID: {result.Data.CustomField.Guid}");
        }
    }
}
```

---

### 更新自定义字段

**函数名称**：更新自定义字段

**函数签名**：
```csharp
Task<FeishuApiResult<CustomFieldsResult>?> UpdateCustomFieldsAsync(
    [Path] string custom_field_guid,
    [Body] UpdateCustomFieldsRequest updateTaskSectionsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| updateTaskSectionsRequest | ✅ | UpdateCustomFieldsRequest | 更新自定义字段请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 更新一个自定义字段的名称和设定
- 自定义字段不允许修改类型，只能根据类型修改其设置

---

### 获取自定义字段详情

**函数名称**：获取自定义字段详情

**函数签名**：
```csharp
Task<FeishuApiResult<CustomFieldsResult>?> GetCustomFieldsByIdAsync(
    [Path] string custom_field_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

---

### 列取自定义字段列表

**函数名称**：列取自定义字段列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<CustomFieldInfo>?> GetCustomFieldsPageListAsync(
    [Query("resource_type")] string? resource_type = null,
    [Query("resource_id")] string? resource_id = null,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_type | ⚪ | string | 资源类型 |
| resource_id | ⚪ | string | 资源ID |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：分页列取用户可访问的自定义字段列表。

---

### 将自定义字段加入资源

**函数名称**：将自定义字段加入资源

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> AddCustomFieldsByIdAsync(
    [Path] string custom_field_guid,
    [Body] CustomFieldsToResourceRequest customFieldsToResourceRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| customFieldsToResourceRequest | ✅ | CustomFieldsToResourceRequest | 将自定义字段加入资源请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 将自定义字段加入一个资源
- 一个自定义字段可以加入多个清单中

---

### 将自定义字段从资源移出

**函数名称**：将自定义字段从资源移出

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> RemoveCustomFieldsByIdAsync(
    [Path] string custom_field_guid,
    [Body] CustomFieldsToResourceRequest customFieldsToResourceRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| customFieldsToResourceRequest | ✅ | CustomFieldsToResourceRequest | 将自定义字段移出资源请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

---

### 创建自定义字段选项

**函数名称**：创建自定义字段选项

**函数签名**：
```csharp
Task<FeishuApiResult<CustomFieldsOptionsResult>?> CreateCustomFieldsOptionsAsync(
    [Path] string custom_field_guid,
    [Body] CreateCustomFieldsOptionsRequest createCustomFieldsOptionsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| createCustomFieldsOptionsRequest | ✅ | CreateCustomFieldsOptionsRequest | 创建自定义任务选项请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 为单选或多选字段添加一个自定义选项
- 一个单选/多选字段最大支持100个选项

---

### 更新自定义字段选项

**函数名称**：更新自定义字段选项

**函数签名**：
```csharp
Task<FeishuApiResult<CustomFieldsOptionsResult>?> UpdateCustomFieldsOptionsAsync(
    [Path] string custom_field_guid,
    [Path] string option_guid,
    [Body] UpdateCustomFieldsOptionsRequest updateCustomFieldsOptionsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 要更新的选项的自定义字段GUID |
| option_guid | ✅ | string | 要更新的选项的GUID |
| updateCustomFieldsOptionsRequest | ✅ | UpdateCustomFieldsOptionsRequest | 更新自定义任务选项请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：根据一个自定义字段的GUID和其选项的GUID，更新该选项的数据。

**代码示例**：
```csharp
// 完整的个人自定义字段管理示例
public async Task ManagePersonalCustomFieldsAsync(string tasklistGuid)
{
    // 1. 创建自定义字段
    var createRequest = new CreateCustomFieldsRequest
    {
        Name = "我的标签",
        ResourceType = "tasklist",
        ResourceId = tasklistGuid,
        Type = "multi_select",
        MultiSelectSetting = new SelectSettingData
        {
            Options = new[]
            {
                new SelectOption { Name = "重要", Color = "red" },
                new SelectOption { Name = "紧急", Color = "orange" },
                new SelectOption { Name = "待定", Color = "gray" }
            }
        }
    };

    var createResult = await _customFieldClient.CreateCustomFieldsAsync(createRequest);
    if (createResult?.Data?.CustomField == null)
    {
        Console.WriteLine("自定义字段创建失败");
        return;
    }

    var fieldGuid = createResult.Data.CustomField.Guid;
    Console.WriteLine($"自定义字段创建成功: {fieldGuid}");

    // 2. 列取所有自定义字段
    var listResult = await _customFieldClient.GetCustomFieldsPageListAsync(
        resource_type: "tasklist",
        resource_id: tasklistGuid
    );

    // 3. 添加新选项
    var optionRequest = new CreateCustomFieldsOptionsRequest
    {
        Name = "已完成",
        Color = "green"
    };

    await _customFieldClient.CreateCustomFieldsOptionsAsync(fieldGuid, optionRequest);

    // 4. 获取字段详情
    var detailResult = await _customFieldClient.GetCustomFieldsByIdAsync(fieldGuid);

    // 5. 更新字段名称
    var updateRequest = new UpdateCustomFieldsRequest
    {
        Name = "个人任务标签",
        UpdateFields = new[] { "name" }
    };

    await _customFieldClient.UpdateCustomFieldsAsync(fieldGuid, updateRequest);
}
```
