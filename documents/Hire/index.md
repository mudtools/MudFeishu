# 飞书招聘（Hire）SDK 接口文档

## 概述

飞书招聘（Hire）SDK 提供了招聘服务端 OpenAPI（hire-v1 招聘配置类端点）的完整封装，支持职位组合创建/更新、职位设置维护、职位管理人员批量更新、职位信息与聚合详情查询、职位列表与发布管理、地址查询、角色与用户角色权限查询以及职位模板/职能分类/职位类别等枚举主数据查询能力。

> 说明：本 SDK 全部 56 个端点均为 **tenant-only**（仅支持 tenant_access_token），令牌类型由接口上的 `[Token(FeishuTokenTypes.TenantAccessToken, ...)]` 声明，调用方无需额外指定。接口已按子域方案（B）合并为 6 个，见下方「API 接口导航」。

**主要功能：**

- 职位组合创建/更新（一步完成职位、管理人员、登记表配置）
- 职位设置（JobConfig）的读取与更新（面试轮次、登记表、自助约面等）
- 职位管理人员（招聘负责人/用人经理/招聘助理）批量更新与查询
- 职位基础信息、聚合详情（含设置、门店、标签、阶段统计）与列表查询
- 职位上架（open）、职位发布记录搜索、招聘官网广告发布
- 地址码查询（按国家/省/市/区）与地址列表查询（按用途）
- 角色详情/列表、用户角色列表查询（含业务管理范围与权限配置）
- 职位模板、职能分类、职位类别列表查询
- 招聘需求 CRUD（创建/更新/查询/删除）与需求模板查询
- 面试设置：面试轮次类型、面试反馈表、面试登记表模板、面试官认证信息
- Offer 设置：申请表列表/模板详情、自定义字段更新、审批模板
- 招聘配置字典：招聘流程、科目、信息登记表模板、人才标签

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

接口已按「模块 × 令牌 × 功能子域」合并为 5 个（合并规则见 AGENTS.md「接口子域合并」）：

| 子域接口 | 方法数 | 覆盖范围 | 历史单资源文档（接口名以合并后为准） |
| -------- | ----- | -------- | ------------------------------------ |
| `IFeishuTenantV1HireJob` | 15 | 职位组合创建/更新、职位设置、管理人员批量更新、职位信息/聚合详情/列表、职位上架、发布人；职位类别、职能分类、职位模板、发布记录搜索、广告发布 | [职位](./FeishuTenantV1HireJob.md)、[发布记录](./FeishuTenantV1HireJobPublishRecord.md)、[广告](./FeishuTenantV1HireAdvertisement.md)、[职位模板](./FeishuTenantV1HireJobSchema.md)、[职能分类](./FeishuTenantV1HireJobFunction.md)、[职位类别](./FeishuTenantV1HireJobType.md) |
| `IFeishuTenantV1HireJobRequirement` | 6 | 招聘需求创建/更新/按 ID 批量查询/列表/删除、需求模板 | — |
| `IFeishuTenantV1HireInterview` | 5 | 面试轮次类型、面试反馈表、面试登记表模板、面试官查询与更新 | — |
| `IFeishuTenantV1HireOffer` | 4 | Offer 申请表列表/详情、申请表自定义字段更新、Offer 审批模板 | — |
| `IFeishuTenantV1HireSetting` | 9 | 招聘流程、科目、信息登记表模板、人才标签、地点、角色、用户角色 | [地址](./FeishuTenantV1HireLocation.md)、[角色](./FeishuTenantV1HireRole.md)、[用户角色](./FeishuTenantV1HireUserRole.md) |
| `IFeishuTenantV1HireCandidate` | 70 | 内推信息与内推官网职位、招聘官网/推广渠道/官网用户/官网职位、官网投递与投递任务、官网申请表模板；人才管理（人才池/文件夹/标签/黑名单/组合创建更新/入职状态）；投递流程管理（面试信息与评价记录、Offer、背调订单、三方协议、入职与员工）；人才备注、评估/阅卷/面试任务、简历来源 | — |

## 查询对象模式（API-2）

查询参数 ≥ 6 个的接口采用查询对象模式（`Mud.HttpUtils.IQueryParameter`），实现类位于 `Mud.Feishu.DataModels.Hire`：

| 查询对象                 | 对应接口                | 查询参数                                                                   |
| ------------------------ | ----------------------- | -------------------------------------------------------------------------- |
| `JobListQuery`           | `IFeishuTenantV1HireJob.GetJobListAsync`           | update_start_time、update_end_time、page_size、page_token、4 个 id_type |
| `JobPublishRecordSearchQuery` | `IFeishuTenantV1HireJob.SearchJobPublishRecordAsync` | page_token、page_size、4 个 id_type |
| `UserRoleListQuery`      | `IFeishuTenantV1HireSetting.GetUserRoleListAsync` | page_token、page_size、user_id、role_id、update_start_time、update_end_time、user_id_type |

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **DTO 命名空间**：`Mud.Feishu.DataModels.Hire`
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
