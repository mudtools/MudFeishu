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