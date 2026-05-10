// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;


/// <summary>
/// 分片上传文件-预上传请求体
/// <para>发送初始化请求，以获取上传事务 ID 和分片策略，为[上传分片]做准备。平台固定以 4MB 的大小对文件进行分片。</para>
/// <para>## 注意事项</para>
/// <para>上传事务 ID 和上传进度在 24 小时内有效。请及时保存和恢复上传。</para>
/// <para>## 使用限制</para>
/// <para>- 该接口不支持并发调用，且调用频率上限为 5 QPS，10000 次/天。否则会返回 1061045 错误码，可通过稍后重试解决。</para>
/// <para>- 上传文件的大小限制因飞书版本而异，详情参考[文件上传、在线预览的大小及格式要求](https://www.feishu.cn/hc/zh-CN/articles/360049067549-%E6%96%87%E4%BB%B6%E4%B8%8A%E4%BC%A0-%E5%9C%A8%E7%BA%BF%E9%A2%84%E8%A7%88%E7%9A%84%E5%A4%A7%E5%B0%8F%E5%8F%8A%E6%A0%BC%E5%BC%8F%E8%A6%81%E6%B1%82)。</para>
/// </summary>
public class FilesUploadPrepareRequest
{
    /// <summary>
    /// <para>文件的名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：test.txt</para>
    /// <para>最大长度：250</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>上传点的类型。取固定值 explorer，表示将文件上传至云空间中。</para>
    /// <para>必填：是</para>
    /// <para>示例值：explorer</para>
    /// <para>可选值：<list type="bullet">
    /// <item>explorer：云空间</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("parent_type")]
    public string ParentType { get; set; } = string.Empty;

    /// <summary>
    /// <para>云空间中文件夹的 token。了解如何获取文件夹 token，参考[文件夹概述](https://open.feishu.cn/document/ukTMukTMukTM/ugTNzUjL4UzM14CO1MTN/folder-overview)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a92abcef</para>
    /// </summary>
    [JsonPropertyName("parent_node")]
    public string ParentNode { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的大小，单位为字节。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1024</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }
}
