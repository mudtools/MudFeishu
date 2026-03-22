# 考勤 SDK 接口文档

## 概述

考勤 SDK 提供了飞书考勤系统的完整 API 封装，支持考勤组管理、班次排班、打卡记录、考勤统计、审批补卡等功能，帮助开发者快速构建企业级考勤管理应用。

**主要功能：**

- 考勤组与班次管理
- 排班表创建与查询
- 打卡流水记录导入与查询
- 考勤统计报表定制与查询
- 假勤审批数据管理
- 补卡审批与休假管理
- 归档报表管理
- 用户人脸识别设置

**适用场景：**

- 企业考勤管理系统集成
- 第三方打卡设备数据同步
- 考勤数据统计与报表分析
- 假勤审批流程自动化

**文档使用指引：**

本索引文档提供了所有考勤相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

// 获取考勤组管理接口
var groups = feishuApp.GetRequiredService<IFeishuTenantV1AttendanceGroups>();

// 获取考勤组列表
var result = await groups.GetGroupPageListAsync(pageSize: 10);
```

## API 接口导航

### 考勤组与班次管理

- [考勤组管理接口（租户）](./FeishuTenantV1AttendanceGroups.md) — 管理考勤组的创建、删除、查询，设置考勤方式、时间、地点等规则
- [考勤组管理接口（用户）](./FeishuUserV1AttendanceGroups.md) — 用户权限的考勤组管理，适用于员工查询自己所在的考勤组信息
- [考勤班次管理接口](./FeishuTenantV1AttendanceShifts.md) — 管理考勤班次，设置上下班时间、迟到规则、休息规则等
- [考勤排班管理接口](./FeishuTenantV1AttendanceUserDailyShifts.md) — 管理排班表，为指定人员在指定日期安排班次

### 考勤打卡与统计

- [考勤打卡管理接口](./FeishuTenantV1AttendanceUserFlows.md) — 导入、查询、删除员工的打卡流水记录
- [考勤统计接口](./FeishuTenantV1AttendanceStats.md) — 定制统计报表表头，查询考勤统计数据

### 考勤审批与休假

- [考勤审批管理接口](./FeishuTenantV1AttendanceApprovals.md) — 管理三方系统假勤审批的请假、加班、外出和出差审批数据
- [考勤补卡管理接口](./FeishuTenantV1AttendanceRemedys.md) — 创建补卡审批、查询可补卡时间及补卡记录
- [考勤休假管理接口](./FeishuV1AttendanceLeave_Tenant.md) — 休假发放记录查询与修改

### 归档报表与用户设置

- [考勤归档报表管理接口（租户）](./FeishuTenantV1AttendanceArchives.md) — 租户权限的归档报表管理，可访问企业内所有考勤归档数据
- [考勤归档报表管理接口（用户）](./FeishuUserV1AttendanceArchives.md) — 用户权限的归档报表管理，访问权限受限于当前用户
- [考勤用户设置接口](./FeishuTenantV1AttendanceUserSettings.md) — 修改用户人脸识别信息、上传下载人脸照片

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：待补充
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
