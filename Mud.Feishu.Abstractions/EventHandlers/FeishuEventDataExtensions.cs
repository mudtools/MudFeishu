// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// <see cref="EventData"/> 事件载荷读取扩展（E-P1-2 双轨收敛）
/// </summary>
/// <remarks>
/// R-E1（AD-3）：SDK 生产路径自本版本起将 <see cref="EventData.Event"/> 统一写入
/// JSON 原文字符串；属性类型保持 <c>object?</c> 仅为源兼容。历史版本 WS 通道写入过
/// <see cref="JsonElement"/>（Clone），宿主若持有旧对象残留或手工构造 EventData，
/// 本扩展统一两种形态的读取口径，下一 major <c>Event</c> 将收敛为 <c>string?</c>。
/// </remarks>
public static class FeishuEventDataExtensions
{
    /// <summary>
    /// 以 JSON 原文字符串读取 <see cref="EventData.Event"/> 载荷
    /// </summary>
    /// <param name="eventData">事件数据（可为 null）</param>
    /// <returns>
    /// <see cref="EventData.Event"/> 为 <see cref="string"/> 时原样返回；
    /// 为 <see cref="JsonElement"/> 时返回 <see cref="JsonElement.GetRawText"/>；
    /// 为 null 或其它类型时返回 null。
    /// </returns>
    public static string? GetEventRawJson(this EventData? eventData)
    {
        return eventData?.Event switch
        {
            string raw => raw,
            JsonElement element => element.ValueKind == JsonValueKind.Undefined ? null : element.GetRawText(),
            _ => null,
        };
    }
}
