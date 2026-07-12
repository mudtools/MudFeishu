// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Mud.Feishu.Abstractions.Observability;

/// <summary>
/// 飞书 SDK 分布式追踪源。
/// 统一所有 Feishu 模块（Webhook/WebSocket/Event）的 ActivitySource，便于一次性注册到 OTel SDK。
/// </summary>
public static class FeishuActivitySource
{
    /// <summary>
    /// ActivitySource 名称，遵循 OTel 命名约定。
    /// </summary>
    public const string Name = "Mud.Feishu";

    /// <summary>
    /// ActivitySource 版本。
    /// </summary>
    public const string Version = "3.0.0";

    /// <summary>
    /// 静态 ActivitySource 实例。
    /// </summary>
    public static readonly ActivitySource Instance = new(Name, Version);

    // ── Activity 名称常量 ──

    /// <summary>Webhook 请求处理 Activity</summary>
    public const string ActivityNameWebhookRequest = "Feishu.Webhook.Request";

    /// <summary>事件处理 Activity</summary>
    public const string ActivityNameEventHandling = "Feishu.Event.Handle";

    /// <summary>WebSocket 消息处理 Activity</summary>
    public const string ActivityNameWebSocketMessage = "Feishu.WebSocket.Message";

    /// <summary>WebSocket 连接 Activity</summary>
    public const string ActivityNameWebSocketConnect = "Feishu.WebSocket.Connect";

    // ── Tag 常量 ──

    /// <summary>
    /// OTel 语义约定与 Feishu 自定义标签的常量集合。
    /// </summary>
    public static class Tags
    {
        /// <summary>飞书应用 AppKey（多应用区分维度）</summary>
        public const string AppKey = "feishu.app_key";

        /// <summary>飞书应用 AppId</summary>
        public const string AppId = "feishu.app_id";

        /// <summary>事件类型</summary>
        public const string EventType = "feishu.event.type";

        /// <summary>事件 ID</summary>
        public const string EventId = "feishu.event.id";

        /// <summary>事件处理器类型名</summary>
        public const string HandlerType = "feishu.event.handler_type";

        /// <summary>租户 Key</summary>
        public const string TenantKey = "feishu.tenant_key";

        /// <summary>WebSocket 消息类型</summary>
        public const string MessageType = "feishu.websocket.message_type";

        /// <summary>去重类型</summary>
        public const string DedupType = "feishu.dedup.type";

        /// <summary>去重是否命中</summary>
        public const string DedupHit = "feishu.dedup.hit";

        /// <summary>关联 ID</summary>
        public const string CorrelationId = "feishu.correlation_id";
    }
}
