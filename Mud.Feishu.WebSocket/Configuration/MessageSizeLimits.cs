// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;


/// <summary>
/// 消息大小限制配置
/// </summary>
public class MessageSizeLimits
{
    /// <summary>
    /// 最大文本消息大小（字符数），默认为1MB
    /// </summary>
    public int MaxTextMessageSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// 最大二进制消息大小（字节），默认为10MB
    /// </summary>
    public long MaxBinaryMessageSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 最大文本消息字节数（UTF-8 编码后）。0 表示按 3 × <see cref="MaxTextMessageSize"/> 自动推导。
    /// </summary>
    /// <remarks>
    /// P1-4 修复引入：文本发送侧此前按<b>字符数</b>校验，而接收侧（分片重组）按<b>字节数</b>比较同一配置项，
    /// 二进制发送则完全无上限校验。<see cref="MaxTextMessageSize"/> 的"字符"语义已在 4 份文档中契约化，
    /// 直接改为字节属破坏性变更，故新增本字段表达字节维度。
    /// <para>
    /// UTF-8 对 UTF-16 字符的最坏展开为 3 字节/字符，因此默认值（0 → 3 × <see cref="MaxTextMessageSize"/>）
    /// 与旧实现的"最大合法消息"完全等价，属<b>放宽</b>而非收紧：现有一切合法消息继续通过。
    /// </para>
    /// <para>取值范围：0 ≤ 值；负值会在 <see cref="FeishuWebSocketOptions.Validate"/> 中抛错。</para>
    /// </remarks>
    public int MaxTextMessageBytes { get; set; } = 0;

    /// <summary>
    /// 解析生效的文本字节上限（供发送/接收统一调用，保证收发同源）。
    /// </summary>
    /// <returns>
    /// 配置值大于 0 时返回配置值；否则返回 <c>3 × <see cref="MaxTextMessageSize"/></c>，
    /// 并以 <see cref="int.MaxValue"/> 为上限饱和（P2-1 修复）。
    /// </returns>
    /// <remarks>
    /// P2-1 修复：此前为 <c>MaxTextMessageSize * 3</c> 的裸 <see cref="int"/> 乘法——当
    /// <see cref="MaxTextMessageSize"/> 超过 <c>int.MaxValue / 3</c>（715,827,882）时溢出为负数，
    /// 使"消息大小校验"变成 <c>buffer.Length &gt; 负数</c> 恒真 ⇒ **文本发送全失败 + 分片文本全丢弃**。
    /// <para>
    /// 现改为 <see cref="long"/> 中间量 + 饱和到 <see cref="int.MaxValue"/>（返回类型保持 <c>int</c>
    /// 以免破坏公共签名）。同时 <see cref="FeishuWebSocketOptions.Validate"/> 已补
    /// <c>MaxTextMessageSize ≤ 10MB</c> 上界 ⇒ 该溢出路径在启动期即被拦截，本方法只作纵深防御。
    /// </para>
    /// </remarks>
    public int ResolveMaxTextMessageBytes()
    {
        if (MaxTextMessageBytes > 0)
        {
            return MaxTextMessageBytes;
        }

        var derived = (long)MaxTextMessageSize * 3L;
        return derived > int.MaxValue ? int.MaxValue : (int)derived;
    }
}
