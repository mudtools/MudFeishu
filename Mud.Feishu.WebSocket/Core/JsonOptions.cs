// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 共享的JSON序列化选项
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// 默认的JSON序列化选项（用于序列化场景）
    /// </summary>
    public static JsonSerializerOptions Default => FeishuJsonDefaults.SerializerOptions;

    /// <summary>
    /// 反序列化专用选项（R-06 修正：包含 PropertyNameCaseInsensitive=true，语义准确）。
    /// 使用 getter 实时读取，确保 ConfigureUserResolver 后 TypeInfoResolver 变更能传播。
    /// </summary>
    public static JsonSerializerOptions Deserializer => FeishuJsonDefaults.DeserializerOptions;
}
