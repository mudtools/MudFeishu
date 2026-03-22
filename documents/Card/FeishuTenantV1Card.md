# 飞书卡片管理接口 - FeishuTenantV1Card

## 接口名称
**飞书卡片管理（租户权限）** - (FeishuTenantV1Card)

## 功能描述
飞书卡片是应用的一种能力，包括构建卡片内容所需的组件和发送卡片所需的能力，并提供了可视化搭建工具。本接口支持创建卡片实体、更新卡片配置、局部更新卡片内容以及全量更新卡片，适用于租户应用场景。

## 参考文档
- [飞书开放平台 - 飞书卡片资源概述](https://open.feishu.cn/document/cardkit-v1/feishu-card-resource-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCardAsync | 创建卡片实体 | 租户令牌 | POST |
| UpdateCardSettingsByIdAsync | 更新卡片配置 | 租户令牌 | PATCH |
| PartialUpdateCardByIdAsync | 局部更新卡片 | 租户令牌 | POST |
| UpdateCardByIdAsync | 全量更新卡片 | 租户令牌 | PATCH |

---

## 函数详细内容

### CreateCardAsync - 创建卡片实体

基于卡片 JSON 代码或卡片搭建工具搭建的卡片，创建卡片实体。用于后续通过卡片实体 ID（card_id）发送卡片、更新卡片等。

#### 函数签名

```csharp
Task<FeishuApiResult<CreateCardResult>?> CreateCardAsync(
    [Body] CreateCardRequest createCardRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| createCardRequest | CreateCardRequest | ✅ | 创建卡片实体请求体 | - |
| ├─ card_content | object | ✅ | 卡片 JSON 内容 | - |
| ├─ config | object | ⚪ | 卡片配置 | - |
| ├─ card_link | object | ⚪ | 卡片跳转链接 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "card_id": "7355372766134157313",
    "create_time": 1705312800
  }
}
```

#### 说明
- 创建的卡片实体可用于后续发送和更新操作
- card_id 是后续操作卡片的唯一标识

#### 代码示例

```csharp
public class CardService
{
    private readonly IFeishuTenantV1Card _cardClient;

    public CardService(IFeishuTenantV1Card cardClient)
    {
        _cardClient = cardClient;
    }

    public async Task<string> CreateSampleCardAsync()
    {
        var request = new CreateCardRequest
        {
            CardContent = new
            {
                elements = new[]
                {
                    new { tag = "div", text = new { tag = "plain_text", content = "欢迎使用飞书卡片" } }
                }
            },
            Config = new { wide_screen_mode = true }
        };

        var result = await _cardClient.CreateCardAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"卡片创建成功，ID: {result.Data?.CardId}");
            return result.Data?.CardId;
        }
        return string.Empty;
    }
}
```

---

### UpdateCardSettingsByIdAsync - 更新卡片配置

更新指定卡片实体的配置，支持卡片配置 config 字段和卡片跳转链接 card_link 字段。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> UpdateCardSettingsByIdAsync(
    [Path] string card_id,
    [Body] UpdateCardSettingsRequest updateCardRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| updateCardRequest | UpdateCardSettingsRequest | ✅ | 更新卡片实体配置请求体 | - |
| ├─ config | object | ⚪ | 卡片配置 | - |
| ├─ card_link | object | ⚪ | 卡片跳转链接 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 仅更新卡片的配置信息，不影响卡片内容
- 可用于修改卡片链接、样式配置等

#### 代码示例

```csharp
public async Task UpdateCardConfigAsync(string cardId)
{
    var request = new UpdateCardSettingsRequest
    {
        Config = new { wide_screen_mode = true, enable_forward = true },
        CardLink = new { url = "https://example.com/detail" }
    };

    var result = await _cardClient.UpdateCardSettingsByIdAsync(cardId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("卡片配置更新成功");
    }
}
```

---

### PartialUpdateCardByIdAsync - 局部更新卡片

更新卡片实体局部内容，包括配置和组件。支持同时对多个组件进行增删改等不同操作。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> PartialUpdateCardByIdAsync(
    [Path] string card_id,
    [Body] PartialUpdateCardRequest partialUpdateCardRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| partialUpdateCardRequest | PartialUpdateCardRequest | ✅ | 局部更新卡片实体配置请求体 | - |
| ├─ elements | object[] | ✅ | 更新的组件列表 | - |
| ├─ ├─ element_id | string | ✅ | 组件 ID | "div_1" |
| ├─ ├─ tag | string | ✅ | 组件类型 | "div" |
| ├─ ├─ operate_type | string | ✅ | 操作类型：add/update/delete | "update" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 支持批量操作多个组件
- 操作类型包括：add（新增）、update（更新）、delete（删除）
- 适用于需要动态更新部分卡片内容的场景

#### 代码示例

```csharp
public async Task PartialUpdateCardAsync(string cardId)
{
    var request = new PartialUpdateCardRequest
    {
        Elements = new[]
        {
            new CardElementOperation
            {
                ElementId = "status_text",
                Tag = "plain_text",
                OperateType = "update",
                Content = "状态已更新：处理中"
            }
        }
    };

    var result = await _cardClient.PartialUpdateCardByIdAsync(cardId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("卡片局部更新成功");
    }
}
```

---

### UpdateCardByIdAsync - 全量更新卡片

传入新的卡片 JSON 代码，覆盖更新指定的卡片实体的所有内容。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> UpdateCardByIdAsync(
    [Path] string card_id,
    [Body] UpdateCardRequest updateCardRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| updateCardRequest | UpdateCardRequest | ✅ | 全量更新卡片实体请求体 | - |
| ├─ card_content | object | ✅ | 新的卡片 JSON 内容 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 会完全覆盖原卡片内容
- 适用于需要整体替换卡片内容的场景

#### 代码示例

```csharp
public async Task FullUpdateCardAsync(string cardId)
{
    var request = new UpdateCardRequest
    {
        CardContent = new
        {
            header = new { title = new { tag = "plain_text", content = "更新后的标题" } },
            elements = new[]
            {
                new { tag = "div", text = new { tag = "plain_text", content = "全新的卡片内容" } }
            }
        }
    };

    var result = await _cardClient.UpdateCardByIdAsync(cardId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("卡片全量更新成功");
    }
}
```
