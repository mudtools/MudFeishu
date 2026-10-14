// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 媒体输入配置（配置节 <c>FeishuAgent:MediaInput</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>A16 配置面</b>：所有属性有真实消费点（装配器读取）。
/// </para>
/// <para>
/// <b>A15 零差异</b>：默认启用图片 OCR 和音频 STT——但装配器本身经
/// <c>ImConversationAssemblers.MediaMessage</c> 位标记启用，未启用时零行为差异。
/// </para>
/// </remarks>
public sealed class MediaInputOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FeishuAgent:MediaInput";

    /// <summary>是否启用图片 OCR（默认 true）。</summary>
    public bool EnableImageOcr { get; set; } = true;

    /// <summary>是否启用语音 STT（默认 true）。</summary>
    public bool EnableAudioStt { get; set; } = true;

    /// <summary>下载字节数上限（默认 5 MB，与平台 OCR 限制一致）。</summary>
    public int MaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>语音时长上限（秒，默认 300）。</summary>
    public int MaxDurationSeconds { get; set; } = 300;

    /// <summary>单轮注入总预算（字符，默认 1500）。</summary>
    public int TotalLengthBudget { get; set; } = 1500;

    /// <summary>单条转写文本截断长度（字符）。</summary>
    public int PerItemLength { get; set; } = 500;

    /// <summary>
    /// 校验配置自洽性。
    /// </summary>
    /// <exception cref="InvalidOperationException">配置值越界或不自洽时抛出。</exception>
    public void Validate()
    {
        if (MaxBytes is < 1024 or > 10 * 1024 * 1024)
        {
            throw new InvalidOperationException(
                $"{nameof(MaxBytes)} 必须在 1KB..10MB 范围内（当前 {MaxBytes}）");
        }

        if (MaxDurationSeconds is < 1 or > 600)
        {
            throw new InvalidOperationException(
                $"{nameof(MaxDurationSeconds)} 必须在 1..600 秒范围内（当前 {MaxDurationSeconds}）");
        }

        if (TotalLengthBudget is < 1 or > 8000)
        {
            throw new InvalidOperationException(
                $"{nameof(TotalLengthBudget)} 必须在 1..8000 字符范围内（当前 {TotalLengthBudget}）");
        }

        if (PerItemLength is < 1 || PerItemLength > TotalLengthBudget)
        {
            throw new InvalidOperationException(
                $"{nameof(PerItemLength)} 必须在 1..{nameof(TotalLengthBudget)} 范围内（当前 {PerItemLength}）");
        }
    }
}
