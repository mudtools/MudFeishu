---
title: Mud.Feishu SDK 接口文档 | MudFeishu
description: 该文档为 Mud.Feishu 企业级 .NET SDK 的总入口，介绍 SDK 对飞书开放平台 API 的完整封装，涵盖 HTTP 客户端、WebSocket 实时事件与 Webhook 事件处理等核心特性。
---

# Mud.Feishu SDK 接口文档

## 概述

Mud.Feishu 是一个企业级 .NET SDK，为飞书开放平台 API 提供完整的封装。SDK 支持 HTTP API 客户端、WebSocket 实时事件和 Webhook 事件处理，帮助开发者快速构建企业级飞书应用。

**核心特性：**

- 完整的飞书 API 接口封装
- 支持租户令牌和用户令牌两种认证方式
- 异步编程模型，支持取消令牌
- 统一的响应模型和错误处理
- 依赖注入友好设计

**适用场景：**

- 企业内部系统集成
- 飞书机器人开发
- 企业办公自动化
- 数据同步与分析

**文档使用指引：**

本索引文档提供了所有 SDK 模块的导航入口。每个模块包含该领域相关 API 的详细文档。点击各模块链接可查看该模块的完整 API 列表。

## 快速开始

### 安装

```bash
dotnet add package Mud.Feishu
```

### 配置文件

在 `appsettings.json` 中添加飞书应用配置：

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "your_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "IsDefault": true
    }
  ]
}
```

### 注册服务

在 `Program.cs` 中注册飞书服务：

```csharp
// 添加飞书服务
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册 API 服务
builder.Services.CreateFeishuServicesBuilder()
    .AddModules(FeishuModule.All)
    .Build();
```

### 依赖注入使用

在 Controller 或服务中通过构造函数注入接口：

```csharp
using Mud.Feishu;

public class MyController : ControllerBase
{
    private readonly IFeishuTenantV1Message _messageApi;
    private readonly IFeishuTenantV3User _userApi;

    public MyController(IFeishuTenantV1Message messageApi, IFeishuTenantV3User userApi)
    {
        _messageApi = messageApi;
        _userApi = userApi;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var result = await _messageApi.SendMessageAsync(request, "open_id");
        return Ok(result);
    }
}
```

## 模块导航

### 协作与沟通

| 模块                                      | 说明                                                         |
| ----------------------------------------- | ------------------------------------------------------------ |
| [消息（Message）](./Message/index.md)     | 消息发送、回复、编辑、撤回、转发，获取历史消息与已读状态     |
| [群组（ChatGroup）](./ChatGroup/index.md) | 群聊创建、更新、解散，群成员管理，群公告，群菜单，会话标签页 |
| [卡片（Card）](./Card/index.md)           | 卡片创建、更新，卡片组件管理，消息流卡片                     |
| [邮箱（Mail）](./Mail/index.md)           | 邮件收发、会话与草稿、邮箱组织、邮件组、模板与签名           |

### 文档与存储

| 模块                                | 说明                                          |
| ----------------------------------- | --------------------------------------------- |
| [文档（Docx）](./Docx/index.md)     | 飞书云文档创建、获取，文档块管理              |
| [云空间（Drive）](./Drive/index.md) | 文件上传下载、文件夹管理、素材管理            |
| [知识库（Wiki）](./Wiki/index.md)   | 知识空间管理，节点创建、移动、复制，Wiki 搜索 |

### 组织与人员

| 模块                                                | 说明                                             |
| --------------------------------------------------- | ------------------------------------------------ |
| [组织架构（Organization）](./Organization/index.md) | 部门管理、用户管理、角色权限、职务职级、用户群组 |

### 业务应用

| 模块                                        | 说明                                               |
| ------------------------------------------- | -------------------------------------------------- |
| [审批（Approval）](./Approval/index.md)     | 原生审批与三方审批管理，审批实例创建与查询         |
| [考勤（Attendance）](./Attendance/index.md) | 考勤组管理、班次管理、打卡记录、考勤统计、请假审批 |
| [任务（Task）](./Task/index.md)             | 任务创建与管理，清单管理，评论、附件、自定义字段   |
| [飞书妙搭（Spark）](./Spark/index.md)       | 应用管理与运营数据、用户 ID 转换、产品使用权限、自定义枚举、执行 SQL、数据表与记录管理、视图查询、文件存储 |
| [飞书妙记（Minutes）](./Minutes/index.md)   | 妙记基础信息、音视频下载、文字记录导出、统计数据、AI 产物、搜索、剪辑与导入生成、事件订阅 |
| [飞书招聘（Hire）](./Hire/index.md)         | 职位组合创建/更新、职位设置与管理人员、职位列表与发布、地址查询、角色与用户角色权限、职位模板/职能/类别 |
| [帮助中心（HelpDesk）](./HelpDesk/index.md) | 工单管理、客服坐席、工单分类、常见问题、事件订阅与通知       |

### 人工智能

| 模块 | 说明 |
| --- | --- |
| [飞书 Aily（Aily）](./Aily/index.md) | 智能体对话与会话管理、技能调用、数据知识问答与管理 |

### 跨模块通用主题

| 主题 | 说明 |
| --- | --- |
| [错误处理指南](./ErrorHandling.md) | 统一响应模型、`ApiException` 语义、**文件下载类接口（`Task<byte[]?>`）的错误契约与残余风险** |
| [响应缓存使用指南](./ResponseCaching.md) | `[Cache]` 特性接入方式、**多应用缓存键隔离约束**、适合缓存的接口清单 |

## Webhook 排障速查

> 响应码就是**对飞书的 ACK 语义**：200 = 已消费（停止重推）、4xx = 终态丢弃（飞书不再重推）、
> 5xx = 请重推。完整决策表见 [错误处理指南](./ErrorHandling.md) 的「Webhook ACK 决策表」。

| 现象 | 判定 | 处置 |
| --- | --- | --- |
| 持续返回 **503** 且指标 `signature_unavailable` 上升 | 验签**无法完成判定**（密钥服务故障 / 配置热更后构建失败）——**不是**签名不匹配 | 检查 `IEncryptKeyProvider`（KeyVault/Vault）与最近一次配置变更；连续 10 次 503 即应介入 |
| 持续返回 **503** 但 `signature_unavailable` 不涨 | 去重体系致命故障、验签/解密超时，或拦截器要求重推（`InterceptionAckMode=Retryable`） | 看响应头 `Retry-After` 与去重实现（Redis）健康度 |
| 返回 **403** | 签名已被判定为无效（终态，飞书不再重推） | 核对 `EncryptKey` 与 `SHA-256(timestamp + nonce + encryptKey + body)` 计算口径 |
| 端点全量 **404**、启动日志却干净 | `FeishuWebhook:Apps` 为空，或路由前缀写法不匹配 | 3.x 起 `Apps` 为空会在 `UseFeishuWebhook()` 处**启动失败**；前缀 `"/feishu"` / `"feishu/"` 会被自动归一化 |
| 事件被 **413** 拒绝 | 请求体超过 `MaxRequestBodySize`（默认 **1MB**） | 确有超大卡片事件时显式调大该值；服务端日志记录实际长度 |

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：3.0.0
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
