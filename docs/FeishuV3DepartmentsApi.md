# 飞书部门管理 API 文档

## 概述

飞书组织机构部门是指企业组织架构树上的某一个节点。在部门内部，可添加用户作为部门成员，也可添加新的部门作为子部门。

**版本**: v3  
**基础URL**: `https://open.feishu.cn/open-apis/contact/v3/departments`  
**完整文档**: [https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview](https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview)

---

## 创建部门

### 接口名称
在通讯录内创建一个部门。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments
```

### 方法
POST

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| client_token | string | Query | 否 | 幂等性判断token | "create_dept_123" |
| departmentCreateRequest | DepartmentCreateRequest | Body | 是 | 创建部门的请求体 | 见下方示例 |

#### DepartmentCreateRequest 结构

```json
{
  "name": "技术部",
  "i18n_name": {
    "zh_cn": "技术部",
    "en_us": "Technology Department"
  },
  "parent_department_id": "0",
  "leader_user_id": "ou_123456789",
  "order": "1",
  "create_group_chat": true,
  "leaders": [
    {
      "user_id": "ou_123456789",
      "type": 1
    }
  ],
  "group_chat_employee_types": [1, 2],
  "department_id": "custom_tech_dept_001",
  "unit_ids": ["unit_001"],
  "department_hrbps": ["ou_987654321"]
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "name": "技术部",
      "i18n_name": {
        "zh_cn": "技术部",
        "en_us": "Technology Department"
      },
      "parent_department_id": "0",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "open_department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "leader_user_id": "ou_123456789",
      "order": "1",
      "member_count": 0,
      "status": {
        "is_deleted": false
      },
      "leaders": [
        {
          "user_id": "ou_123456789",
          "type": 1
        }
      ],
      "department_hrbps": ["ou_987654321"],
      "unit_ids": ["unit_001"]
    }
  }
}
```

#### 错误响应
```json
{
  "code": 99991401,
  "msg": "access_token invalid"
}
```

### 代码示例

```csharp
var createRequest = new DepartmentCreateRequest
{
    Name = "技术部",
    I18nName = new I18nName
    {
        ZhCn = "技术部",
        EnUs = "Technology Department"
    },
    ParentDepartmentId = "0", // 根部门
    LeaderUserId = "ou_123456789",
    Order = "1",
    CreateGroupChat = true,
    Leaders = new List<DepartmentLeader>
    {
        new DepartmentLeader
        {
            UserId = "ou_123456789",
            Type = 1
        }
    },
    GroupChatEmployeeTypes = new List<int> { 1, 2 },
    DepartmentId = "custom_tech_dept_001",
    UnitIds = new List<string> { "unit_001" },
    DepartmentHrbps = new List<string> { "ou_987654321" }
};

var result = await _departmentsApi.CreateDepartmentAsync(
    tenant_access_token,
    createRequest,
    client_token: "create_dept_123"
);

if (result.Code == 0)
{
    var departmentId = result.Data?.Department?.DepartmentId;
    Console.WriteLine($"部门创建成功，ID: {departmentId}");
}
```

### 说明
- 部门名称不可包含斜杠（/），不能与存量部门名称重复
- `parent_department_id` 为 "0" 时表示在根部门下创建
- `department_id_type` 支持：department_id、open_department_id、custom_id
- `group_chat_employee_types`: 1-正式员工, 2-实习生, 3-外包, 4-劳务, 5-顾问

---

## 部分更新部门

### 接口名称
更新指定部门的部分信息，包括名称、父部门、排序以及负责人等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}
```

### 方法
PATCH

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_id | string | Path | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| departmentCreateRequest | DepartmentPartUpdateRequest | Body | 是 | 部分更新部门的请求体 | 见下方示例 |

#### DepartmentPartUpdateRequest 结构

```json
{
  "name": "新技术部",
  "i18n_name": {
    "zh_cn": "新技术部",
    "en_us": "New Technology Department"
  },
  "parent_department_id": "od-123456789",
  "leader_user_id": "ou_987654321",
  "order": "2",
  "create_group_chat": false,
  "leaders": [
    {
      "user_id": "ou_987654321",
      "type": 1
    }
  ],
  "group_chat_employee_types": [1],
  "department_hrbps": ["ou_111111111"]
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "name": "新技术部",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "parent_department_id": "od-123456789",
      "leader_user_id": "ou_987654321",
      "order": "2"
    }
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "department not found"
}
```

### 代码示例

```csharp
var updateRequest = new DepartmentPartUpdateRequest
{
    Name = "新技术部",
    I18nName = new I18nName
    {
        ZhCn = "新技术部",
        EnUs = "New Technology Department"
    },
    ParentDepartmentId = "od-123456789",
    LeaderUserId = "ou_987654321",
    Order = "2",
    Leaders = new List<DepartmentLeader>
    {
        new DepartmentLeader
        {
            UserId = "ou_987654321",
            Type = 1
        }
    },
    DepartmentHrbps = new List<string> { "ou_111111111" }
};

var result = await _departmentsApi.UpdatePartDepartmentAsync(
    tenant_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("部门部分更新成功");
}
```

### 说明
- 部分更新接口只会更新传递的字段，未传递的字段保持原值
- 不支持更新部门的自定义ID，需要使用专门的更新ID接口

---

## 完整更新部门

### 接口名称
更新指定部门的信息，包括名称、父部门以及负责人等信息。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}
```

### 方法
PUT

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_id | string | Path | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| departmentCreateRequest | DepartmentUpdateRequest | Body | 是 | 完整更新部门的请求体 | 见下方示例 |

#### DepartmentUpdateRequest 结构

```json
{
  "name": "完整更新的技术部",
  "i18n_name": {
    "zh_cn": "完整更新的技术部",
    "en_us": "Fully Updated Technology Department"
  },
  "parent_department_id": "od-123456789",
  "leader_user_id": "ou_987654321",
  "order": "3",
  "create_group_chat": true,
  "leaders": [
    {
      "user_id": "ou_987654321",
      "type": 1
    }
  ],
  "group_chat_employee_types": [1, 2, 3]
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "name": "完整更新的技术部",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c"
    }
  }
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid request parameters"
}
```

### 代码示例

```csharp
var updateRequest = new DepartmentUpdateRequest
{
    Name = "完整更新的技术部",
    I18nName = new I18nName
    {
        ZhCn = "完整更新的技术部",
        EnUs = "Fully Updated Technology Department"
    },
    ParentDepartmentId = "od-123456789",
    LeaderUserId = "ou_987654321",
    Order = "3",
    CreateGroupChat = true
};

var result = await _departmentsApi.UpdateDepartmentAsync(
    tenant_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("部门完整更新成功");
}
```

### 说明
- PUT 接口会完整更新部门信息，未传递的字段可能会被重置为默认值
- 建议使用 PATCH 接口进行部分更新，更加安全

---

## 更新部门ID

### 接口名称
更新部门的自定义ID，即 department_id。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}/update_department_id
```

### 方法
PATCH

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_id | string | Path | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| departMentUpdateIdRequest | DepartMentUpdateIdRequest | Body | 是 | 更新部门ID的请求体 | 见下方示例 |

#### DepartMentUpdateIdRequest 结构

```json
{
  "new_department_id": "new_custom_tech_dept_001"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Department ID already exists"
}
```

### 代码示例

```csharp
var updateIdRequest = new DepartMentUpdateIdRequest
{
    NewDepartmentId = "new_custom_tech_dept_001"
};

var result = await _departmentsApi.UpdateDepartmentIdAsync(
    tenant_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    updateIdRequest
);

if (result.Code == 0)
{
    Console.WriteLine("部门ID更新成功");
}
```

### 说明
- 新的自定义部门ID必须在企业内唯一
- 更新部门ID不会影响其他部门信息

---

## 解绑部门群聊

### 接口名称
将指定部门的部门群转为普通群。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/unbind_department_chat
```

### 方法
POST

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| departmentRequest | DepartmentRequest | Body | 是 | 部门标识信息 | 见下方示例 |

#### DepartmentRequest 结构

```json
{
  "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "Department chat not found"
}
```

### 代码示例

```csharp
var departmentRequest = new DepartmentRequest
{
    DepartmentId = "od-4e6789c92a3c8e02dbe89d3f9b87c"
};

var result = await _departmentsApi.UnbindDepartmentChatAsync(
    tenant_access_token,
    departmentRequest
);

if (result.Code == 0)
{
    Console.WriteLine("部门群聊解绑成功");
}
```

### 说明
- 解绑后，原部门群聊将变为普通群聊
- 群聊成员关系不会改变
- 操作不可逆，请谨慎使用

---

## 获取单个部门信息

### 接口名称
获取单个部门信息，包括部门名称、ID、父部门、负责人、状态以及成员个数等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}
```

### 方法
GET

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| department_id | string | Path | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "name": "技术部",
      "i18n_name": {
        "zh_cn": "技术部",
        "en_us": "Technology Department"
      },
      "parent_department_id": "0",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "open_department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "leader_user_id": "ou_123456789",
      "chat_id": "oc_xxxxxxxxx",
      "order": "1",
      "member_count": 25,
      "primary_member_count": 20,
      "status": {
        "is_deleted": false
      },
      "leaders": [
        {
          "user_id": "ou_123456789",
          "type": 1
        }
      ],
      "department_hrbps": ["ou_987654321"],
      "group_chat_employee_types": [1, 2],
      "unit_ids": ["unit_001"]
    }
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "Department not found"
}
```

### 代码示例

```csharp
var result = await _departmentsApi.GetDepartmentInfoByIdAsync(
    user_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c"
);

if (result.Code == 0)
{
    var department = result.Data?.Department;
    Console.WriteLine($"部门名称：{department?.Name}");
    Console.WriteLine($"成员数量：{department?.MemberCount}");
    Console.WriteLine($"主要成员：{department?.PrimaryMemberCount}");
}
```

### 说明
- `member_count`: 部门总成员数量
- `primary_member_count`: 主要成员数量（通常为正式员工）
- `department_hrbps`: 部门HRBP列表

---

## 批量获取部门信息

### 接口名称
获取单个部门信息，包括部门名称、ID、父部门、负责人、状态以及成员个数等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/batch
```

### 方法
GET

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_ids | string[] | Query | 是 | 部门ID列表 | ["od-123", "od-456"] |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "技术部",
        "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
        "member_count": 25,
        "status": {
          "is_deleted": false
        }
      },
      {
        "name": "产品部",
        "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87d",
        "member_count": 15,
        "status": {
          "is_deleted": false
        }
      }
    ]
  }
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid department_ids"
}
```

### 代码示例

```csharp
var departmentIds = new string[] 
{ 
    "od-4e6789c92a3c8e02dbe89d3f9b87c", 
    "od-4e6789c92a3c8e02dbe89d3f9b87d" 
};

var result = await _departmentsApi.GetDepartmentsByIdsAsync(
    tenant_access_token,
    departmentIds
);

if (result.Code == 0)
{
    foreach (var department in result.Data?.Items ?? [])
    {
        Console.WriteLine($"部门：{department.Name}，成员：{department.MemberCount}");
    }
}
```

### 说明
- 最多支持查询50个部门
- 查询结果按请求顺序返回

---

## 获取子部门列表

### 接口名称
查询指定部门下的子部门列表，列表内包含部门的名称、ID、父部门、负责人以及状态等信息。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}/children
```

### 方法
GET

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_id | string | Path | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| fetch_child | bool | Query | 否 | 是否递归获取子部门 | false |
| page_size | int | Query | 否 | 分页大小 | 10 |
| page_token | string | Query | 否 | 分页标记 | "" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "前端组",
        "department_id": "od-frontend-001",
        "parent_department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
        "member_count": 8,
        "status": {
          "is_deleted": false
        }
      },
      {
        "name": "后端组",
        "department_id": "od-backend-001",
        "parent_department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
        "member_count": 12,
        "status": {
          "is_deleted": false
        }
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "Department not found"
}
```

### 代码示例

```csharp
var result = await _departmentsApi.GetDepartmentsByParentIdAsync(
    tenant_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    fetch_child: false,
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"找到 {result.Data?.Items?.Count} 个子部门");
    foreach (var dept in result.Data?.Items ?? [])
    {
        Console.WriteLine($"子部门：{dept.Name}，成员：{dept.MemberCount}");
    }
}
```

### 说明
- `fetch_child=true` 时会递归获取所有层级的子部门
- 支持分页查询，使用 `page_token` 获取下一页
- 部门ID为 "0" 时获取根部门的子部门

---

## 获取父部门列表

### 接口名称
递归获取指定部门的父部门信息，包括部门名称、ID、负责人以及状态等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/parent
```

### 方法
GET

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| department_id | string | Query | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| page_size | int | Query | 否 | 分页大小 | 10 |
| page_token | string | Query | 否 | 分页标记 | "" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "技术部",
        "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
        "parent_department_id": "0",
        "leader_user_id": "ou_123456789",
        "member_count": 25
      },
      {
        "name": "公司",
        "department_id": "od-root-001",
        "parent_department_id": "0",
        "leader_user_id": "ou_admin_001",
        "member_count": 100
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "Department not found"
}
```

### 代码示例

```csharp
var result = await _departmentsApi.GetParentDepartmentsByIdAsync(
    tenant_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"部门层级路径：");
    foreach (var dept in result.Data?.Items ?? [])
    {
        Console.WriteLine($"-> {dept.Name} ({dept.DepartmentId})");
    }
}
```

### 说明
- 从直接父部门开始，逐级向上返回，直到根部门
- 返回结果按层级从下到上排序
- 支持分页查询

---

## 搜索部门

### 接口名称
以用户身份通过部门名称关键词查询可见部门的信息，包括部门的 ID、父部门、负责人以及状态等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/search
```

### 方法
POST

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| searchRequest | SearchRequest | Body | 是 | 搜索请求 | 见下方示例 |
| page_size | int | Query | 否 | 分页大小 | 10 |
| page_token | string | Query | 否 | 分页标记 | "" |
| user_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |

#### SearchRequest 结构

```json
{
  "query": "技术"
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "技术部",
        "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
        "parent_department_id": "0",
        "leader_user_id": "ou_123456789",
        "member_count": 25,
        "status": {
          "is_deleted": false
        }
      },
      {
        "name": "技术支持部",
        "department_id": "od-tech-support-001",
        "parent_department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
        "leader_user_id": "ou_456789123",
        "member_count": 10,
        "status": {
          "is_deleted": false
        }
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid query parameter"
}
```

### 代码示例

```csharp
var searchRequest = new SearchRequest
{
    Query = "技术"
};

var result = await _departmentsApi.SearchDepartmentsAsync(
    user_access_token,
    searchRequest,
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"搜索到 {result.Data?.Items?.Count} 个部门");
    foreach (var dept in result.Data?.Items ?? [])
    {
        Console.WriteLine($"部门：{dept.Name}，成员：{dept.MemberCount}");
    }
}
```

### 说明
- 仅匹配部门名称，不支持国际化名称匹配
- 只能搜索用户有权限查看的部门
- 支持模糊搜索

---

## 删除部门

### 接口名称
从通讯录中删除指定的部门。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}
```

### 方法
DELETE

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| department_id | string | Path | 是 | 部门ID | "od-4e6789c92a3c8e02dbe89d3f9b87c" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 403,
  "msg": "No permission to delete department"
}
```

### 代码示例

```csharp
var result = await _departmentsApi.DeleteDepartmentByIdAsync(
    user_access_token,
    "od-4e6789c92a3c8e02dbe89d3f9b87c"
);

if (result.Code == 0)
{
    Console.WriteLine("部门删除成功");
}
```

### 说明
- 删除部门会同时删除其所有子部门
- 删除前请确认部门内没有成员
- 删除操作不可恢复，请谨慎使用

---

## 错误码说明

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 0 | 成功 | - |
| 99991401 | access_token 无效或已过期 | 重新获取 access_token |
| 99991400 | 参数错误 | 检查请求参数格式和必填项 |
| 403 | 无权限 | 检查应用权限和用户权限 |
| 404 | 部门不存在 | 确认 department_id 是否正确 |
| 400 | 请求参数无效 | 检查参数值是否符合规范 |
| 429 | 请求频率超限 | 降低请求频率，使用限流策略 |
| 500 | 服务器内部错误 | 稍后重试或联系技术支持 |

---

## 最佳实践

### 1. 部门层级管理
```csharp
public async Task<DepartmentTree> BuildDepartmentTreeAsync(string rootDepartmentId = "0")
{
    var tree = new DepartmentTree();
    
    async Task BuildTreeRecursive(string parentId, DepartmentNode parentNode)
    {
        var result = await _departmentsApi.GetDepartmentsByParentIdAsync(
            _token, 
            parentId, 
            fetch_child: false
        );
        
        if (result.Code == 0 && result.Data?.Items != null)
        {
            foreach (var dept in result.Data.Items)
            {
                var node = new DepartmentNode
                {
                    Id = dept.DepartmentId,
                    Name = dept.Name,
                    MemberCount = dept.MemberCount
                };
                
                parentNode.Children.Add(node);
                await BuildTreeRecursive(dept.DepartmentId, node);
            }
        }
    }
    
    var rootNode = new DepartmentNode { Id = rootDepartmentId, Name = "Root" };
    await BuildTreeRecursive(rootDepartmentId, rootNode);
    
    return tree;
}
```

### 2. 批量操作优化
```csharp
public async Task<List<GetDepartmentInfo>> GetAllDepartmentsAsync()
{
    var allDepartments = new List<GetDepartmentInfo>();
    var departmentIds = await GetAllDepartmentIdsAsync();
    
    // 分批处理，每次最多50个
    const int batchSize = 50;
    for (int i = 0; i < departmentIds.Count; i += batchSize)
    {
        var batch = departmentIds.Skip(i).Take(batchSize).ToArray();
        var result = await _departmentsApi.GetDepartmentsByIdsAsync(_token, batch);
        
        if (result.Code == 0 && result.Data?.Items != null)
        {
            allDepartments.AddRange(result.Data.Items);
        }
        
        // 避免限流
        await Task.Delay(100);
    }
    
    return allDepartments;
}
```

### 3. 缓存策略
```csharp
public async Task<GetDepartmentInfo> GetDepartmentWithCacheAsync(string departmentId)
{
    var cacheKey = $"department:{departmentId}";
    
    if (_cache.TryGetValue(cacheKey, out GetDepartmentInfo cachedDepartment))
    {
        return cachedDepartment;
    }
    
    var result = await _departmentsApi.GetDepartmentInfoByIdAsync(_token, departmentId);
    if (result.Code == 0 && result.Data?.Department != null)
    {
        var department = result.Data.Department;
        _cache.Set(cacheKey, department, TimeSpan.FromMinutes(30));
        return department;
    }
    
    throw new Exception($"获取部门信息失败: {departmentId}");
}
```

### 4. 权限检查
```csharp
public async Task<bool> HasDepartmentPermissionAsync(string departmentId, string userId)
{
    try
    {
        var result = await _departmentsApi.GetDepartmentInfoByIdAsync(_token, departmentId);
        if (result.Code == 0 && result.Data?.Department != null)
        {
            var dept = result.Data.Department;
            
            // 检查是否为部门负责人
            if (dept.LeaderUserId == userId)
                return true;
                
            // 检查是否在负责人列表中
            if (dept.Leaders.Any(l => l.UserId == userId))
                return true;
        }
    }
    catch
    {
        // 权限检查失败
    }
    
    return false;
}
```

---

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2025-11-01 | 初始版本，支持部门基本CRUD操作 |

---

## 支持与反馈

如果您在使用过程中遇到问题，请通过以下方式获取帮助：

1. 查看 [飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview)
2. 提交问题到项目的 Issues
3. 联系技术支持团队

---

*最后更新时间: 2025-11-20*