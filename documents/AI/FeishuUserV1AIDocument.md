# IFeishuUserV1AIDocument - 用户AI文档识别API

## 功能描述
飞书AI文档接口包括智能文档处理（支持17种证件识别），如：简历信息解析、机动车发票识别、健康证识别、中国护照识别等能力。支持用户通过用户访问令牌进行各类证件与文档的智能识别。

## 参考文档
- [简历信息解析](https://open.feishu.cn/document/ai/document_ai-v1/resume/parse)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| ParseResumeAsync | 识别文件中的简历信息 | UserAccessToken | POST |
| RecognizeHkmMainlandTravelPermitAsync | 识别港澳居民来往内地通行证 | UserAccessToken | POST |
| RecognizeTwMainlandTravelPermitAsync | 识别台湾居民来往大陆通行证 | UserAccessToken | POST |
| RecognizeChinesePassportAsync | 识别中国护照 | UserAccessToken | POST |
| RecognizeBankCardAsync | 识别银行卡 | UserAccessToken | POST |
| RecognizeVehicleLicenseAsync | 识别行驶证 | UserAccessToken | POST |
| RecognizeTrainInvoiceAsync | 识别火车票 | UserAccessToken | POST |
| RecognizeTaxiInvoiceAsync | 识别出租车发票 | UserAccessToken | POST |
| RecognizeIdCardAsync | 识别身份证 | UserAccessToken | POST |
| RecognizeFoodProduceLicenseAsync | 识别食品生产许可证 | UserAccessToken | POST |
| RecognizeFoodManageLicenseAsync | 识别食品经营许可证 | UserAccessToken | POST |
| RecognizeDrivingLicenseAsync | 识别驾驶证 | UserAccessToken | POST |
| RecognizeVatInvoiceAsync | 识别增值税发票 | UserAccessToken | POST |
| RecognizeBusinessLicenseAsync | 识别营业执照 | UserAccessToken | POST |
| RecognizeContractFieldAsync | 识别合同字段 | UserAccessToken | POST |
| RecognizeBusinessCardAsync | 识别名片 | UserAccessToken | POST |
| RecognizeVehicleInvoiceAsync | 识别机动车发票 | UserAccessToken | POST |
| RecognizeHealthCertificateAsync | 识别健康证 | UserAccessToken | POST |

## 函数详细内容

### ParseResumeAsync
识别文件中的简历信息

**函数签名**
```csharp
Task<FeishuApiResult<ParseResumeResult>?> ParseResumeAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "name": "张三",
    "phone": "138xxxx8888",
    "email": "zhangsan@example.com",
    "education": "本科"
  }
}
```

**说明**
- 简历信息解析接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M
- 使用 user_access_token 时，仅可识别当前授权用户上传的文件

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\resume.pdf" };
var result = await aiDocApi.ParseResumeAsync(request);
Console.WriteLine($"姓名: {result?.Data?.Name}");
```

---

### RecognizeHkmMainlandTravelPermitAsync
识别文件中的港澳居民来往内地通行证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeHkmMainlandTravelPermitResult>?> RecognizeHkmMainlandTravelPermitAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "张三", "permit_number": "H12345678" }
}
```

**说明**
- 港澳居民来往内地通行证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\permit.jpg" };
var result = await aiDocApi.RecognizeHkmMainlandTravelPermitAsync(request);
Console.WriteLine($"通行证号: {result?.Data?.PermitNumber}");
```

---

### RecognizeTwMainlandTravelPermitAsync
识别文件中的台湾居民来往大陆通行证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeTwMainlandTravelPermitResult>?> RecognizeTwMainlandTravelPermitAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "张三", "permit_number": "T12345678" }
}
```

**说明**
- 台湾居民来往大陆通行证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\tw_permit.jpg" };
var result = await aiDocApi.RecognizeTwMainlandTravelPermitAsync(request);
Console.WriteLine($"通行证号: {result?.Data?.PermitNumber}");
```

---

### RecognizeChinesePassportAsync
识别文件中的中国护照信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeChinesePassportResult>?> RecognizeChinesePassportAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "张三", "passport_number": "E12345678", "nationality": "CHN" }
}
```

**说明**
- 中国护照识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\passport.jpg" };
var result = await aiDocApi.RecognizeChinesePassportAsync(request);
Console.WriteLine($"护照号: {result?.Data?.PassportNumber}");
```

---

### RecognizeBankCardAsync
识别文件中的银行卡信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeBankCardResult>?> RecognizeBankCardAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "card_number": "6222********1234", "bank_name": "招商银行" }
}
```

**说明**
- 银行卡识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\bankcard.jpg" };
var result = await aiDocApi.RecognizeBankCardAsync(request);
Console.WriteLine($"卡号: {result?.Data?.CardNumber}");
```

---

### RecognizeVehicleLicenseAsync
识别文件中的行驶证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeVehicleLicenseResult>?> RecognizeVehicleLicenseAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "plate_number": "粤B12345", "vehicle_type": "小型普通客车" }
}
```

**说明**
- 行驶证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\vehicle_license.jpg" };
var result = await aiDocApi.RecognizeVehicleLicenseAsync(request);
Console.WriteLine($"车牌号: {result?.Data?.PlateNumber}");
```

---

### RecognizeTrainInvoiceAsync
识别文件中的火车票信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeTrainInvoiceResult>?> RecognizeTrainInvoiceAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "from_station": "北京南", "to_station": "上海虹桥", "ticket_number": "T123456789" }
}
```

**说明**
- 火车票识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\train.jpg" };
var result = await aiDocApi.RecognizeTrainInvoiceAsync(request);
Console.WriteLine($"车次: {result?.Data?.TicketNumber}");
```

---

### RecognizeTaxiInvoiceAsync
识别文件中的出租车发票信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeTaxiInvoiceResult>?> RecognizeTaxiInvoiceAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "invoice_code": "144001800000", "amount": "38.00" }
}
```

**说明**
- 出租车发票识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\taxi.jpg" };
var result = await aiDocApi.RecognizeTaxiInvoiceAsync(request);
Console.WriteLine($"金额: {result?.Data?.Amount}");
```

---

### RecognizeIdCardAsync
识别文件中的身份证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeIdCardResult>?> RecognizeIdCardAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "张三", "id_number": "110101199001011234", "address": "北京市朝阳区..." }
}
```

**说明**
- 身份证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\idcard.jpg" };
var result = await aiDocApi.RecognizeIdCardAsync(request);
Console.WriteLine($"身份证号: {result?.Data?.IdNumber}");
```

---

### RecognizeFoodProduceLicenseAsync
识别文件中的食品生产许可证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeFoodProduceLicenseResult>?> RecognizeFoodProduceLicenseAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "license_number": "SC12345678901234", "company_name": "某某食品有限公司" }
}
```

**说明**
- 食品生产许可证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\food_produce.jpg" };
var result = await aiDocApi.RecognizeFoodProduceLicenseAsync(request);
Console.WriteLine($"许可证号: {result?.Data?.LicenseNumber}");
```

---

### RecognizeFoodManageLicenseAsync
识别文件中的食品经营许可证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeFoodManageLicenseResult>?> RecognizeFoodManageLicenseAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "license_number": "JY12345678901234", "company_name": "某某餐饮有限公司" }
}
```

**说明**
- 食品经营许可证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\food_manage.jpg" };
var result = await aiDocApi.RecognizeFoodManageLicenseAsync(request);
Console.WriteLine($"许可证号: {result?.Data?.LicenseNumber}");
```

---

### RecognizeDrivingLicenseAsync
识别文件中的驾驶证信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeDrivingLicenseResult>?> RecognizeDrivingLicenseAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "张三", "license_number": "110101199001011234", "vehicle_type": "C1" }
}
```

**说明**
- 驾驶证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\driving_license.jpg" };
var result = await aiDocApi.RecognizeDrivingLicenseAsync(request);
Console.WriteLine($"驾驶证号: {result?.Data?.LicenseNumber}");
```

---

### RecognizeVatInvoiceAsync
识别文件中的增值税发票信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeVatInvoiceResult>?> RecognizeVatInvoiceAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "invoice_code": "011001800000", "invoice_number": "12345678", "amount": "1000.00" }
}
```

**说明**
- 增值税发票识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\vat_invoice.jpg" };
var result = await aiDocApi.RecognizeVatInvoiceAsync(request);
Console.WriteLine($"发票号码: {result?.Data?.InvoiceNumber}");
```

---

### RecognizeBusinessLicenseAsync
识别文件中的营业执照信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeBusinessLicenseResult>?> RecognizeBusinessLicenseAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "company_name": "某某科技有限公司", "credit_code": "91110108MA01ABCDEF", "legal_person": "李四" }
}
```

**说明**
- 营业执照识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\business_license.jpg" };
var result = await aiDocApi.RecognizeBusinessLicenseAsync(request);
Console.WriteLine($"公司名称: {result?.Data?.CompanyName}");
```

---

### RecognizeContractFieldAsync
识别文件中的合同字段信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeContractFieldResult>?> RecognizeContractFieldAsync(
    [FormContent] ContractFileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | ContractFileUploadRequest | ✅ | 上传用于AI处理的合同文件请求体（含待抽取的字段配置） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "fields": [ { "key": "甲方", "value": "某某公司" }, { "key": "金额", "value": "100000" } ] }
}
```

**说明**
- 合同字段识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M
- 需使用 `ContractFileUploadRequest` 并指定待抽取字段

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new ContractFileUploadRequest
{
    FilePath = @"C:\temp\contract.pdf",
    ExtractFields = new List<string> { "甲方", "金额" }
};
var result = await aiDocApi.RecognizeContractFieldAsync(request);
if (result?.Data?.Fields != null)
    foreach (var f in result.Data.Fields)
        Console.WriteLine($"{f.Key}: {f.Value}");
```

---

### RecognizeBusinessCardAsync
识别文件中的名片信息

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeBusinessCardResult>?> RecognizeBusinessCardAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "王五", "title": "产品经理", "company": "某某科技", "phone": "139xxxx0000", "email": "wangwu@example.com" }
}
```

**说明**
- 名片识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\card.jpg" };
var result = await aiDocApi.RecognizeBusinessCardAsync(request);
Console.WriteLine($"姓名: {result?.Data?.Name}, 公司: {result?.Data?.Company}");
```

---

### RecognizeVehicleInvoiceAsync
识别文件中的机动车发票

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeVehicleInvoiceResult>?> RecognizeVehicleInvoiceAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "invoice_code": "161001800000", "invoice_number": "12345678", "vehicle_price": "150000.00" }
}
```

**说明**
- 机动车发票识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\vehicle_invoice.jpg" };
var result = await aiDocApi.RecognizeVehicleInvoiceAsync(request);
Console.WriteLine($"发票号: {result?.Data?.InvoiceNumber}");
```

---

### RecognizeHealthCertificateAsync
识别文件中的健康证

**函数签名**
```csharp
Task<FeishuApiResult<RecognizeHealthCertificateResult>?> RecognizeHealthCertificateAsync(
    [FormContent] FileUploadRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileUploadRequest | ✅ | 上传用于AI处理的文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": { "name": "张三", "certificate_number": "HC12345678", "valid_until": "2027-01-01" }
}
```

**说明**
- 健康证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别
- 文件大小需要小于30M

**代码示例**
```csharp
var aiDocApi = feishuApp.GetApi<IFeishuUserV1AIDocument>();
var request = new FileUploadRequest { FilePath = @"C:\temp\health_cert.jpg" };
var result = await aiDocApi.RecognizeHealthCertificateAsync(request);
Console.WriteLine($"健康证号: {result?.Data?.CertificateNumber}");
```
