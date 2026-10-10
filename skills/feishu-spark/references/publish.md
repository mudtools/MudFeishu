# 妙搭发布与未策展能力（spark/publish.md）

## 为什么发布不在工具面
妙搭的**发布**与**素材上传**接口入参是**本地文件**（`[FormContent]`：HTML 代码包、图标文件）：
- `UploadHtmlCodeAndReleaseAsync`（上传并发布 HTML 代码包）
- `UploadAppIconAsync`（上传应用图标）

模型没有文件系统，工具面无法表达"从用户机器上传某个文件"这一动作；强行策展会得到一个
**永远无法成功**的工具。同理未策展：
- `upload_storage` / `download_storage`（含 `byte[]`，二进制会击穿上下文）；
- `execute_sql`（任意 SQL 执行，无法机械校验表归属，是比 `feishu.api_call` 更宽的越权通道）；
- `batch_update_table_records`（与 `update_table_records` 重叠，且约束反直觉）。

## 前置链
发布前先备好两件事：① `spark.get_app_visibility` 确认可用范围（别把内部应用发成全公开）；
② `spark.query_table_records` 确认数据表内容符合预期。发布动作本身在本主题给出的两条路径里完成。

## 宿主的集成路径（发布怎么做）
1. **控制台路径**（推荐给用户）：模型完成 `spark.create_app` / 数据表写入后，把
   `online_url`（来自 `spark.list_apps` 或 `spark.create_app` 的结果）给用户，由用户在妙搭控制台发布。
2. **宿主自主发布**：宿主在自己的服务端（有文件系统）用 SDK 直接调用
   `IFeishuUserV1SparkApp.UploadHtmlCodeAndReleaseAsync`，与工具面解耦。

## 避坑
- 发布前不做范围确认是最常见的翻车点：内部应用一旦 `scope=Public` 即对全员可见。
- 发布后：`spark.get_app_analytics` 观察访问量是否有异常增长；异常时及时用
  `spark.update_app_visibility` 收紧范围。
- 用户要求"发布应用 / 上传图标 / 执行 SQL"时：说明工具面**不提供**这些能力，
  并给出控制台路径；**不要**改用 `feishu.api_call` 之类兜底工具绕过策展边界。
- 不要因为"工具面没有发布工具"就断言"应用不能发布"——发布走宿主/控制台，与工具面边界无关。

## 示例
「把刚才建的应用发布出去」→ 先 `spark.get_app_visibility` 确认范围（内部使用就保持 `Tenant`/`Range`）
→ 把 `online_url` 交给用户，由用户在妙搭控制台完成发布。