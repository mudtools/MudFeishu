# 考勤归档报表管理接口（用户） - FeishuUserV1AttendanceArchives

## 接口名称
**考勤归档报表管理（用户令牌）** - (FeishuUserV1AttendanceArchives)

## 功能描述
归档报表用于对应后台假勤管理-考勤统计-报表-归档报表功能。支持引用系统报表，可设置归档时间和数据归档周期，并且支持根据部门/人员、国家/地区、人员类型、工作地点、职级、序列、职务进行人员圈选。

## 参考文档
- [飞书开放平台 - 考勤归档报表文档](https://open.feishu.cn/document/server-docs/attendance-v1/archive_rule/list)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| （继承自 FeishuV1AttendanceArchives） | 查询归档报表表头 / 删除归档报表行数据 / 查询所有归档规则 | 用户令牌 | POST/GET |

> 注意：写入归档报表结果（upload_report）按飞书官方元数据仅支持**租户令牌**，已从本接口中移除，请使用 `IFeishuTenantV1AttendanceArchives.UploadReportArchiveRuleAsync`。

---

## 说明

此接口继承自 `FeishuV1AttendanceArchives` 基础接口，使用**用户令牌**进行认证。所有具体功能方法定义在父接口中。

### 认证方式
**用户令牌** (UserAccessToken)

### 使用场景
- 适用于需要以当前登录用户身份访问归档报表的场景
- 访问权限受限于当前用户的权限范围
- 常用于前端应用或个人工作台集成

### 代码示例

```csharp
// 使用用户令牌获取归档报表信息
public class UserArchiveService
{
    private readonly IFeishuUserV1AttendanceArchives _archivesClient;

    public UserArchiveService(IFeishuUserV1AttendanceArchives archivesClient)
    {
        _archivesClient = archivesClient;
    }

    public async Task GetUserArchiveReportAsync()
    {
        // 调用父接口定义的方法
        // 注意：具体方法取决于 FeishuV1AttendanceArchives 的定义
        // 当前用户只能访问自己有权限的归档数据
    }
}
```
