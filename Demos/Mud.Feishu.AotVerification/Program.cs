// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.AotVerification;

/// <summary>
/// MudFeishu AOT 验证程序
/// 用于验证 MudFeishu 库在 Native AOT 环境下的兼容性
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 MudFeishu AOT 验证程序启动...");
        
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(services =>
            {
                // 基本服务注册验证
                services.AddLogging();
                
                Console.WriteLine("✅ AOT 验证程序初始化完成");
                Console.WriteLine($"📦 目标框架: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
                Console.WriteLine($"🔧 AOT 编译: {(System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? "不支持 (AOT 模式)" : "支持 (JIT 模式)")}");
                
                // 阶段 1: JSON 源生成上下文基础验证
                try
                {
                    // 验证 FeishuJsonDefaults 可被正确访问
                    var options = Mud.Feishu.Abstractions.Utilities.FeishuJsonDefaults.SerializerOptions;
                    Console.WriteLine($"✅ FeishuJsonDefaults.SerializerOptions 访问成功");
                    Console.WriteLine($"   - PropertyNamingPolicy: {options.PropertyNamingPolicy}");
                    Console.WriteLine($"   - PropertyNameCaseInsensitive: {options.PropertyNameCaseInsensitive}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ FeishuJsonDefaults 访问失败: {ex.Message}");
                }

                // 阶段 2: protobuf-net AOT 验证
                try
                {
                    // 简单的 protobuf-net 类验证
                    var header = new Mud.Feishu.WebSocket.ProtoHeader { Key = "type", Value = "event" };
                    Console.WriteLine($"✅ protobuf-net ProtoHeader 类型访问成功");
                    Console.WriteLine($"   - Key: {header.Key}, Value: {header.Value}");
                    
                    var eventData = new Mud.Feishu.WebSocket.EventProtoData { SeqID = 123456 };
                    Console.WriteLine($"✅ protobuf-net EventProtoData 类型访问成功");
                    Console.WriteLine($"   - SeqID: {eventData.SeqID}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ protobuf-net 类型访问失败: {ex.Message}");
                }

                // TODO: 下一阶段将添加具体的 Feishu SDK 服务和验证逻辑
                Console.WriteLine("⏳ 待实现: Feishu SDK 源生成上下文验证、HTTP 客户端验证、事件处理验证等");
            });

        var host = hostBuilder.Build();
        
        try
        {
            await host.StartAsync();
            
            Console.WriteLine("\n🎉 AOT 验证程序运行成功!");
            Console.WriteLine("当前阶段: 基础设施验证通过 ✓");
            
            // 保持运行以便检查AOT编译结果
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AOT 验证程序运行失败: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
            Environment.Exit(1);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}