# 飞书绩效（Performance）SDK 接口文档

## 概述

飞书绩效 SDK 提供了飞书绩效（performance-v1 / performance-v2）相关 API 的完整封装，支持绩效结果与详情数据查询、周期任务查询、关键指标数据的获取与录入，以及周期项目、评估配置与指标配置等后台配置的读取与维护。

**主要功能：**

- 获取绩效结果（v1）与绩效详情数据（v2）
- 按指定用户或全量分页获取周期任务
- 获取与录入被评估人关键指标数据
- 查询周期列表与项目配置、被评估人信息、人员快照
- 批量查询/导入/删除被评估人补充信息，更新人员组成员
- 查询绩效模板、评估项、标签填写题，以及指标模板、指标库指标、指标字段与指标标签

**适用场景：**

- 绩效结果数据的抽取与报表分析
- 关键指标数据的批量录入与同步
- 绩效周期与项目配置的自动化维护
- 评估模板与指标库的对接管理

**文档使用指引：**

本索引文档提供了所有飞书绩效相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及响应示例。点击各 API 链接可查看详细文档。

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

public class PerformanceController : ControllerBase
{
    private readonly IFeishuTenantV2PerformanceReviewData _reviewDataApi;
    private readonly IFeishuTenantV2PerformanceSemesterActivity _configApi;

    public PerformanceController(IFeishuTenantV2PerformanceReviewData reviewDataApi, IFeishuTenantV2PerformanceSemesterActivity configApi)
    {
        _reviewDataApi = reviewDataApi;
        _configApi = configApi;
    }

    [HttpPost("review_datas")]
    public async Task<IActionResult> QueryReviewData([FromBody] QueryReviewDataDetailRequest request)
    {
        var result = await _reviewDataApi.QueryReviewDataDetailAsync(request);
        return Ok(result);
    }

    [HttpGet("semesters")]
    public async Task<IActionResult> GetSemesters()
    {
        var result = await _configApi.GetSemesterListAsync();
        return Ok(result);
    }
}
```

## API 接口导航

### 绩效结果

- [绩效结果 v1（租户）](./FeishuTenantV1PerformanceReviewData.md) — 获取被评估人在指定周期、项目中各环节的评估结果
- [绩效详情数据 v2（租户）](./FeishuTenantV2PerformanceReviewData.md) — 获取各环节评估详情与提交状态，返回数据更丰富

### 周期任务

- [周期任务（租户）](./FeishuTenantV1PerformanceStageTask.md) — 按指定用户或全量分页获取周期内各环节任务

### 关键指标数据

- [关键指标数据（租户）](./FeishuTenantV2PerformanceMetricDetail.md) — 获取被评估人关键指标结果、批量录入关键指标数据

### 后台配置

- [绩效后台配置（租户）](./FeishuTenantV2PerformanceSemesterActivity.md) — 周期与项目、补充信息、人员组、被评估人快照、评估配置与指标配置

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
