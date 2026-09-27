# Microsoft.Agents.AI AOT 适配结论备忘（Phase 0）

> **文档编号**：AI-FD-P0-N1
> **日期**：2026-09-27
> **关联**：[Phase 0 设计 §5/§7](../../.docs/AI/AI-Native-Agent-Phase0-Design.md)（DoD：AotStrictMode 冒烟 + MAF AOT 适配结论备忘）
> **基线**：`Microsoft.Agents.AI` / `Microsoft.Agents.AI.OpenAI` **1.20.0**（nuget.org；`Microsoft.Extensions.AI` 10.9.0 传递）

## 结论

**MAF 1.20.0 在 `-p:AotStrictMode=true` 下净零，`Mud.Feishu.AI` 保留 `IsAotCompatible`（继承根 `Directory.Build.props` 的 net8+ 全局配置），无需剥离或 `[RequiresDynamicCode]` 边界。**

验证命令（与 `scripts/verify-build.ps1` 步骤 3 同口径）：

```
dotnet build Mud.Feishu.AI/Mud.Feishu.AI.csproj -c Release -f net8.0 -p:AotStrictMode=true --no-incremental
```

结果：0 错误、0 `IL2026`、0 `IL3050`、0 `AOT00x`（`--no-incremental` 强制重编译，规避 CoreCompile 时间戳跳过造成的假绿）。

## 版本对齐事实

| 包 | 版本 | 说明 |
| --- | --- | --- |
| `Microsoft.Agents.AI` / `Microsoft.Agents.AI.OpenAI` | 1.20.0 | 唯一版本声明，由 `Tests/Mud.Feishu.AI.Tests` 的 `AgentContractGuards.AgentsAi_PackageReferences_ShouldBeSingleVersion` 锁定 |
| `Microsoft.Extensions.AI`（传递） | 10.9.0 | MAF 1.20.0 依赖面 |
| `Microsoft.Extensions.*`（DI/Logging/Options/Binder/Hosting） | 10.0.11 | 与 MAF 传递依赖对齐，全仓 `Directory.Build.props` 统一钉版（含 net6.0/netstandard2.0 组——原 8.0.x 与 MAF 混图会产生 CS1705/MSB3277） |

TFM 覆盖：MAF 1.20.0 提供 `netstandard2.0` / `net8.0` / `net9.0` / `net10.0` 资产，与本仓四 TFM 完全兼容（net6.0 消费 ns2.0 资产）。

## 运行时 API 面（已验证）

| 设计文档表述 | 实际 API（MAF 1.20.0） |
| --- | --- |
| `chatClient.AsAIAgent(instructions: …)` | `ChatClientExtensions.AsAIAgent(IChatClient, …)` ✔（本实现改用 `new ChatClientAgent(IChatClient, ChatClientAgentOptions, …)` 显式构造） |
| `agent.CreateSessionAsync()` | `AIAgent.CreateSessionAsync(ct) → ValueTask<AgentSession>` ✔ |
| `agent.RunAsync(text, session)` | `RunAsync(string, AgentSession, AgentRunOptions?, CancellationToken) → Task<AgentResponse>`（`.Text` 取文本）✔ |
| `RunCoreAsync` / `RunCoreStreamingAsync` 覆写 | 抽象成员存在，签名 `(IEnumerable<ChatMessage>, AgentSession, AgentRunOptions, CancellationToken)` ✔ |
| `SerializeSessionAsync` / `DeserializeSessionAsync` | 会话 JSON 序列化对存在；`IConversationStore` 存 `JsonElement.GetRawText()` 字符串 ✔ |
| `MaxTurnCount` | **MAF 1.20.0 无对应旋钮**（`AgentRunOptions`/`ChatOptions` 均无 max-turns），未纳入 Options——待 Phase 2 写工具阶段由自有执行链消费（R5：无消费点的配置不进 Options） |
| `MaxHistoryMessages` | `MessageCountingChatReducer(targetCount)`（MEAI 10.9.0，实验性 `MEAI001` 已在 AI csproj 按有意采用抑制）注入 `InMemoryChatHistoryProvider` ✔ |

## 已知边界

1. `MessageCountingChatReducer` 标注 `[Experimental]`（诊断 `MEAI001`）：MEAI 唯一内置消息计数裁剪器，按有意采用抑制；若未来 API 变动，替代路径是自研 `IChatReducer`（接口已稳定）。
2. 流式路径的 token 用量：`AgentResponseUpdate` 不携带 `UsageDetails`，流式 Span 目前不记 token 计数（非流式 Span 已记 `feishu.llm.input/output/total_tokens`）。
3. 会话损坏容错：`DeserializeSessionAsync` 抛 `JsonException` 时按 miss 重建（Phase 0 §8 回滚策略），单测覆盖。
