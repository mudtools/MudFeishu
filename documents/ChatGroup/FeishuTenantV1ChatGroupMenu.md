# 飞书群菜单 API（租户级）

## 接口名称
**飞书群菜单 API -（IFeishuTenantV1ChatGroupMenu）**

## 功能描述
在飞书群组内设置自定义菜单，方便群成员快速访问特定链接或者执行特定操作。
群菜单分为一级菜单和二级菜单，通过 OpenAPI 你可以添加、删除、修改或者查询群菜单。
当前接口使用租户令牌访问，适应于租户应用场景。

## 参考文档
- [飞书官方文档 - 群菜单](https://open.feishu.cn/document/server-docs/group/chat-menu_tree/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| AddMenuByIdAsync | 添加群菜单 | 租户令牌 | POST |
| UpdateMenuByIdAsync | 更新群菜单 | 租户令牌 | PATCH |
| DeleteMenuByIdAsync | 删除群菜单 | 租户令牌 | DELETE |
| SortMenuByIdAsync | 排序群菜单 | 租户令牌 | POST |
| GetMenuByIdAsync | 获取群菜单 | 租户令牌 | GET |

---

## 函数详细内容

### 添加群菜单

**函数名称**：添加群菜单

**函数签名**：
```csharp
Task<FeishuApiResult<ChatGroupMenuResult>?> AddMenuByIdAsync(
    [Path] string chat_id,
    [Body] AddChatGroupMenuRequest addChatGroupMenuRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID，示例："oc_a0553eda9014c201e6969b478895c230" |
| addChatGroupMenuRequest | AddChatGroupMenuRequest | ✅ | 添加群菜单请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "menu_tree": [
      {
        "menu_item_id": "menu_001",
        "name": "工作台",
        "icon": "https://example.com/icon.png",
        "url": "https://workbench.example.com"
      }
    ]
  }
}
```

**说明**：在指定群组中添加一个或多个群菜单。成功调用后接口会返回当前群组内所有群菜单信息。

**代码示例**：
```csharp
public class ChatMenuService
{
    private readonly IFeishuTenantV1ChatGroupMenu _menuClient;

    public ChatMenuService(IFeishuTenantV1ChatGroupMenu menuClient)
    {
        _menuClient = menuClient;
    }

    public async Task AddQuickLinksAsync(string chatId)
    {
        var request = new AddChatGroupMenuRequest
        {
            menus = new List<MenuItem>
            {
                new MenuItem
                {
                    name = "项目文档",
                    icon = "https://example.com/doc_icon.png",
                    url = "https://docs.example.com/project"
                },
                new MenuItem
                {
                    name = "任务看板",
                    icon = "https://example.com/board_icon.png",
                    url = "https://board.example.com/tasks"
                }
            }
        };

        var result = await _menuClient.AddMenuByIdAsync(chatId, request);
        if (result?.Data != null)
        {
            Console.WriteLine("菜单添加成功");
        }
    }
}
```

---

### 更新群菜单

**函数名称**：更新群菜单

**函数签名**：
```csharp
Task<FeishuApiResult<UpdateChatMenuItemResult>?> UpdateMenuByIdAsync(
    [Path] string chat_id,
    [Path] string menu_item_id,
    [Body] UpdateChatMenuItemRequest updateChatMenuItemRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| menu_item_id | string | ✅ | 一级菜单或者二级菜单的 ID，示例："7156553273518882844" |
| updateChatMenuItemRequest | UpdateChatMenuItemRequest | ✅ | 更新群菜单请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "menu_item_id": "menu_001",
    "name": "更新后的名称",
    "url": "https://new-url.example.com"
  }
}
```

**说明**：修改指定群组内的某个一级菜单或者二级菜单的元信息，包括图标、名称、国际化名称和跳转链接。

---

### 删除群菜单

**函数名称**：删除群菜单

**函数签名**：
```csharp
Task<FeishuApiResult<ChatGroupMenuResult>?> DeleteMenuByIdAsync(
    [Path] string chat_id,
    [Body] ChartMenuIdsRequest deleteMenuIdsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| deleteMenuIdsRequest | ChartMenuIdsRequest | ✅ | 删除群内的一级菜单请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "menu_tree": []
  }
}
```

**说明**：删除指定群内的一级菜单。成功调用后接口会返回群组内最新的群菜单信息。

---

### 排序群菜单

**函数名称**：排序群菜单

**函数签名**：
```csharp
Task<FeishuApiResult<ChatGroupMenuResult>?> SortMenuByIdAsync(
    [Path] string chat_id,
    [Body] ChartMenuIdsRequest sortMenuRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| sortMenuRequest | ChartMenuIdsRequest | ✅ | 菜单项目排序请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "menu_tree": [
      {
        "menu_item_id": "menu_002",
        "name": "置顶菜单"
      },
      {
        "menu_item_id": "menu_001",
        "name": "第二菜单"
      }
    ]
  }
}
```

**说明**：调整指定群组内的群菜单排列顺序，成功调用后接口会返回群组内所有群菜单信息。

**代码示例**：
```csharp
public async Task ReorderMenusAsync(string chatId, List<string> menuIdsInNewOrder)
{
    var request = new ChartMenuIdsRequest
    {
        menu_ids = menuIdsInNewOrder
    };

    await _menuClient.SortMenuByIdAsync(chatId, request);
}
```

---

### 获取群菜单

**函数名称**：获取群菜单

**函数签名**：
```csharp
Task<FeishuApiResult<ChatGroupMenuResult>?> GetMenuByIdAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "menu_tree": [
      {
        "menu_item_id": "menu_001",
        "name": "工作台",
        "icon": "https://example.com/icon.png",
        "url": "https://workbench.example.com",
        "children": [
          {
            "menu_item_id": "menu_001_1",
            "name": "子菜单",
            "url": "https://sub.example.com"
          }
        ]
      }
    ]
  }
}
```

**说明**：获取指定群组内的群菜单信息，包括所有一级或二级菜单的名称、跳转链接、图标等信息。

**代码示例**：
```csharp
public async Task ShowGroupMenusAsync(string chatId)
{
    var result = await _menuClient.GetMenuByIdAsync(chatId);
    if (result?.Data?.menu_tree != null)
    {
        foreach (var menu in result.Data.menu_tree)
        {
            Console.WriteLine($"菜单: {menu.name} -> {menu.url}");
            if (menu.children != null)
            {
                foreach (var child in menu.children)
                {
                    Console.WriteLine($"  子菜单: {child.name}");
                }
            }
        }
    }
}
```
