# 考勤归档报表管理接口（租户） - FeishuTenantV1AttendanceArchives

## 接口名称
**考勤归档报表管理（租户令牌）** - (FeishuTenantV1AttendanceArchives)

## 功能描述
归档报表用于对应后台假勤管理-考勤统计-报表-归档报表功能。支持引用系统报表，可设置归档时间和数据归档周期，并且支持根据部门/人员、国家/地区、人员类型、工作地点、职级、序列、职务进行人员圈选。

## 参考文档
- [飞书开放平台 - 考勤归档报表文档](https://open.feishu.cn/document/server-docs/attendance-v1/archive_rule/list)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `UploadReportArchiveRuleAsync` | 写入归档报表结果 | 租户令牌 | POST |
| （继承自 FeishuV1AttendanceArchives） | 查询归档报表表头 / 删除归档报表行数据 / 查询所有归档规则 | 租户令牌 | POST/GET |

---

## UploadReportArchiveRuleAsync - 写入归档报表结果

写入归档报表结果，对应假勤管理-考勤统计-报表-归档报表页签，点击报表名称进入后的导入功能。可以将数据直接写入归档报表。

### 函数签名

```csharp
[Post("/open-apis/attendance/v1/archive_rule/upload_report")]
Task<FeishuApiResult<ArchiveUploadReportResult>?> UploadReportArchiveRuleAsync(
      [Body] ArchiveUploadReportRequest archiveUploadReportRequest,
      [Query("employee_type")] string employee_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
```

### 说明

- 飞书官方元数据标注该接口**仅支持租户令牌**，因此方法定义在 `IFeishuTenantV1AttendanceArchives` 上，不再出现在用户令牌客户端中。
- 其余方法（查询归档报表表头、删除归档报表行数据、查询所有归档规则）同时支持租户/用户令牌，继承自 `IFeishuV1AttendanceArchives`。

---

## 说明

此接口继承自 `FeishuV1AttendanceArchives` 基础接口，使用**租户令牌**进行认证。所有具体功能方法定义在父接口中。

### 认证方式
**租户令牌** (TenantAccessToken)

### 使用场景
- 适用于需要以应用身份访问归档报表的场景
- 可访问企业内所有考勤归档数据
- 常用于服务端后台管理系统集成

### 代码示例

```csharp
// 使用租户令牌获取归档报表信息
public class ArchiveService
{
    private readonly IFeishuTenantV1AttendanceArchives _archivesClient;

    public ArchiveService(IFeishuTenantV1AttendanceArchives archivesClient)
    {
        _archivesClient = archivesClient;
    }

    public async Task GetArchiveReportAsync()
    {
        // 调用父接口定义的方法
        // 注意：具体方法取决于 FeishuV1AttendanceArchives 的定义
    }
}
```
