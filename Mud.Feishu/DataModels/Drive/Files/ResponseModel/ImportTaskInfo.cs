// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>导入结果</para>
/// </summary>
public class ImportTaskInfo
{
    /// <summary>
    /// <para>导入任务 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7369583175086912356</para>
    /// </summary>
    [JsonPropertyName("ticket")]
    public string? Ticket { get; set; }

    /// <summary>
    /// <para>导入的在线云文档类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：sheet</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务的状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：导入成功。但需关注是否有 extra 参数返回。如果源文件内容超过系统上限，将被系统截断，此时将返回 extra 参数，进行额外提示。extra 参数说明见本文末尾。</item>
    /// <item>1：初始化</item>
    /// <item>2：处理中</item>
    /// <item>3：内部错误</item>
    /// <item>100：导入文档已加密</item>
    /// <item>101：内部错误</item>
    /// <item>102：内部错误</item>
    /// <item>103：内部错误</item>
    /// <item>104：租户容量不足</item>
    /// <item>105：文件夹节点太多</item>
    /// <item>106：内部错误</item>
    /// <item>108：处理超时</item>
    /// <item>109：内部错误</item>
    /// <item>110：无权限</item>
    /// <item>112：格式不支持</item>
    /// <item>113：office格式不支持</item>
    /// <item>114：内部错误</item>
    /// <item>115：导入文件过大</item>
    /// <item>116：当前身份无导入至该文件夹的权限。</item>
    /// <item>117：目录已删除</item>
    /// <item>118：导入文件和任务指定后缀不匹配</item>
    /// <item>119：目录不存在</item>
    /// <item>120：导入文件和任务指定文件类型不匹配</item>
    /// <item>121：导入文件已过期</item>
    /// <item>122：创建副本中禁止导出</item>
    /// <item>129：文件格式损坏。请另存为新文件后导入</item>
    /// <item>5000：内部错误</item>
    /// <item>7000：docx block 数量超过系统上限</item>
    /// <item>7001：docx block 层级超过系统上线</item>
    /// <item>7002：docx block 大小超过系统上限</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("job_status")]
    public int? JobStatus { get; set; }

    /// <summary>
    /// <para>任务失败的原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("job_error_msg")]
    public string? JobErrorMsg { get; set; }

    /// <summary>
    /// <para>导入云文档的 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：Fm7osyjtMh5o7Ktrv32c73abcef</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>导入云文档的 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.feishu.cn/sheets/Fm7osyjtMh5o7Ktrv32c73abcef</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>导入成功的额外提示。详情参考下文。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string[]? Extra { get; set; }
}