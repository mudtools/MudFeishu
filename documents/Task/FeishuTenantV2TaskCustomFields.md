# 任务自定义字段 V2 - 租户令牌

## 接口名称
**任务自定义字段 V2 -（IFeishuTenantV2TaskCustomFields）**

## 功能描述
任务功能支持在任务中扩充自定义字段，更清晰地添加任务关键信息，高效管理任务，辅助协作推进。

任务的使用者可以在使用"任务截止时间"、"任务负责人"等系统字段之外，自行定义如"优先级"、"项目发布日期"、"价格"等和使用场景密切相关的字段。

本接口提供以租户身份管理任务自定义字段的能力，支持多种字段类型（数字、日期、成员、单选、多选、文本）。

## 参考文档
- [飞书开放平台 - 任务自定义字段概述](https://open.feishu.cn/document/task-v2/custom_field/custom-field-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCustomFieldsAsync | 创建自定义字段 | 租户令牌 | POST |
| UpdateCustomFieldsAsync | 更新自定义字段 | 租户令牌 | PATCH |
| GetCustomFieldsByIdAsync | 获取自定义字段详情 | 租户令牌 | GET |
| GetCustomFieldsPageListAsync | 列取自定义字段列表 | 租户令牌 | GET |
| AddCustomFieldsByIdAsync | 将自定义字段加入资源 | 租户令牌 | POST |
| RemoveCustomFieldsByIdAsync | 将自定义字段从资源移出 | 租户令牌 | POST |
| CreateCustomFieldsOptionsAsync | 创建自定义字段选项 | 租户令牌 | POST |
| UpdateCustomFieldsOptionsAsync | 更新自定义字段选项 | 租户令牌 | POST |

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| createCustomFieldsRequest | ✅ | CreateCustomFieldsRequest | 创建自定义字段请求体 |
| └ name | ✅ | string | 字段名称，最大50个字符 |
| └ resource_type | ✅ | string | 归属资源类型，支持"tasklist" |
| └ resource_id | ✅ | string | 归属资源ID（清单GUID） |
| └ type | ✅ | string | 字段类型：number、datetime、member、single_select、multi_select、text |
| └ number_setting | ⚪ | NumberSettingData | 数字类型的字段设置 |
| └ member_setting | ⚪ | MemberSettingData | 人员类型的字段设置 |
| └ datetime_setting | ⚪ | DatetimeSettingData | 时间日期类型的字段设置 |
| └ single_select_setting | ⚪ | SelectSettingData | 单选设置 |
| └ multi_select_setting | ⚪ | SelectSettingData | 多选设置 |
| └ text_setting | ⚪ | object | 文本类型设置 |
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
      "single_select_setting": {
        "options": [
          {
            "guid": "option-guid-1",
            "name": "P0-紧急",
            "color": "red"
          },
          {
            "guid": "option-guid-2",
            "name": "P1-高",
            "color": "orange"
          }
        ]
      },
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
// 使用租户令牌创建自定义字段
public class TaskCustomFieldService
{
    private readonly IFeishuTenantV2TaskCustomFields _customFieldClient;

    public TaskCustomFieldService(IFeishuTenantV2TaskCustomFields customFieldClient)
    {
        _customFieldClient = customFieldClient;
    }

    // 创建优先级单选字段
    public async Task CreatePriorityFieldAsync(string tasklistGuid)
    {
        var request = new CreateCustomFieldsRequest
        {
            Name = "优先级",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid,
            Type = "single_select",
            SingleSelectSetting = new SelectSettingData
            {
                Options = new[]
                {
                    new SelectOption { Name = "P0-紧急", Color = "red" },
                    new SelectOption { Name = "P1-高", Color = "orange" },
                    new SelectOption { Name = "P2-中", Color = "yellow" },
                    new SelectOption { Name = "P3-低", Color = "blue" }
                }
            }
        };

        var result = await _customFieldClient.CreateCustomFieldsAsync(request);
        if (result?.Data?.CustomField != null)
        {
            Console.WriteLine($"自定义字段创建成功，GUID: {result.Data.CustomField.Guid}");
        }
    }

    // 创建数字类型的预算字段
    public async Task CreateBudgetFieldAsync(string tasklistGuid)
    {
        var request = new CreateCustomFieldsRequest
        {
            Name = "预算金额",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid,
            Type = "number",
            NumberSetting = new NumberSettingData
            {
                Formatter = "0.00"  // 保留两位小数
            }
        };

        var result = await _customFieldClient.CreateCustomFieldsAsync(request);
        if (result?.Data?.CustomField != null)
        {
            Console.WriteLine($"预算字段创建成功");
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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID，示例：5ffbe0ca-6600-41e0-a634-2b38cbcf13b8 |
| updateTaskSectionsRequest | ✅ | UpdateCustomFieldsRequest | 更新自定义字段请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 更新一个自定义字段的名称和设定
- 更新时，将update_fields字段中填写所有要修改的任务字段名，同时在custom_field字段中填写要修改的字段的新值
- 自定义字段不允许修改类型，只能根据类型修改其设置

**代码示例**：
```csharp
// 更新自定义字段
public async Task UpdateCustomFieldAsync(string customFieldGuid)
{
    var request = new UpdateCustomFieldsRequest
    {
        Name = "任务优先级",
        UpdateFields = new[] { "name" }
    };

    var result = await _customFieldClient.UpdateCustomFieldsAsync(customFieldGuid, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("自定义字段更新成功");
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：更新一个自定义字段的名称和设定。

**代码示例**：
```csharp
// 获取自定义字段详情
public async Task GetCustomFieldDetailAsync(string customFieldGuid)
{
    var result = await _customFieldClient.GetCustomFieldsByIdAsync(customFieldGuid);
    if (result?.Data?.CustomField != null)
    {
        var field = result.Data.CustomField;
        Console.WriteLine($"字段名称: {field.Name}");
        Console.WriteLine($"字段类型: {field.Type}");
        Console.WriteLine($"关联资源数: {field.Resources?.Length ?? 0}");
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_type | ⚪ | string | 资源类型，如提供表示仅查询特定资源下的自定义字段，目前只支持tasklist |
| resource_id | ⚪ | string | 要查询自定义字段的归属resource_id |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：分页列取用户可访问的自定义字段列表。如果不提供resource_type和resource_id参数，则返回用户可访问的所有自定义字段。

**代码示例**：
```csharp
// 列取清单的自定义字段
public async Task ListTasklistCustomFieldsAsync(string tasklistGuid)
{
    var result = await _customFieldClient.GetCustomFieldsPageListAsync(
        resource_type: "tasklist",
        resource_id: tasklistGuid,
        page_size: 50
    );

    if (result?.Data?.Items != null)
    {
        foreach (var field in result.Data.Items)
        {
            Console.WriteLine($"字段: {field.Name} ({field.Type})");
        }
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| customFieldsToResourceRequest | ✅ | CustomFieldsToResourceRequest | 将自定义字段加入资源请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 将自定义字段加入一个资源。目前资源类型支持清单tasklist
- 一个自定义字段可以加入多个清单中
- 加入后，该清单可以展示任务的该字段的值，同时基于该字段实现筛选、分组等功能

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| customFieldsToResourceRequest | ✅ | CustomFieldsToResourceRequest | 将自定义字段移出资源请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 将自定义字段从资源中移出。移除后，该资源将无法再使用该字段
- 目前资源的类型支持"tasklist"
- 如果要移除自定义字段本来就不存在于资源，本接口将正常返回

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 自定义字段GUID |
| createCustomFieldsOptionsRequest | ✅ | CreateCustomFieldsOptionsRequest | 创建自定义任务选项请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 为单选或多选字段添加一个自定义选项
- 一个单选/多选字段最大支持100个选项
- 新添加的选项如果不隐藏，其名字不能和已存在的不隐藏选项的名字重复

**代码示例**：
```csharp
// 为单选字段添加选项
public async Task AddOptionToFieldAsync(string customFieldGuid)
{
    var request = new CreateCustomFieldsOptionsRequest
    {
        Name = "P4-极低",
        Color = "gray"
    };

    var result = await _customFieldClient.CreateCustomFieldsOptionsAsync(
        customFieldGuid, 
        request
    );

    if (result?.Code == 0)
    {
        Console.WriteLine("选项添加成功");
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| custom_field_guid | ✅ | string | 要更新的选项的自定义字段GUID |
| option_guid | ✅ | string | 要更新的选项的GUID，示例：d4f1e8b3-5f4e-4c3a-8f7b-2e2f4e5c6d7e |
| updateCustomFieldsOptionsRequest | ✅ | UpdateCustomFieldsOptionsRequest | 更新自定义任务选项请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：根据一个自定义字段的GUID和其选项的GUID，更新该选项的数据。要更新的字段必须是单选或者多选类型，且要更新的选项必须归属于该字段。
