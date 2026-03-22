# 考勤组管理接口（用户） - FeishuUserV1AttendanceGroups

## 接口名称
**考勤组管理（用户权限）** - (FeishuUserV1AttendanceGroups)

## 功能描述
考勤组是对部门或者员工在某个特定场所及特定时间段内的出勤情况的一种规则设定，包括上下班、迟到、早退、病假、婚假、丧假、公休、工作时间、加班情况等。通过设置考勤组，可以从部门、员工两个维度，来设定考勤方式、考勤时间、考勤地点等考勤规则。

## 参考文档
- [飞书开放平台 - 考勤组管理文档](https://open.feishu.cn/document/server-docs/attendance-v1/group/create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| （继承自 FeishuV1AttendanceGroups） | 参见父接口文档 | 用户令牌 | - |

---

## 说明

此接口继承自 `FeishuV1AttendanceGroups` 基础接口，使用**用户令牌**进行认证。所有具体功能方法定义在父接口中。

### 认证方式
**用户令牌** (UserAccessToken)

### 接口特性
- 继承 `ICurrentUserId` 接口，可获取当前登录用户ID
- 访问权限受限于当前用户的权限范围
- 适用于前端应用或个人工作台场景

### 使用场景
- 员工查询自己所在的考勤组信息
- 考勤管理员查看自己管辖的考勤组
- 个人考勤相关功能集成

### 代码示例

```csharp
// 使用用户权限操作考勤组
public class UserAttendanceGroupService
{
    private readonly IFeishuUserV1AttendanceGroups _groupsClient;

    public UserAttendanceGroupService(IFeishuUserV1AttendanceGroups groupsClient)
    {
        _groupsClient = groupsClient;
    }

    public async Task GetCurrentUserGroupAsync()
    {
        // 获取当前用户所在的考勤组
        // 注意：具体方法取决于 FeishuV1AttendanceGroups 的定义
        var currentUserId = ((ICurrentUserId)_groupsClient).CurrentUserId;
        Console.WriteLine($"当前用户ID: {currentUserId}");
        
        // 调用父接口方法查询考勤组信息
    }
}
```
