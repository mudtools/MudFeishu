# 审批 SDK 接口文档

## 概述

审批 SDK 提供了飞书审批系统的完整 API 封装，支持原生审批管理、三方审批集成、审批任务操作、审批查询等功能，帮助开发者快速构建企业级审批应用。

**主要功能：**

- 原生审批定义与实例管理
- 三方审批系统集成与数据同步
- 审批任务操作（同意、拒绝、转交、退回等）
- 审批数据查询（租户级别与用户级别）
- 审批评论管理
- 审批 Bot 消息推送
- 审批文件上传

**适用场景：**

- 企业内部审批流程自动化
- 第三方审批系统与飞书审批集成
- 审批数据统计分析与报表
- 审批消息通知与提醒

**文档使用指引：**

本索引文档提供了所有审批相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

## 快速开始

### 安装

```bash
dotnet add package Mud.Feishu
```

### 基本使用

```csharp
using Mud.Feishu;

// 创建飞书应用客户端
var feishuApp = FeishuAppBuilder.Create()
    .WithAppConfig(new FeishuAppConfig
    {
        AppKey = "your_app_id",
        AppSecret = "your_app_secret"
    })
    .Build();

// 获取审批接口
var approval = feishuApp.GetRequiredService<IFeishuTenantV4Approval>();

// 创建审批实例
var result = await approval.CreateInstanceAsync(new CreateApprovalInstanceRequest
{
    ApprovalCode = "approval_code",
    UserId = "user_id"
});
```

## API 接口导航

### 审批管理

- [原生审批管理接口](./FeishuV4Approval_Tenant.md) — 管理飞书原生审批定义与实例，支持创建审批定义、创建/撤回审批实例等操作
- [三方审批管理接口](./FeishuV4ApprovalExternal_Tenant.md) — 将企业原有审批系统与飞书审批系统连通，实现数据同步与流转

### 审批任务与评论

- [审批任务操作接口](./FeishuV4ApprovalTask_Tenant.md) — 操作审批任务，支持同意、拒绝、转交、退回、加签等操作
- [审批评论接口](./FeishuV4ApprovalComments_Tenant.md) — 管理审批实例内的评论功能，支持创建、删除、查询评论

### 审批查询

- [审批查询接口（租户）](./FeishuV4ApprovalQuery_Tenant.md) — 租户级别的审批数据查询，支持查询审批实例、抄送、任务列表
- [审批查询接口（用户）](./FeishuV4ApprovalQuery_User.md) — 用户级别的审批数据查询，适用于用户自助查询场景

### 其他功能

- [审批 Bot 消息接口](./FeishuV1ApprovalMessage_Tenant.md) — 通过审批 Bot 推送消息给用户或更新审批 Bot 消息
- [审批文件管理接口](./FeishuV4ApprovalFile_Tenant.md) — 上传审批表单控件内的文件（图片、附件等）

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：待补充
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
