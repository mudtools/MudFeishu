# IFeishuTenantV1AIOpticalCharRecognition - 租户AI光学字符识别API

## 功能描述
飞书AI光学字符识别接口，包括识别图片中的文字，按图片中的区域划分，分段返回文本列表。支持租户管理员通过租户访问令牌对图片进行文字识别。

## 参考文档
- [基础图片识别](https://open.feishu.cn/document/server-docs/ai/optical_char_recognition-v1/basic_recognize)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| BasicRecognizeImageAsync | 识别图片中的文字 | TenantAccessToken | POST |

## 函数详细内容

### BasicRecognizeImageAsync
识别图片中的文字

**函数签名**
```csharp
Task<FeishuApiResult<BasicRecognizeImageResult>?> BasicRecognizeImageAsync(
    [Body] BasicRecognizeImageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | BasicRecognizeImageRequest | ✅ | 上传用于AI处理的文件请求体（包含图片base64或URL） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "text_list": [
      { "text": "第一段识别文本", "location": { "x": 10, "y": 20, "w": 100, "h": 30 } },
      { "text": "第二段识别文本", "location": { "x": 10, "y": 60, "w": 120, "h": 30 } }
    ]
  }
}
```

**说明**
- 可识别图片中的文字，按图片中的区域划分，分段返回文本列表
- 文件大小需小于5M

**代码示例**
```csharp
var ocrApi = feishuApp.GetApi<IFeishuTenantV1AIOpticalCharRecognition>();
var request = new BasicRecognizeImageRequest
{
    ImageBase64 = "iVBORw0KGgoAAAANSUhEUgAA..."
};
var result = await ocrApi.BasicRecognizeImageAsync(request);
if (result?.Data?.TextList != null)
{
    foreach (var item in result.Data.TextList)
        Console.WriteLine($"[{item.Location?.X},{item.Location?.Y}] {item.Text}");
}
```
