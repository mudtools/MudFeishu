# 租户V3单位管理 - FeishuTenantV3Unit

## 接口名称
**租户V3单位管理 - (IFeishuTenantV3Unit)**

## 功能描述
本接口提供飞书通讯录单位（Unit）的管理功能，适用于租户应用场景。支持单位的创建、修改、部门绑定/解绑、查询和删除等操作。

通讯录的单位用于代表企业中的"子公司"、"分支机构"这类组织实体。例如，企业下存在负责不同业务的两家子公司，那么可以在同一个租户内，为两家子公司分别创建对应的单位资源。目前单位资源的主要作用是在部分用户权限上实现"子公司"级别的权限隔离。

## 参考文档
- [飞书官方文档 - 单位概述](https://open.feishu.cn/document/server-docs/contact-v3/unit/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateUnitAsync | 创建单位 | 租户令牌 | POST |
| UpdateUnitAsync | 更新单位 | 租户令牌 | PATCH |
| BindDepartmentAsync | 绑定部门 | 租户令牌 | POST |
| UnBindDepartmentAsync | 解绑部门 | 租户令牌 | POST |
| GetDepartmentListAsync | 获取单位部门列表 | 租户令牌 | GET |
| GetUnitInfoAsync | 获取单位详情 | 租户令牌 | GET |
| GetUnitListAsync | 获取单位列表 | 租户令牌 | GET |
| DeleteUnitByIdAsync | 删除单位 | 租户令牌 | DELETE |

## 函数详细内容

### 创建单位

**函数名称**：创建单位

**函数签名**：
```csharp
Task<FeishuApiResult<UnitCreateResult>?> CreateUnitAsync(
    [Body] UnitInfoRequest groupInfoRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| groupInfoRequest | UnitInfoRequest | ✅ | 单位信息请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "xxx",
      "name": "北京分公司",
      "type": 1
    }
  }
}
```

**说明**：创建一个单位，用于代表子公司或分支机构。

**代码示例**：
```csharp
public class UnitService
{
    private readonly IFeishuTenantV3Unit _unitClient;

    public UnitService(IFeishuTenantV3Unit unitClient)
    {
        _unitClient = unitClient;
    }

    public async Task CreateBranchUnitAsync()
    {
        var request = new UnitInfoRequest
        {
            Name = "上海分公司",
            Type = 1
        };

        var result = await _unitClient.CreateUnitAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"单位创建成功，ID: {result.Data?.Unit?.UnitId}");
        }
    }
}
```

---

### 更新单位

**函数名称**：更新单位

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateUnitAsync(
    [Path] string unit_id,
    [Body] UnitNameUpdateRequest nameUpdateRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| unit_id | string | ✅ | 单位ID |
| nameUpdateRequest | UnitNameUpdateRequest | ✅ | 单位名称更新请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：修改指定单位的名字。

---

### 绑定部门

**函数名称**：绑定部门

**函数签名**：
```csharp
Task<FeishuApiResult<UnitCreateResult>?> BindDepartmentAsync(
    [Body] UnitBindDepartmentRequest unitBindDepartment,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| unitBindDepartment | UnitBindDepartmentRequest | ✅ | 部门与单位的绑定关系请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "xxx",
      "name": "北京分公司"
    }
  }
}
```

**说明**：
- 建立部门与单位的绑定关系
- 一个部门同时只能绑定一个单位
- 单个单位可关联的部门数量上限为1,000
- 同一个部门只能关联一个单位

**代码示例**：
```csharp
public async Task BindDepartmentToUnitAsync(string unitId, string departmentId)
{
    var request = new UnitBindDepartmentRequest
    {
        UnitId = unitId,
        DepartmentId = departmentId
    };

    var result = await _unitClient.BindDepartmentAsync(request);
    if (result?.Code == 0)
    {
        Console.WriteLine("部门绑定成功");
    }
}
```

---

### 解绑部门

**函数名称**：解绑部门

**函数签名**：
```csharp
Task<FeishuApiResult<UnitCreateResult>?> UnBindDepartmentAsync(
    [Body] UnitBindDepartmentRequest unitBindDepartment,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| unitBindDepartment | UnitBindDepartmentRequest | ✅ | 部门与单位的绑定关系请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "xxx"
    }
  }
}
```

**说明**：解除部门与单位的绑定关系。

---

### 获取单位部门列表

**函数名称**：获取单位部门列表

**函数签名**：
```csharp
Task<FeishuApiResult<UnitDepartmentListResult>?> GetDepartmentListAsync(
    [Query("unit_id")] string unit_id,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| unit_id | string | ✅ | 单位ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "departments": [
      {
        "department_id": "od-xxx",
        "name": "技术部"
      }
    ]
  }
}
```

**说明**：获取单位绑定的部门列表。

---

### 获取单位详情

**函数名称**：获取单位详情

**函数签名**：
```csharp
Task<FeishuApiResult<UnitInfoResult>?> GetUnitInfoAsync(
    [Path] string unit_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| unit_id | string | ✅ | 单位ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "xxx",
      "name": "北京分公司",
      "type": 1
    }
  }
}
```

**说明**：获取指定单位的信息，包括单位ID、名字、类型。

---

### 获取单位列表

**函数名称**：获取单位列表

**函数签名**：
```csharp
Task<FeishuApiResult<UnitListDataResult>?> GetUnitListAsync(
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "units": [
      {
        "unit_id": "xxx",
        "name": "北京分公司",
        "type": 1
      },
      {
        "unit_id": "yyy",
        "name": "上海分公司",
        "type": 1
      }
    ]
  }
}
```

**说明**：获取当前租户内的单位列表。列表内主要包含各单位的ID、名字、类型信息。

---

### 删除单位

**函数名称**：删除单位

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteUnitByIdAsync(
    [Path] string unit_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| unit_id | string | ✅ | 需删除的单位ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除指定单位。
