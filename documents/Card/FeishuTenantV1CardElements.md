# 飞书卡片组件管理接口 - FeishuTenantV1CardElements

## 接口名称
**飞书卡片组件管理（租户令牌）** - (FeishuTenantV1CardElements)

## 功能描述
飞书卡片是应用的一种能力，包括构建卡片内容所需的组件和发送卡片所需的能力。本接口支持对卡片中的组件进行精细化管理，包括新增、更新、删除组件，以及流式更新文本内容，适用于租户应用场景。

## 参考文档
- [飞书开放平台 - 飞书卡片资源概述](https://open.feishu.cn/document/cardkit-v1/feishu-card-resource-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCardElementAsync | 新增卡片组件 | 租户令牌 | POST |
| UpdateCardElementByIdAsync | 更新卡片组件 | 租户令牌 | PUT |
| UpdateCardElementAttributeByIdAsync | 更新组件属性 | 租户令牌 | PATCH |
| StreamUpdateCardTextByIdAsync | 流式更新文本 | 租户令牌 | PUT |
| DeleteCardElementByIdAsync | 删除卡片组件 | 租户令牌 | DELETE |

---

## 函数详细内容

### CreateCardElementAsync - 新增卡片组件

为指定卡片实体新增组件，以扩展卡片内容，如在卡片中添加一个点击按钮。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> CreateCardElementAsync(
    [Path] string card_id,
    [Body] CreateCardElementRequest cardElementRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| cardElementRequest | CreateCardElementRequest | ✅ | 新增卡片组件请求体 | - |
| ├─ element | object | ✅ | 组件定义 | - |
| ├─ ├─ tag | string | ✅ | 组件类型 | "button" |
| ├─ ├─ element_id | string | ✅ | 组件唯一标识 | "btn_submit" |
| ├─ ├─ text | object | ✅ | 按钮文本 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- element_id 由开发者自定义，需保证唯一性
- 新增组件会追加到卡片内容末尾

#### 代码示例

```csharp
public class CardElementService
{
    private readonly IFeishuTenantV1CardElements _cardElementClient;

    public CardElementService(IFeishuTenantV1CardElements cardElementClient)
    {
        _cardElementClient = cardElementClient;
    }

    public async Task AddButtonToCardAsync(string cardId)
    {
        var request = new CreateCardElementRequest
        {
            Element = new
            {
                tag = "button",
                element_id = "btn_approve",
                text = new { tag = "plain_text", content = "审批通过" },
                type = "primary",
                value = new { action = "approve" }
            }
        };

        var result = await _cardElementClient.CreateCardElementAsync(cardId, request);
        if (result?.Code == 0)
        {
            Console.WriteLine("按钮组件添加成功");
        }
    }
}
```

---

### UpdateCardElementByIdAsync - 更新卡片组件

更新卡片实体中的指定组件为新组件。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> UpdateCardElementByIdAsync(
    [Path] string card_id,
    [Path] string element_id,
    [Body] UpdateCardElementRequest cardElementRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| element_id | string | ✅ | 要更新的组件 ID | "markdown_1" |
| cardElementRequest | UpdateCardElementRequest | ✅ | 更新卡片组件请求体 | - |
| ├─ element | object | ✅ | 新的组件定义 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 使用新组件完全替换原有组件
- element_id 对应卡片 JSON 中组件的 element_id 属性

#### 代码示例

```csharp
public async Task ReplaceCardElementAsync(string cardId)
{
    var request = new UpdateCardElementRequest
    {
        Element = new
        {
            tag = "markdown",
            element_id = "markdown_1",
            content = "**更新后的内容**"
        }
    };

    var result = await _cardElementClient.UpdateCardElementByIdAsync(cardId, "markdown_1", request);
    if (result?.Code == 0)
    {
        Console.WriteLine("组件更新成功");
    }
}
```

---

### UpdateCardElementAttributeByIdAsync - 更新组件属性

通过传入 card_id 和 element_id，更新卡片实体中对应组件的属性。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> UpdateCardElementAttributeByIdAsync(
    [Path] string card_id,
    [Path] string element_id,
    [Body] UpdateCardElementAttributeRequest cardElementRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| element_id | string | ✅ | 要更新的组件 ID | "markdown_1" |
| cardElementRequest | UpdateCardElementAttributeRequest | ✅ | 更新卡片组件属性请求体 | - |
| ├─ attributes | object | ✅ | 要更新的属性集合 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 只更新指定的属性，不影响组件其他属性
- 适用于修改按钮状态、文本颜色等场景

#### 代码示例

```csharp
public async Task UpdateElementAttributeAsync(string cardId)
{
    var request = new UpdateCardElementAttributeRequest
    {
        Attributes = new
        {
            content = "状态：已处理",
            text_align = "center"
        }
    };

    var result = await _cardElementClient.UpdateCardElementAttributeByIdAsync(
        cardId, 
        "status_label", 
        request
    );
    
    if (result?.Code == 0)
    {
        Console.WriteLine("组件属性更新成功");
    }
}
```

---

### StreamUpdateCardTextByIdAsync - 流式更新文本

对卡片中的普通文本元素（tag 为 plain_text）或富文本组件（tag 为 markdown）传入全量文本内容，以实现"打字机"式的文字输出效果。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> StreamUpdateCardTextByIdAsync(
    [Path] string card_id,
    [Path] string element_id,
    [Body] StreamUpdateTextRequest cardElementRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| element_id | string | ✅ | 要更新的组件 ID | "markdown_1" |
| cardElementRequest | StreamUpdateTextRequest | ✅ | 流式更新文本请求体 | - |
| ├─ content | string | ✅ | 完整的文本内容 | "这是最终显示的文本" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 仅支持 plain_text 和 markdown 类型的组件
- 适合AI对话、进度展示等需要逐字显示的场景
- 调用后飞书客户端会自动呈现打字机效果

#### 代码示例

```csharp
public async Task StreamUpdateTextAsync(string cardId)
{
    // 模拟AI逐步生成回复的场景
    var fullContent = "这是一段需要流式展示的文本内容，" +
                      "可以用于展示AI的回复过程，" +
                      "给用户更好的交互体验。";

    var request = new StreamUpdateTextRequest
    {
        Content = fullContent
    };

    var result = await _cardElementClient.StreamUpdateCardTextByIdAsync(
        cardId, 
        "ai_response", 
        request
    );
    
    if (result?.Code == 0)
    {
        Console.WriteLine("流式文本更新成功");
    }
}
```

---

### DeleteCardElementByIdAsync - 删除卡片组件

删除指定卡片实体中的组件。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> DeleteCardElementByIdAsync(
    [Path] string card_id,
    [Path] string element_id,
    [Body] DeleteCardElementRequest cardElementRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| element_id | string | ✅ | 要删除的组件 ID | "btn_temp" |
| cardElementRequest | DeleteCardElementRequest | ✅ | 删除卡片组件请求体 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 删除后组件不可恢复
- 适用于根据业务状态动态隐藏或移除组件的场景

#### 代码示例

```csharp
public async Task RemoveCardElementAsync(string cardId)
{
    var request = new DeleteCardElementRequest
    {
        Confirm = true
    };

    var result = await _cardElementClient.DeleteCardElementByIdAsync(
        cardId, 
        "btn_temp", 
        request
    );
    
    if (result?.Code == 0)
    {
        Console.WriteLine("组件删除成功");
    }
}
```
