# 考勤用户设置接口 - FeishuTenantV1AttendanceUserSettings

## 接口名称
**考勤用户设置** - (FeishuTenantV1AttendanceUserSettings)

## 功能描述
考勤用户管理接口主要实现了修改用户人脸识别信息、批量查询用户人脸识别信息以及上传下载用户人脸识别照片。

## 参考文档
- [飞书开放平台 - 考勤用户设置文档](https://open.feishu.cn/document/server-docs/attendance-v1/user_setting/modify)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| ModifyUserSettingAsync | 修改用户设置 | 租户令牌 | POST |
| QueryUserSettingAsync | 查询用户设置 | 租户令牌 | GET |
| UploadFileAsync | 上传人脸照片 | 租户令牌 | POST |
| DownloadFileAsync | 下载人脸照片 | 租户令牌 | GET |

---

## 函数详细内容

### ModifyUserSettingAsync - 修改用户设置

修改授权内员工的用户设置信息，包括人脸照片文件ID。修改用户人脸识别信息目前只支持API方式修改，管理后台已无法修改。

#### 函数签名

```csharp
Task<FeishuApiResult<UserSettingsResult>?> ModifyUserSettingAsync(
    [Body] ModifyUserFacialRecognitionRequest userFacialRecognitionRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userFacialRecognitionRequest | ModifyUserFacialRecognitionRequest | ✅ | 修改用户人脸识别信息请求体 | - |
| ├─ user_id | string | ✅ | 用户ID | "ou_abc123" |
| ├─ file_id | string | ✅ | 人脸照片文件ID | "file_xxx" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_id": "ou_abc123",
    "file_id": "file_xxx",
    "updated_at": 1704067200
  }
}
```

#### 说明
- 人脸识别信息只能通过API修改
- 人脸照片需要先通过 `UploadFileAsync` 上传

#### 代码示例

```csharp
// 修改用户人脸照片
var request = new ModifyUserFacialRecognitionRequest
{
    UserId = "ou_abc123",
    FileId = "file_vxxxx"  // 通过UploadFileAsync上传获取
};

var result = await feishuClient.ModifyUserSettingAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine("用户人脸信息修改成功");
}
```

---

### QueryUserSettingAsync - 查询用户设置

批量查询授权内员工的用户设置信息，包括人脸照片文件ID、人脸照片更新时间。

#### 函数签名

```csharp
Task<FeishuApiResult<UserSettingsResult>?> QueryUserSettingAsync(
    [Body] UserSettingsQueryRequest userSettingsQueryRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userSettingsQueryRequest | UserSettingsQueryRequest | ✅ | 批量查询用户人脸识别信息请求体 | - |
| ├─ user_ids | string[] | ✅ | 用户ID列表 | ["ou_xxx"] |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_settings": [
      {
        "user_id": "ou_abc123",
        "file_id": "file_xxx",
        "file_updated_at": 1704067200
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 批量查询用户人脸信息
var request = new UserSettingsQueryRequest
{
    UserIds = new[] { "ou_abc123", "ou_def456" }
};

var result = await feishuClient.QueryUserSettingAsync(request);
if (result?.Code == 0)
{
    foreach (var setting in result.Data?.UserSettings ?? [])
    {
        Console.WriteLine($"用户: {setting.UserId}");
        Console.WriteLine($"人脸照片ID: {setting.FileId}");
        Console.WriteLine($"更新时间: {DateTimeOffset.FromUnixTimeSeconds(setting.FileUpdatedAt):yyyy-MM-dd}");
    }
}
```

---

### UploadFileAsync - 上传人脸照片

上传用户人脸照片并获取文件ID，对应小程序端的人脸录入功能。

#### 函数签名

```csharp
Task<FeishuApiResult<UserFileUploadResult>?> UploadFileAsync(
    [FormContent] UploadFileRequest uploadFileRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| uploadFileRequest | UploadFileRequest | ✅ | 需要上传的用户人脸照片文件 | - |
| ├─ file_name | string | ✅ | 文件名 | "face.jpg" |
| ├─ file_content | byte[] | ✅ | 文件内容 | - |
| ├─ file_type | string | ✅ | 文件类型：image/jpeg等 | "image/jpeg" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_id": "file_vxxxx",
    "file_name": "face.jpg",
    "file_size": 102400
  }
}
```

#### 说明
- 支持的照片格式：JPEG、PNG
- 照片建议尺寸：不小于200x200像素
- 上传后可通过返回的file_id修改用户人脸信息

#### 代码示例

```csharp
// 上传用户人脸照片
byte[] fileBytes = await File.ReadAllBytesAsync("path/to/face.jpg");

var request = new UploadFileRequest
{
    FileName = "face.jpg",
    FileContent = fileBytes,
    FileType = "image/jpeg"
};

var result = await feishuClient.UploadFileAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"文件上传成功，ID: {result.Data?.FileId}");
    
    // 使用返回的file_id修改用户人脸信息
    await feishuClient.ModifyUserSettingAsync(new ModifyUserFacialRecognitionRequest
    {
        UserId = "ou_abc123",
        FileId = result.Data?.FileId
    });
}
```

---

### DownloadFileAsync - 下载人脸照片

通过文件ID下载用户的头像照片文件。

#### 函数签名

```csharp
Task<byte[]?> DownloadFileAsync(
    [Path] string file_id,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| file_id | string | ✅ | 需要下载的用户人脸照片文件ID | "file_vxxxx" |

#### 响应

返回文件的字节数组。

#### 代码示例

```csharp
// 下载用户人脸照片
byte[]? fileBytes = await feishuClient.DownloadFileAsync("file_vxxxx");
if (fileBytes != null)
{
    await File.WriteAllBytesAsync("downloaded_face.jpg", fileBytes);
    Console.WriteLine("文件下载成功");
}
```

#### 完整流程示例

```csharp
// 完整的更新用户人脸照片流程
public async Task UpdateUserFacePhotoAsync(string userId, string imagePath)
{
    // 1. 上传人脸照片
    byte[] fileBytes = await File.ReadAllBytesAsync(imagePath);
    var uploadRequest = new UploadFileRequest
    {
        FileName = Path.GetFileName(imagePath),
        FileContent = fileBytes,
        FileType = "image/jpeg"
    };

    var uploadResult = await _feishuClient.UploadFileAsync(uploadRequest);
    if (uploadResult?.Code != 0)
    {
        throw new Exception("文件上传失败");
    }

    // 2. 修改用户人脸信息
    var modifyRequest = new ModifyUserFacialRecognitionRequest
    {
        UserId = userId,
        FileId = uploadResult.Data?.FileId
    };

    var modifyResult = await _feishuClient.ModifyUserSettingAsync(modifyRequest);
    if (modifyResult?.Code == 0)
    {
        Console.WriteLine("用户人脸照片更新成功");
    }
}
```
