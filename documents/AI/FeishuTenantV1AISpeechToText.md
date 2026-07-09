# IFeishuTenantV1AISpeechToText - 租户AI语音转文字API

## 功能描述
飞书AI语音转文字接口，包括将音频文件转换为文字内容。支持租户管理员通过租户访问令牌进行语音文件或流式语音的识别。

## 参考文档
- [文件识别](https://open.feishu.cn/document/server-docs/ai/speech_to_text-v1/file_recognize)
- [流式识别](https://open.feishu.cn/document/server-docs/ai/speech_to_text-v1/stream_recognize)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| FileRecognizeSpeechAsync | 识别语音文件 | TenantAccessToken | POST |
| StreamRecognizeSpeechAsync | 识别流式语音 | TenantAccessToken | POST |

## 函数详细内容

### FileRecognizeSpeechAsync
识别语音文件

**函数签名**
```csharp
Task<FeishuApiResult<FileRecognizeSpeechResult>?> FileRecognizeSpeechAsync(
    [Body] FileRecognizeSpeechRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | FileRecognizeSpeechRequest | ✅ | 上传用于AI处理的语音文件请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "recognition_text": "这是一段识别出来的语音文本内容。"
  }
}
```

**说明**
- 语音文件识别接口，上传整段语音文件进行一次性识别
- 接口适合 60 秒以内音频识别

**代码示例**
```csharp
var speechApi = feishuApp.GetApi<IFeishuTenantV1AISpeechToText>();
var request = new FileRecognizeSpeechRequest
{
    SpeechBase64 = "UklGRiQAAABXQVZFZm10IBAAAANAAA...",
    Format = "wav",
    SampleRate = 16000
};
var result = await speechApi.FileRecognizeSpeechAsync(request);
Console.WriteLine($"识别结果: {result?.Data?.RecognitionText}");
```

---

### StreamRecognizeSpeechAsync
识别流式语音

**函数签名**
```csharp
Task<FeishuApiResult<StreamRecognizeSpeechResult>?> StreamRecognizeSpeechAsync(
    [Body] StreamRecognizeSpeechRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | StreamRecognizeSpeechRequest | ✅ | 上传用于AI处理的流式语音请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "recognition_text": "这是实时返回的部分识别文本。",
    "is_final": true
  }
}
```

**说明**
- 语音流式接口，将整个音频文件分片进行传入模型
- 能够实时返回数据，建议每个音频分片的大小为 100-200ms

**代码示例**
```csharp
var speechApi = feishuApp.GetApi<IFeishuTenantV1AISpeechToText>();
var request = new StreamRecognizeSpeechRequest
{
    SpeechBase64 = "UklGRiQAAABXQVZFZm10IBAAAANAAA...",
    Format = "wav",
    SampleRate = 16000,
    Seq = 1
};
var result = await speechApi.StreamRecognizeSpeechAsync(request);
Console.WriteLine($"流式识别: {result?.Data?.RecognitionText} (最终: {result?.Data?.IsFinal})");
```
