// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 事件解析失败原因（受控枚举，用于日志与指标维度）
/// </summary>
public enum FeishuEventDataParseFailure
{
    /// <summary>解析成功</summary>
    None,

    /// <summary>无法确定事件类型（event.type 缺失且根级 type 属保留字或缺失）</summary>
    NoEventType,

    /// <summary>输入形态不支持（非 JSON 对象）</summary>
    UnsupportedShape,
}

/// <summary>
/// 飞书事件数据共享解析器（v1.0 / v2.0 / data 包裹兼容形态的<b>单一真源</b>）
/// </summary>
/// <remarks>
/// <para>
/// R-E1（AD-1）：Webhook 与 WebSocket 两通道此前各持 2 份平行解析实现，v1.0 官方格式
/// （根级 <c>uuid</c>/<c>token</c>/<c>ts</c>、事件类型位于 <c>event.type</c>、app_id/tenant_key 位于
/// <c>event</c> 内）被错误映射，导致 v1 订阅在 Webhook 被空字段 fail-closed 拒绝（E-P0-1）、
/// 在 WS 通道整帧丢弃（E-P0-2）。本类型将解析规则收敛为单一真源。
/// </para>
/// <para>
/// 稳定契约：
/// ① v2.0（含 <c>schema</c> 键）→ 完整 <see cref="FeishuEventHeader"/>，字段逐项透传；
/// ② v1.0 官方形态 → 构造<b>合成 Header</b>（<see cref="FeishuEventHeader.Schema"/> 恒为 null，
/// 字段取自根级 uuid/token/ts 与 event.*，AD-2），根级平铺属性照旧填充；
/// ③ 网关包裹形态（根级含 <c>data</c> 对象）→ 以 data 子树为有效根重走 ①②（AD-4）；
/// ④ <see cref="EventData.Event"/> 恒为 JSON 原文字符串（<see cref="JsonElement.GetRawText"/>），
/// 消费方请使用 <see cref="FeishuEventDataExtensions.GetEventRawJson"/> 读取（AD-3）；
/// ⑤ <see cref="EventData.CreateTime"/> 恒为<b>毫秒</b> long（AD-3/§3.5 启发式）。
/// </para>
/// <para>
/// url_verification 帧不在本解析器处理：Webhook 在 Decryptor 分流（challenge 提取）；
/// WS 走 <see cref="FeishuEventDataParseFailure.NoEventType"/> 丢弃路径。
/// </para>
/// <para>
/// AOT：纯 <see cref="JsonDocument"/>/TryGetProperty 手工解析，无反射序列化，无
/// [RequiresUnreferencedCode]。失败原因枚举为受控维度，可直接入指标标签。
/// </para>
/// </remarks>
public static class FeishuEventDataParser
{
    // v1 根级 type 的保留字：这些值不能作为事件类型回退（url_verification 由 Webhook 分流；
    // event_callback 是 v1 包络自身；data 是包裹形态键）。
    private static readonly HashSet<string> ReservedRootTypes = new(StringComparer.Ordinal)
    {
        "event_callback",
        "url_verification",
        "data",
    };

    /// <summary>
    /// 尝试将飞书事件根元素解析为 <see cref="EventData"/>（v2.0 / v1.0 / data 包裹形态）
    /// </summary>
    /// <param name="root">已解析的 JSON 根元素（调用方负责 <see cref="JsonDocument"/> 生命周期）</param>
    /// <param name="eventData">解析成功时输出事件数据；失败时为 null</param>
    /// <param name="failure">失败原因；成功时为 <see cref="FeishuEventDataParseFailure.None"/></param>
    /// <returns>解析是否成功</returns>
    public static bool TryParse(JsonElement root, out EventData eventData, out FeishuEventDataParseFailure failure)
    {
        eventData = null!;
        failure = FeishuEventDataParseFailure.None;

        if (root.ValueKind != JsonValueKind.Object)
        {
            failure = FeishuEventDataParseFailure.UnsupportedShape;
            return false;
        }

        // AD-4：包裹形态（{"type":"event","data":{...}}）——以 data 子树为有效根重走判别。
        // 仅当根级自身不含 schema/event（即不是直接的 v2/v1 帧）时才下钻，防止误包裹。
        var effectiveRoot = root;
        if (!root.TryGetProperty("schema", out _) &&
            !root.TryGetProperty("event", out _) &&
            root.TryGetProperty("data", out var dataElement) &&
            dataElement.ValueKind == JsonValueKind.Object)
        {
            effectiveRoot = dataElement;
        }

        if (effectiveRoot.ValueKind != JsonValueKind.Object)
        {
            failure = FeishuEventDataParseFailure.UnsupportedShape;
            return false;
        }

        if (effectiveRoot.TryGetProperty("schema", out _))
        {
            eventData = ParseV2(effectiveRoot);
        }
        else
        {
            eventData = ParseV1(effectiveRoot);
        }

        if (string.IsNullOrEmpty(eventData.EventType))
        {
            eventData = null!;
            failure = FeishuEventDataParseFailure.NoEventType;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 解析 v2.0 事件（有效根含 schema 键；逻辑迁移自两通道原 ParseV2Event，含 E9 非数字 create_time 防御）
    /// </summary>
    private static EventData ParseV2(JsonElement root)
    {
        var eventData = new EventData();

        if (root.TryGetProperty("header", out var headerElement))
        {
            var header = new FeishuEventHeader { Schema = "2.0" };

            if (headerElement.TryGetProperty("event_id", out var eventIdElement))
            {
                var eventId = eventIdElement.GetString() ?? string.Empty;
                header.EventId = eventId;
                eventData.EventId = eventId;
            }

            if (headerElement.TryGetProperty("event_type", out var eventTypeElement))
            {
                var eventType = eventTypeElement.GetString() ?? string.Empty;
                header.EventType = eventType;
                eventData.EventType = eventType;
            }

            if (headerElement.TryGetProperty("create_time", out var createTimeElement))
            {
                // E9（R2）：TryGetInt64 仅在 Number 值类型上合法——非数字值静默为 null/0。
                header.CreateTime = createTimeElement.ValueKind == JsonValueKind.String
                    ? createTimeElement.GetString()
                    : createTimeElement.ValueKind == JsonValueKind.Number && createTimeElement.TryGetInt64(out var ct) ? ct.ToString() : null;
                eventData.CreateTime = ParseCreateTime(createTimeElement);
            }

            if (headerElement.TryGetProperty("token", out var tokenElement))
                header.Token = tokenElement.GetString();

            if (headerElement.TryGetProperty("tenant_key", out var tenantKeyElement))
            {
                header.TenantKey = tenantKeyElement.GetString() ?? string.Empty;
                eventData.TenantKey = header.TenantKey;
            }

            if (headerElement.TryGetProperty("app_id", out var appIdElement))
            {
                header.AppId = appIdElement.GetString() ?? string.Empty;
                eventData.AppId = header.AppId;
            }

            eventData.Header = header;
        }

        // 解析 schema（在 header 之外）
        if (root.TryGetProperty("schema", out var schemaElement))
        {
            eventData.Header ??= new FeishuEventHeader();
            eventData.Header.Schema = schemaElement.GetString();
        }

        if (root.TryGetProperty("event", out var eventElement))
        {
            // AD-3：统一为 JSON 原文字符串（GetRawText 复制脱离 JsonDocument 生命周期，
            // 原 WS Clone() 的 JsonElement 逃逸风险随之消失）。
            eventData.Event = eventElement.GetRawText();
        }

        return eventData;
    }

    /// <summary>
    /// 解析 v1.0 事件（官方格式：根级 uuid/token/ts，事件字段在 event 内；兼容 data 包裹形态已由调用方下钻）
    /// </summary>
    private static EventData ParseV1(JsonElement root)
    {
        var eventData = new EventData();

        string? eventId = null;
        if (root.TryGetProperty("uuid", out var uuidElement) && uuidElement.ValueKind == JsonValueKind.String)
            eventId = uuidElement.GetString();
        if (string.IsNullOrEmpty(eventId) && root.TryGetProperty("event_id", out var eventIdElement))
            eventId = eventIdElement.GetString();
        eventData.EventId = eventId ?? string.Empty;

        // 事件类型：官方 v1 位于 event.type；根级 type 是包络（event_callback/url_verification），
        // 仅当不属于保留字时才允许回退（兼容部分网关直接在根级携带真实事件类型的变体）。
        string? eventType = null;
        JsonElement eventElement = default;
        var hasEventObject = root.TryGetProperty("event", out eventElement) && eventElement.ValueKind == JsonValueKind.Object;
        if (hasEventObject &&
            eventElement.TryGetProperty("type", out var innerTypeElement) && innerTypeElement.ValueKind == JsonValueKind.String)
        {
            eventType = innerTypeElement.GetString();
        }
        if (string.IsNullOrEmpty(eventType) &&
            root.TryGetProperty("event_type", out var rootEventTypeElement) && rootEventTypeElement.ValueKind == JsonValueKind.String)
        {
            // 兼容历史变体：根级 event_type（旧实现仅认此位置；官方 v1.0 无此键）
            eventType = rootEventTypeElement.GetString();
        }
        if (string.IsNullOrEmpty(eventType) &&
            root.TryGetProperty("type", out var rootTypeElement) && rootTypeElement.ValueKind == JsonValueKind.String)
        {
            var rootType = rootTypeElement.GetString();
            if (!string.IsNullOrEmpty(rootType) && !ReservedRootTypes.Contains(rootType!))
                eventType = rootType;
        }
        eventData.EventType = eventType ?? string.Empty;

        // tenant_key / app_id：官方 v1 位于 event 内；逐字段回退根级兼容变体
        //（legacy 帧两字段可能分散在 event 与根级，见测试 TryParse_WithLegacyRootEventType）
        if (TryGetStringProperty(hasEventObject ? eventElement : root, "tenant_key", out var tenantKeyValue) ||
            (hasEventObject && TryGetStringProperty(root, "tenant_key", out tenantKeyValue)))
        {
            eventData.TenantKey = tenantKeyValue;
        }

        if (TryGetStringProperty(hasEventObject ? eventElement : root, "app_id", out var appIdValue) ||
            (hasEventObject && TryGetStringProperty(root, "app_id", out appIdValue)))
        {
            eventData.AppId = appIdValue;
        }

        // 根级 token（v1 唯一凭据位置；E-P1-1）
        string? token = null;
        if (root.TryGetProperty("token", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String)
            token = tokenElement.GetString();

        // 根级 ts（秒或浮点秒字符串；E-P2-2）；Header.CreateTime 保留原始字符串（对齐 v2 口径）
        string? headerCreateTime = null;
        if (root.TryGetProperty("ts", out var tsElement))
        {
            eventData.CreateTime = ParseCreateTime(tsElement);
            headerCreateTime = tsElement.ValueKind == JsonValueKind.String
                ? tsElement.GetString()
                : tsElement.TryGetInt64(out var ts) ? ts.ToString() : null;
        }

        // AD-2：合成 Header（Schema 恒为 null，契约见 FeishuEventHeader）。
        eventData.Header = new FeishuEventHeader
        {
            Schema = null,
            EventId = eventData.EventId,
            EventType = eventData.EventType,
            Token = token,
            TenantKey = eventData.TenantKey,
            AppId = eventData.AppId,
            CreateTime = headerCreateTime,
        };

        if (hasEventObject)
        {
            eventData.Event = eventElement.GetRawText();
        }

        return eventData;
    }

    /// <summary>
    /// 解析时间戳字段为<b>毫秒</b> long（§3.5 启发式，E-P2-2 单一口径）：
    /// 数字/整数按位数判别（≥16 位微秒、13~15 位毫秒、≤12 位秒）；
    /// 字符串浮点按秒解析 ×1000；无法解析返回 0。
    /// </summary>
    private static long ParseCreateTime(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var numberValue))
                    return NormalizeTimestamp(numberValue);
                // 非整数 Number（如 1502199207.5）：按秒浮点处理
                return (long)Math.Round(element.GetDouble() * 1000);
            case JsonValueKind.String:
                var raw = element.GetString();
                if (string.IsNullOrEmpty(raw))
                    return 0;
                var trimmed = raw!.Trim();
                if (long.TryParse(trimmed, out var longValue))
                    return NormalizeTimestamp(longValue);
                if (double.TryParse(trimmed, out var doubleValue))
                    return (long)Math.Round(doubleValue * 1000);
                return 0;
            default:
                return 0;
        }
    }

    /// <summary>读取字符串属性（仅 String 值类型命中）</summary>
    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var propertyElement) &&
            propertyElement.ValueKind == JsonValueKind.String)
        {
            value = propertyElement.GetString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>按十进制位数判别时间单位：≥16 位微秒、13~15 位毫秒、≤12 位秒（×1000）</summary>
    private static long NormalizeTimestamp(long value)
    {
        if (value <= 0)
            return 0;
        var digits = (long)Math.Floor(Math.Log10(value)) + 1;
        if (digits >= 16)
            return value / 1000; // 微秒 → 毫秒
        if (digits >= 13)
            return value;        // 已是毫秒
        return value * 1000;     // 秒 → 毫秒
    }
}
