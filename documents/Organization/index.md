# 组织架构 SDK 接口文档

## 概述

组织架构 SDK 提供了飞书通讯录的完整 API 封装，支持部门管理、用户管理、角色管理、用户组管理等功能，帮助开发者构建企业级组织管理应用。

**主要功能：**

- 部门创建、更新、删除、查询
- 用户创建、更新、离职、恢复
- 角色与角色成员管理
- 用户组与成员管理
- 职级、职务、序列管理
- 单位与人员类型管理

**适用场景：**

- 企业组织架构同步
- 员工入离职自动化
- 权限管理系统集成
- 组织数据分析与报表

**文档使用指引：**

本索引文档提供了所有组织架构相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class UserController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;

    public UserController(IFeishuTenantV3User userApi)
    {
        _userApi = userApi;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var result = await _userApi.GetUserInfoByIdAsync(userId);
        return Ok(result.Data);
    }
}
```

## API 接口导航

### 部门管理

- [租户 V1 部门管理](./FeishuTenantV1Departments.md) — 部门创建、更新、删除、批量查询与搜索
- [租户 V3 部门管理](./FeishuTenantV3Departments.md) — 部门信息获取、子部门列表、部门 ID 变更、部门群解绑
- [用户 V1 部门管理](./FeishuUserV1Departments.md) — 用户权限的部门管理（V1 版本）
- [用户 V3 部门管理](./FeishuUserV3Departments.md) — 用户权限的部门查询（V3 版本）

### 用户管理

- [租户 V1 员工管理](./FeishuTenantV1Employees.md) — 员工创建、更新、离职、恢复、批量查询
- [租户 V3 用户管理](./FeishuTenantV3User.md) — 用户创建、更新、删除、恢复、JSAPI 票据获取
- [用户 V1 员工管理](./FeishuUserV1Employees.md) — 用户权限的员工管理（V1 版本）
- [用户 V3 用户管理](./FeishuUserV3User.md) — 用户权限的用户查询与搜索（V3 版本）

### 角色与用户组

- [租户 V3 角色管理](./FeishuTenantV3Role.md) — 角色创建、更新、删除
- [租户 V3 角色成员管理](./FeishuTenantV3RoleMember.md) — 角色成员添加、管理范围设置、成员查询
- [租户 V3 用户组管理](./FeishuTenantV3UserGroup.md) — 用户组创建、更新、查询、删除
- [租户 V3 用户组成员管理](./FeishuTenantV3UserGroupMember.md) — 用户组成员添加、查询、移除

### 职级职务序列

- [租户 V3 职级管理](./FeishuTenantV3JobLevel.md) — 职级创建、更新、查询、删除
- [租户 V3 职务管理](./FeishuTenantV3JobTitle.md) — 职务列表与详情查询
- [租户 V3 序列管理](./FeishuTenantV3JobFamilies.md) — 序列创建、更新、查询、删除
- [用户 V3 职务管理](./FeishuUserV3JobTitle.md) — 用户权限的职务查询

### 单位与人员类型

- [租户 V3 单位管理](./FeishuTenantV3Unit.md) — 单位创建、更新、部门绑定、查询
- [租户 V3 人员类型管理](./FeishuTenantV3EmployeeType.md) — 人员类型创建、更新、查询、删除

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
