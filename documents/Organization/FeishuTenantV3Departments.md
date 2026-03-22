# 租户V3部门管理 - FeishuTenantV3Departments

## 接口名称
**租户V3部门管理 - (IFeishuTenantV3Departments)**

## 功能描述
本接口提供飞书通讯录V3版本部门的管理功能，适用于租户应用场景。支持部门的创建、更新（部分/全部）、删除、查询、ID变更以及部门群解绑等操作。

飞书组织机构部门是指企业组织架构树上的某一个节点。在部门内部，可添加用户作为部门成员，也可添加新的部门作为子部门。

## 参考文档
- [飞书官方文档 - 部门字段概述](https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetDepartmentInfoByIdAsync | 获取部门信息 | 租户令牌 | GET |
| GetDepartmentsByIdsAsync | 批量获取部门 | 租户令牌 | GET |
| GetDepartmentsByParentIdAsync | 获取子部门列表 | 租户令牌 | GET |
| GetParentDepartmentsByIdAsync | 获取父部门列表 | 租户令牌 | GET |
| CreateDepartmentAsync | 创建部门 | 租户令牌 | POST |
| UpdatePartDepartmentAsync | 部分更新部门 | 租户令牌 | PATCH |
| UpdateDepartmentAsync | 全量更新部门 | 租户令牌 | PUT |
| UpdateDepartmentIdAsync | 更新部门ID | 租户令牌 | PATCH |
| UnbindDepartmentChatAsync | 解绑部门群 | 租户令牌 | POST |
| DeleteDepartmentByIdAsync | 删除部门 | 租户令牌 | DELETE |

## 函数详细内容

### 获取部门信息

**函数名称**：获取部门信息

**函数签名**：
```csharp
Task<FeishuApiResult<GetDepartmentInfoResult>?> GetDepartmentInfoByIdAsync(
    [Path] string department_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| user_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "name": "产品研发部",
      "department_id": "od-xxx",
      "parent_department_id": "0",
      "leader_user_id": "ou-xxx",
      "member_count": 50,
      "status": {"is_deleted": false}
    }
  }
}
```

**代码示例**：
```csharp
public class DepartmentV3Service
{
    private readonly IFeishuTenantV3Departments _departmentClient;

    public DepartmentV3Service(IFeishuTenantV3Departments departmentClient)
    {
        _departmentClient = departmentClient;
    }

    public async Task GetDepartmentInfoAsync(string departmentId)
    {
        var result = await _departmentClient.GetDepartmentInfoByIdAsync(departmentId);
        if (result?.Code == 0)
        {
            var dept = result.Data?.Department;
            Console.WriteLine($"部门: {dept?.Name}, 成员数: {dept?.MemberCount}");
        }
    }
}
```

---

### 批量获取部门

**函数名称**：批量获取部门

**函数签名**：
```csharp
Task<FeishuApiResult<BatchGetDepartmentRequest>?> GetDepartmentsByIdsAsync(
    [Query("department_ids")] string[] department_ids,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_ids | string[] | ✅ | 部门ID数组 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 获取子部门列表

**函数名称**：获取子部门列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<GetDepartmentInfo>?> GetDepartmentsByParentIdAsync(
    [Path] string department_id,
    [Query("fetch_child")] bool fetch_child = false,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| fetch_child | bool | ⚪ | 是否递归获取所有子部门，默认 `false` |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 获取父部门列表

**函数名称**：获取父部门列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<GetDepartmentInfo>?> GetParentDepartmentsByIdAsync(
    [Query("department_id")] string department_id,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 创建部门

**函数名称**：创建部门

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentCreateUpdateResult>?> CreateDepartmentAsync(
    [Body] DepartmentCreateRequest departmentCreateRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| departmentCreateRequest | DepartmentCreateRequest | ✅ | 创建部门的请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| client_token | string | ⚪ | 幂等判断令牌，避免重复创建 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "department_id": "od-xxx",
      "open_department_id": "od-xxx",
      "name": "技术部"
    }
  }
}
```

---

### 部分更新部门

**函数名称**：部分更新部门

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentCreateUpdateResult>?> UpdatePartDepartmentAsync(
    [Path] string department_id,
    [Body] DepartmentPartUpdateRequest departmentCreateRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| departmentCreateRequest | DepartmentPartUpdateRequest | ✅ | 部分更新请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：仅更新传入的字段，未传入的字段保持不变。

---

### 全量更新部门

**函数名称**：全量更新部门

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentUpdateResult>?> UpdateDepartmentAsync(
    [Path] string department_id,
    [Body] DepartmentUpdateRequest departmentCreateRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| departmentCreateRequest | DepartmentUpdateRequest | ✅ | 全量更新请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：更新指定部门的全部信息，未传入的字段将被清空或设为默认值。

---

### 更新部门ID

**函数名称**：更新部门ID

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateDepartmentIdAsync(
    [Path] string department_id,
    [Body] DepartMentUpdateIdRequest departMentUpdateIdRequest,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 原部门ID |
| departMentUpdateIdRequest | DepartMentUpdateIdRequest | ✅ | 更新部门ID请求体 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：用于更新部门的自定义ID，即 `department_id`。

---

### 解绑部门群

**函数名称**：解绑部门群

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UnbindDepartmentChatAsync(
    [Body] DepartmentRequest departmentRequest,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| departmentRequest | DepartmentRequest | ✅ | 部门请求体 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：将指定部门的部门群转为普通群。

---

### 删除部门

**函数名称**：删除部门

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteDepartmentByIdAsync(
    [Path] string department_id,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```
