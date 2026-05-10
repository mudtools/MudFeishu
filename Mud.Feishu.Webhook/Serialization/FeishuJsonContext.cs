// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook.Serialization;


/// <summary>
/// 飞书 Webhook JSON 序列化上下文
/// 支持源生成，兼容 Native AOT 部署
/// </summary>
/// <remarks>
/// 使用说明：
/// 1. 此类使用 .NET 源生成器生成高效的序列化代码
/// 2. 替代传统的反射序列化，提升性能并支持 AOT
/// 3. 使用方式：JsonSerializer.Deserialize&lt;T&gt;(json, FeishuJsonContext.Default.T)
/// </remarks>
[JsonSerializable(typeof(FeishuWebhookRequest))]
[JsonSerializable(typeof(EventVerificationRequest))]
[JsonSerializable(typeof(EventVerificationResponse))]
[JsonSerializable(typeof(EventData))]
internal partial class FeishuJsonContext : JsonSerializerContext
{
}
