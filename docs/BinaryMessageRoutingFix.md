# 二进制消息路由修复说明

## 问题描述

原始的`MessageRouter`只处理文本消息，无法处理从二进制消息转换而来的JSON内容。这导致飞书WebSocket客户端在接收到二进制消息时，虽然能够正确解析，但解析后的JSON内容无法被路由到相应的事件处理器。

## 修复内容

### 1. 扩展MessageRouter功能

**文件**: `Mud.Feishu.WebSocket/Core/MessageRouter.cs`

- 添加了`RouteBinaryMessageAsync`方法，专门处理从二进制消息转换的JSON内容
- 重构了原有的路由逻辑为`RouteMessageInternalAsync`方法，支持来源标识
- 添加了详细的日志记录，区分文本消息和二进制转换消息

### 2. 增强BinaryMessageProcessor

**文件**: `Mud.Feishu.WebSocket/Core/BinaryMessageProcessor.cs`

- 添加了`MessageRouter`依赖注入支持
- 新增构造函数重载，支持传入`MessageRouter`实例
- 在成功解析二进制消息为JSON后，自动调用MessageRouter进行路由
- 支持Frame解析和JSON Fallback两种模式的路由

### 3. 更新FeishuWebSocketClient

**文件**: `Mud.Feishu.WebSocket/FeishuWebSocketClient.cs`

- 修改`BinaryMessageProcessor`的初始化，传入`MessageRouter`实例
- 建立了二进制消息处理和消息路由的连接

## 工作流程

### 文本消息处理流程
```
WebSocket接收文本消息 → MessageRouter.RouteMessageAsync → 路由到对应处理器
```

### 二进制消息处理流程（修复后）
```
WebSocket接收二进制消息 → BinaryMessageProcessor → 
1. 触发BinaryMessageReceived事件
2. 解析为JSON内容 → MessageRouter.RouteBinaryMessageAsync → 路由到对应处理器
```

## 支持的二进制消息格式

### 1. Frame格式（ProtoBuf）
- 通过ProtoBuf反序列化获取Frame对象
- 提取Frame.Payload并转换为JSON字符串
- 消息类型标识为"Frame"

### 2. 纯JSON格式（Fallback）
- 当ProtoBuf解析失败时，直接将二进制数据作为UTF-8编码的JSON处理
- 消息类型标识为"JSON_Fallback"

## 事件处理器兼容性

现有的所有事件处理器（包括新添加的`DepartmentCreatedEventHandler`）都无需修改即可支持二进制消息路由，因为路由发生在JSON层面，处理器接收到的是标准的JSON字符串。

## 日志记录示例

```
[DEBUG] 将消息路由到处理器: DepartmentCreatedEventHandler (来源: Binary_Frame, 消息类型: event)
[INFO] 处理部门创建事件: {"event":{"department":{"department_id":"xxx","name":"新部门"}}}
```

## 配置说明

无需额外配置，修复是向后兼容的。只要启用了二进制消息处理（`EnableBinaryMessageProcessing = true`，默认启用），就会自动进行消息路由。

## 测试验证

1. 编译通过：`Mud.Feishu.WebSocket`和`Mud.Feishu.WebSocket.Demo`项目都能成功编译
2. 功能验证：二进制消息解析后能正确路由到对应的事件处理器
3. 兼容性：原有的文本消息处理流程不受影响

## 注意事项

1. 二进制消息路由是可选的，即使路由失败，原始的`BinaryMessageReceived`事件仍然会触发
2. 消息处理器接收到的消息内容格式与文本消息完全一致
3. 日志中会明确标识消息来源（Text或Binary），便于调试和监控