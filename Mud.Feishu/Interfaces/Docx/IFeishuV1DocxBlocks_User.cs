// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 块是文档中的最小构建单元，是内容的结构化组成元素，有着明确的含义。
/// <para>在一篇文档中，有多个不同类型的段落，这些段落被定义为块（Block）。</para>
/// <para>块有多种形态，可以是一段文字、一张电子表格、一张图片或一个多维表格等。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Docx", InheritedFrom = nameof(FeishuV1DocxBlocks))]
[Token(TokenType.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1DocxBlocks : IFeishuV1DocxBlocks, ICurrentUserId
{
}
