# Mud.Feishu.AotVerification

MudFeishu Native AOT 兼容性验证工程

## 用途

此项目用于验证 MudFeishu 库在 .NET Native AOT 环境下的兼容性，确保:
- JSON 序列化/反序列化在 AOT 下正常工作
- protobuf-net 二进制序列化在 AOT 下正常工作  
- HTTP 客户端在 AOT 下正常工作
- 事件处理机制在 AOT 下正常工作

## 构建和验证

```bash
# 构建 AOT 版本
dotnet publish -r win-x64 -c Release /p:PublishAot=true
dotnet publish -r linux-x64 -c Release /p:PublishAot=true

# 运行验证
./bin/Release/net8.0/win-x64/publish/Mud.Feishu.AotVerification
./bin/Release/net8.0/linux-x64/publish/Mud.Feishu.AotVerification
```

## 验证阶段

### 阶段 0 (当前)
- ✅ AOT 基础架构验证
- ✅ 项目结构验证
- ✅ 编译通过验证

### 阶段 1 (待实现)
- JSON 源生成上下文验证
- 用户自定义 Context 合并验证
- 匿名类型修复验证
- Widget 多态序列化验证

### 阶段 2 (待实现)
- protobuf-net AOT 验证
- WebSocket 二进制帧处理验证

### 阶段 3 (待实现)
- 依赖库 AOT 兼容性验证
- 运行时行为和性能验证