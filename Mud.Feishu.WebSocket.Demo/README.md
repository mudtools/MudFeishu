# 飞书WebSocket演示API

这是一个用于演示飞书WebSocket长连接功能的WebAPI服务。

## 🚀 功能特性

- 🔄 **WebSocket长连接管理** - 支持自动重连、心跳检测
- 👤 **用户事件处理** - 处理用户创建、更新等事件
- 🏢 **部门事件处理** - 处理部门创建、变更等事件  
- ✅ **审批事件处理** - 处理审批通过、拒绝等事件
- 📊 **实时统计监控** - 提供事件处理统计和监控
- 🎯 **模拟事件生成** - 自动生成测试事件用于演示
- 🌐 **Web测试界面** - 提供友好的Web界面进行测试

## 🛠️ 快速开始

### 1. 运行项目

```bash
# 切换到项目目录
cd Mud.Feishu.WebSocket.Demo

# 还原NuGet包
dotnet restore

# 运行项目
dotnet run
```

### 2. 访问测试界面

启动后访问以下地址：

- **测试界面**: http://localhost:5000 (或 https://localhost:5001)
- **API文档**: http://localhost:5000/swagger
- **健康检查**: http://localhost:5000/api/websocketdemo/status

## 📋 配置说明

### appsettings.json 配置

```json
{
  "Feishu": {
    "AppId": "demo_app_id",
    "AppSecret": "demo_app_secret", 
    "WebSocket": {
      "AutoReconnect": true,
      "MaxReconnectAttempts": 5,
      "ReconnectDelayMs": 5000,
      "HeartbeatIntervalMs": 30000,
      "EnableMultiHandlerMode": true,
      "EnableLogging": true
    }
  },
  "DemoSettings": {
    "EnableMockEvents": true,
    "MockEventIntervalMs": 10000
  }
}
```

### 环境变量配置

```bash
# 设置监听端口
set ASPNETCORE_URLS=http://localhost:5000

# 设置运行环境
set ASPNETCORE_ENVIRONMENT=Development

# 启用模拟事件
set DemoSettings__EnableMockEvents=true
```

## 🎯 API 接口

### WebSocket 连接管理

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/websocketdemo/connect` | 启动WebSocket连接 |
| POST | `/api/websocketdemo/disconnect` | 断开WebSocket连接 |
| POST | `/api/websocketdemo/reconnect` | 重新连接WebSocket |
| GET | `/api/websocketdemo/status` | 获取连接状态 |

### 事件生成

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/websocketdemo/generate-user-event` | 生成用户事件 |
| POST | `/api/websocketdemo/generate-department-event` | 生成部门事件 |
| POST | `/api/websocketdemo/generate-approval-event` | 生成审批事件 |

### 统计信息

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/websocketdemo/statistics` | 获取事件统计 |
| GET | `/api/websocketdemo/recent-events` | 获取最近事件 |
| DELETE | `/api/websocketdemo/clear-events` | 清空事件记录 |

## 🎨 Web界面功能

### 连接控制
- ✅ 一键连接/断开WebSocket
- 📊 实时显示连接状态和运行时间
- 🔄 自动重连状态监控

### 事件管理
- 👤 手动生成用户事件
- 🏢 手动生成部门事件  
- ✅ 手动生成审批事件
- 📋 查看最近事件记录

### 实时监控
- 📝 实时日志显示
- 📊 事件统计图表
- 🔄 自动刷新状态

## 🧪 测试用例

### 用户事件测试

```bash
# 生成用户事件
curl -X POST http://localhost:5000/api/websocketdemo/generate-user-event \
  -H "Content-Type: application/json"
```

### 部门事件测试

```bash
# 生成部门事件
curl -X POST http://localhost:5000/api/websocketdemo/generate-department-event \
  -H "Content-Type: application/json"
```

### 审批事件测试

```bash
# 生成审批事件
curl -X POST http://localhost:5000/api/websocketdemo/generate-approval-event \
  -H "Content-Type: application/json"
```

## 📊 事件数据格式

### 用户事件
```json
{
  "EventType": "contact.user.created_v3",
  "EventId": "uuid",
  "EventTime": "2024-01-01T00:00:00Z",
  "Data": {
    "user": {
      "user_id": "user_123",
      "name": "张三",
      "email": "zhangsan@example.com",
      "department": "技术部"
    }
  }
}
```

### 部门事件
```json
{
  "EventType": "contact.department.created_v3",
  "Data": {
    "department": {
      "department_id": "dept_123",
      "name": "技术部",
      "parent_department_id": "dept_root",
      "department_level": 2
    }
  }
}
```

### 审批事件
```json
{
  "EventType": "approval.approval.approved_v1",
  "Data": {
    "approval": {
      "approval_id": "approval_123",
      "definition_code": "LEAVE_REQUEST",
      "approval_status": "approved",
      "applicant_id": "user_123",
      "title": "请假申请"
    }
  }
}
```

## 🔧 开发调试

### 启用详细日志

```json
{
  "Logging": {
    "LogLevel": {
      "Mud.Feishu.WebSocket": "Debug",
      "Mud.Feishu.WebSocket.Demo": "Debug"
    }
  }
}
```

### 禁用模拟事件

```json
{
  "DemoSettings": {
    "EnableMockEvents": false
  }
}
```

## 🚨 故障排除

### WebSocket连接失败
1. 检查网络连接
2. 验证飞书配置信息
3. 查看详细错误日志

### 事件处理失败
1. 检查事件数据格式
2. 验证事件类型是否匹配
3. 查看处理器错误日志

### 页面无法访问
1. 确认服务已正常启动
2. 检查防火墙设置
3. 验证端口配置

## 📝 许可证

本项目遵循 MIT 许可证进行分发和使用。

---

🚀 **立即开始测试飞书WebSocket功能！**