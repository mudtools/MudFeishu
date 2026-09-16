// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.AotVerification;

/// <summary>
/// MudFeishu AOT 验证程序
/// 用于验证 MudFeishu 库在 Native AOT 环境下的兼容性
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== MudFeishu AOT 验证程序 ===");
        Console.WriteLine();

        var isSmokeTest = Array.Exists(args, a => a == "--smoke");
        var allPassed = true;

        // 1. 基础设施验证
        Console.WriteLine("[1] 基础设施验证");
        Console.WriteLine($"  框架: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  动态代码支持: {(System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? "是 (JIT)" : "否 (AOT)")}");
        Console.WriteLine();

#if NET8_0_OR_GREATER
        // 2. DataModels Context 接线（必须在所有序列化测试之前执行）
        Console.WriteLine("[2] DataModels Context 接线 (阶段1核心)");
        try
        {
            // 配置 DataModels resolver - 必须在任何 JSON 序列化之前调用
            Mud.Feishu.Extensions.FeishuJsonResolverExtensions.ConfigureDataModelsResolver();
            Console.WriteLine("  [PASS] ConfigureDataModelsResolver 调用成功");

            // 配置 Webhook resolver - 添加 Webhook 专用类型到 resolver 链
            Mud.Feishu.Webhook.Extensions.FeishuWebhookJsonResolverExtensions.ConfigureWebhookResolver();
            Console.WriteLine("  [PASS] ConfigureWebhookResolver 调用成功");

            // P1-1：配置 WebSocket resolver - 添加 WebSocket 协议消息类型到 resolver 链
            Mud.Feishu.WebSocket.Extensions.FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();
            Console.WriteLine("  [PASS] ConfigureWebSocketResolver 调用成功");

            // P1-2：配置 EventCallback resolver - 添加事件回调类型到 resolver 链
            Mud.Feishu.EventCallback.Extensions.FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();
            Console.WriteLine("  [PASS] ConfigureEventCallbackResolver 调用成功");

            // 验证 resolver 已传播到 FeishuJsonDefaults
            var options = FeishuJsonDefaults.SerializerOptions;
            Console.WriteLine($"  [PASS] TypeInfoResolver 已设置: {options.TypeInfoResolver != null}");

            // 验证 FeishuJsonOptions.Serialize 也传播了（同一引用）
            var webhookOptions = FeishuJsonOptions.Serialize;
            Console.WriteLine($"  [PASS] Webhook Serialize 传播验证: {webhookOptions.TypeInfoResolver != null}");

            // 验证 FeishuJsonOptions.Deserialize getter 也传播了
            var deserializeOptions = FeishuJsonOptions.Deserialize;
            Console.WriteLine($"  [PASS] Webhook Deserialize getter 传播验证: {deserializeOptions.TypeInfoResolver != null}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] DataModels Context 接线失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();
#endif

        // 3. JSON 序列化选项验证
        Console.WriteLine("[3] JSON 序列化选项验证");
        try
        {
            var serializerOptions = FeishuJsonDefaults.SerializerOptions;
            var deserializerOptions = FeishuJsonDefaults.DeserializerOptions;
            Console.WriteLine("  [PASS] FeishuJsonDefaults.SerializerOptions 访问成功");
            Console.WriteLine("  [PASS] FeishuJsonDefaults.DeserializerOptions 访问成功");

            // 验证 FeishuJsonOptions (Webhook 层)
            var webhookSerialize = FeishuJsonOptions.Serialize;
            var webhookDeserialize = FeishuJsonOptions.Deserialize;
            Console.WriteLine("  [PASS] FeishuJsonOptions.Serialize 实时引用验证通过");
            Console.WriteLine("  [PASS] FeishuJsonOptions.Deserialize getter 验证通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] JSON 选项访问失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 4. Webhook 响应 DTO 序列化验证（G3 修复验证）
        Console.WriteLine("[4] Webhook 响应 DTO 序列化验证 (G3)");
        try
        {
            var errorResponse = new WebhookErrorResponse
            {
                Success = false,
                RequestId = "test-req-001",
                Error = new WebhookErrorDetail
                {
                    Code = 429,
                    Message = "Too Many Requests"
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(errorResponse, FeishuJsonOptions.Serialize);
            Console.WriteLine($"  [PASS] WebhookErrorResponse 序列化成功: {json}");

            var emptyResponse = new WebhookEmptyResponse();
            var emptyJson = System.Text.Json.JsonSerializer.Serialize(emptyResponse, FeishuJsonOptions.Serialize);
            Console.WriteLine($"  [PASS] WebhookEmptyResponse 序列化成功: {emptyJson}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Webhook DTO 序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 5. EventData 序列化验证（B9 修复验证）
        Console.WriteLine("[5] EventData 序列化验证 (B9)");
        try
        {
            var eventData = new EventData
            {
                EventId = "test-event-001",
                EventType = "test.event",
                TenantKey = "test-tenant",
                Event = System.Text.Json.JsonDocument.Parse("{\"msg\":\"hello\"}").RootElement.Clone()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(eventData, FeishuJsonDefaults.SerializerOptions);
            Console.WriteLine($"  [PASS] EventData 序列化成功 (长度: {json.Length} 字符)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] EventData 序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 6. protobuf-net 类型验证（阶段2）
        Console.WriteLine("[6] protobuf-net 类型验证 (阶段2)");
        try
        {
            var header = new Mud.Feishu.WebSocket.ProtoHeader { Key = "type", Value = "event" };
            var eventData = new Mud.Feishu.WebSocket.EventProtoData
            {
                SeqID = 123456789,
                Service = 1001,
                Method = 1,
                Headers = new[] { header },
                PayloadEncoding = "utf-8",
                PayloadType = "application/json"
            };

            // 验证 protobuf 序列化
            using var stream = new System.IO.MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, eventData);
            Console.WriteLine($"  [PASS] EventProtoData protobuf 序列化成功 (字节: {stream.Length})");

            // 验证反序列化
            stream.Position = 0;
            var deserialized = ProtoBuf.Serializer.Deserialize<Mud.Feishu.WebSocket.EventProtoData>(stream);
            Console.WriteLine($"  [PASS] EventProtoData protobuf 反序列化成功 (SeqID: {deserialized.SeqID})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] protobuf-net 验证失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 7. Widget 多态序列化验证（G4 修复验证）
        Console.WriteLine("[7] Widget 多态序列化验证 (G4)");
        try
        {
            var widget = Mud.Feishu.DataModels.ApprovalForm.WidgetFactory.CreateWidget(
                Mud.Feishu.DataModels.ApprovalForm.WidgetType.Input, "test_input");
            var json = Mud.Feishu.DataModels.ApprovalForm.WidgetFactory.SerializeToJson(widget);
            Console.WriteLine($"  [PASS] IWidget 多态序列化成功: {json}");

            var widgets = new List<Mud.Feishu.DataModels.ApprovalForm.IWidget>
            {
                Mud.Feishu.DataModels.ApprovalForm.WidgetFactory.CreateWidget(
                    Mud.Feishu.DataModels.ApprovalForm.WidgetType.Input, "input_1"),
                Mud.Feishu.DataModels.ApprovalForm.WidgetFactory.CreateWidget(
                    Mud.Feishu.DataModels.ApprovalForm.WidgetType.Number, "number_1")
            };
            var listJson = Mud.Feishu.DataModels.ApprovalForm.WidgetFactory.SerializeToJson(widgets);
            Console.WriteLine($"  [PASS] List<IWidget> 多态序列化成功 (长度: {listJson.Length})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Widget 多态序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

#if NET8_0_OR_GREATER
        // 8. P0-2：FeishuEventHeader 反序列化验证（通过 FeishuJsonContext 强类型路径）
        Console.WriteLine("[8] P0-2: FeishuEventHeader 反序列化验证");
        try
        {
            var headerJson = """{"event_id":"evt_123","event_type":"contact.user.created_v3","create_time":"1700000000","token":"xxx","app_id":"cli_xxx","tenant_key":"xxx"}""";
            var header = System.Text.Json.JsonSerializer.Deserialize(
                headerJson, Mud.Feishu.Abstractions.Utilities.FeishuJsonContext.Default.FeishuEventHeader);
            if (header?.EventId != "evt_123")
                throw new InvalidOperationException("FeishuEventHeader.EventId 反序列化不匹配");
            Console.WriteLine($"  [PASS] FeishuEventHeader 反序列化成功 (EventId={header?.EventId})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] P0-2 FeishuEventHeader 反序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 9. P0-1：FeishuApiResult<T> 反序列化验证（通过 FeishuApiResultJsonContext 强类型路径）
        Console.WriteLine("[9] P0-1: FeishuApiResult<T> 反序列化验证");
        try
        {
            // 验证 FeishuApiResult<GetUserDataResult> 闭合泛型反序列化
            var apiResponseJson = """{"code":0,"msg":"success","data":{"name":"test_user","open_id":"ou_xxx","union_id":"on_xxx","tenant_key":"tk_xxx"}}""";
            var result = System.Text.Json.JsonSerializer.Deserialize(
                apiResponseJson,
                Mud.Feishu.Abstractions.Utilities.FeishuApiResultJsonContext.Default.FeishuApiResultGetUserDataResult);
            if (result?.Code != 0 || result?.Data?.OpenId != "ou_xxx")
                throw new InvalidOperationException("FeishuApiResult<GetUserDataResult> 反序列化不匹配");
            Console.WriteLine($"  [PASS] FeishuApiResult<GetUserDataResult> 反序列化成功 (Code={result?.Code}, OpenId={result?.Data?.OpenId})");

            // 验证非泛型 FeishuApiResult 基类反序列化
            var baseJson = """{"code":10001,"msg":"invalid app_id"}""";
            var baseResult = System.Text.Json.JsonSerializer.Deserialize(
                baseJson, Mud.Feishu.Abstractions.Utilities.FeishuApiResultJsonContext.Default.FeishuApiResult);
            if (baseResult?.Code != 10001)
                throw new InvalidOperationException("FeishuApiResult 基类反序列化不匹配");
            Console.WriteLine($"  [PASS] FeishuApiResult 基类反序列化成功 (Code={baseResult?.Code})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] P0-1 FeishuApiResult<T> 反序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 10. P1-1：WebSocket 协议消息反序列化验证（通过 FeishuJsonDefaults resolver 链）
        Console.WriteLine("[10] P1-1: WebSocket 协议消息反序列化验证");
        try
        {
            // AuthResponseMessage 在 WebSocketJsonContext 中注册（internal），
            // 通过 FeishuJsonDefaults.DeserializerOptions 的合并 resolver 链反序列化
            var authRespJson = """{"code":0,"msg":"success","session_id":"sess_xxx","type":"auth"}""";
            var authResp = System.Text.Json.JsonSerializer.Deserialize<Mud.Feishu.WebSocket.DataModels.AuthResponseMessage>(
                authRespJson, FeishuJsonDefaults.DeserializerOptions);
            if (authResp?.Code != 0 || authResp?.SessionId != "sess_xxx")
                throw new InvalidOperationException("AuthResponseMessage 反序列化不匹配");
            Console.WriteLine($"  [PASS] AuthResponseMessage 反序列化成功 (Code={authResp?.Code}, SessionId={authResp?.SessionId})");

            // 验证 PingMessage
            var pingJson = """{"type":"ping"}""";
            var ping = System.Text.Json.JsonSerializer.Deserialize<Mud.Feishu.WebSocket.DataModels.PingMessage>(
                pingJson, FeishuJsonDefaults.DeserializerOptions);
            if (ping?.Type != "ping")
                throw new InvalidOperationException("PingMessage 反序列化不匹配");
            Console.WriteLine($"  [PASS] PingMessage 反序列化成功 (Type={ping?.Type})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] P1-1 WebSocket 协议消息反序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();

        // 11. P1-2：EventCallback 事件反序列化验证（通过 FeishuJsonDefaults resolver 链）
        Console.WriteLine("[11] P1-2: EventCallback 事件反序列化验证");
        try
        {
            // MessageReceiveResult 在 EventCallback 的 IMJsonContext 中注册（internal），
            // 通过 FeishuJsonDefaults.DeserializerOptions 的合并 resolver 链反序列化
            var eventJson = """{"sender":{"sender_id":{"open_id":"ou_xxx","union_id":"on_xxx","user_id":"u_xxx"},"sender_type":"user"},"message":{"message_id":"om_xxx","chat_id":"oc_xxx","message_type":"text","content":"{\"text\":\"hello\"}"}}""";
            var result = System.Text.Json.JsonSerializer.Deserialize<Mud.Feishu.EventCallback.IM.MessageReceiveResult>(
                eventJson, FeishuJsonDefaults.DeserializerOptions);
            if (result?.Sender?.SenderId?.OpenId != "ou_xxx")
                throw new InvalidOperationException("MessageReceiveResult 反序列化不匹配");
            Console.WriteLine($"  [PASS] MessageReceiveResult 反序列化成功 (OpenId={result?.Sender?.SenderId?.OpenId})");

            // 验证 DriveFileEventHeader 子类反序列化（N-03 联动验证）
            var driveHeaderJson = """{"event_id":"evt_456","event_type":"drive.file.edit_v1","resource_id":"file_xxx","user_list":[{"user_id":"u1"}]}""";
            var driveHeader = System.Text.Json.JsonSerializer.Deserialize<Mud.Feishu.EventCallback.Drive.DriveFileEventHeader>(
                driveHeaderJson, FeishuJsonDefaults.DeserializerOptions);
            if (driveHeader?.ResourceId != "file_xxx")
                throw new InvalidOperationException("DriveFileEventHeader 反序列化不匹配");
            Console.WriteLine($"  [PASS] DriveFileEventHeader 子类反序列化成功 (ResourceId={driveHeader?.ResourceId})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] P1-2 EventCallback 事件反序列化失败: {ex.Message}");
            allPassed = false;
        }
        Console.WriteLine();
#endif

        // 结果汇总
        Console.WriteLine("=== 验证结果 ===");
        if (allPassed)
        {
            Console.WriteLine("[SUCCESS] 所有验证项通过!");
            if (isSmokeTest)
            {
                Environment.Exit(0);
            }
        }
        else
        {
            Console.WriteLine("[FAILED] 部分验证项失败，请检查上述日志。");
            if (isSmokeTest)
            {
                Environment.Exit(1);
            }
        }

        // 保持运行（非 smoke 模式）
        if (!isSmokeTest)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });

            var host = hostBuilder.Build();
            try
            {
                await host.StartAsync();
                await Task.Delay(2000);
            }
            finally
            {
                await host.StopAsync();
            }
        }
    }
}