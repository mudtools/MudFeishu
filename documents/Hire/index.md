# 飞书招聘（Hire）SDK 接口文档

## 概述

飞书招聘（Hire）SDK 提供了招聘服务端 OpenAPI（hire-v1 招聘配置类端点）的完整封装，支持职位组合创建/更新、职位设置维护、职位管理人员批量更新、职位信息与聚合详情查询、职位列表与发布管理、地址查询、角色与用户角色权限查询以及职位模板/职能分类/职位类别等枚举主数据查询能力。

> 说明：本批次 20 个端点均为 **tenant-only**（仅支持 tenant_access_token），令牌类型由接口上的 `[Token(FeishuTokenTypes.TenantAccessToken, ...)]` 声明，调用方无需额外指定。

**主要功能：**

- 职位组合创建/更新（一步完成职位、管理人员、登记表配置）
- 职位设置（JobConfig）的读取与更新（面试轮次、登记表、自助约面等）
- 职位管理人员（招聘负责人/用人经理/招聘助理）批量更新与查询
- 职位基础信息、聚合详情（含设置、门店、标签、阶段统计）与列表查询
- 职位上架（open）、职位发布记录搜索、招聘官网广告发布
- 地址码查询（按国家/省/市/区）与地址列表查询（按用途）
- 角色详情/列表、用户角色列表查询（含业务管理范围与权限配置）
- 职位模板、职能分类、职位类别列表查询

**适用场景：**

- 与三方招聘系统（ATS/RMS）同步职位与组织枚举主数据
- 按规则批量创建/更新职位并自动发布至招聘官网
- 权限审计：拉取角色与用户角色的权限配置及业务管理范围
- 招聘数据仓库：增量拉取职位列表与发布记录

**文档使用指引：**

本索引文档提供了所有飞书招聘相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及参考文档链接。

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

// 注册 API 服务（FeishuModule.Hire 或 FeishuModule.All 均可）
builder.Services.CreateFeishuServicesBuilder()
    .AddHireApi()
    .Build();
```

### 依赖注入使用

在 Controller 或服务中通过构造函数注入接口：

```csharp
using Mud.Feishu;

public class HireController : ControllerBase
{
    private readonly IFeishuTenantV1HireJob _jobApi;

    public HireController(IFeishuTenantV1HireJob jobApi)
    {
        _jobApi = jobApi;
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetJob(string jobId)
    {
        var result = await _jobApi.GetJobAsync(jobId, user_id_type: "open_id");
        return Ok(result);
    }
}
```

## API 接口导航

### 职位管理

- [职位（Job）](./FeishuTenantV1HireJob.md) — 组合创建/更新职位、职位设置更新与查询、管理人员批量更新、职位信息/聚合详情/列表查询、职位上架、职位发布人查询

### 职位发布

- [职位发布记录（JobPublishRecord）](./FeishuTenantV1HireJobPublishRecord.md) — 按渠道搜索职位发布记录
- [招聘广告（Advertisement）](./FeishuTenantV1HireAdvertisement.md) — 发布招聘官网广告

### 地址

- [地址（Location）](./FeishuTenantV1HireLocation.md) — 按地点类型查询地址码、按用途查询地址列表

### 权限与角色

- [角色（Role）](./FeishuTenantV1HireRole.md) — 角色详情、角色列表（含权限配置）
- [用户角色（UserRole）](./FeishuTenantV1HireUserRole.md) — 用户角色列表（含业务管理范围）

### 枚举与主数据

- [职位模板（JobSchema）](./FeishuTenantV1HireJobSchema.md) — 获取职位模板列表
- [职能分类（JobFunction）](./FeishuTenantV1HireJobFunction.md) — 获取职能分类列表
- [职位类别（JobType）](./FeishuTenantV1HireJobType.md) — 获取职位类别列表

## 查询对象模式（API-2）

查询参数 ≥ 6 个的接口采用查询对象模式（`Mud.HttpUtils.IQueryParameter`），实现类位于 `Mud.Feishu.DataModels.Hire`：

| 查询对象                 | 对应接口                | 查询参数                                                                   |
| ------------------------ | ----------------------- | -------------------------------------------------------------------------- |
| `JobListQuery`           | `IFeishuTenantV1HireJob.GetJobListAsync`           | update_start_time、update_end_time、page_size、page_token、4 个 id_type |
| `JobPublishRecordSearchQuery` | `IFeishuTenantV1HireJobPublishRecord.SearchJobPublishRecordAsync` | page_token、page_size、4 个 id_type |
| `UserRoleListQuery`      | `IFeishuTenantV1HireUserRole.GetUserRoleListAsync` | page_token、page_size、user_id、role_id、update_start_time、update_end_time、user_id_type |

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **DTO 命名空间**：`Mud.Feishu.DataModels.Hire`
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
