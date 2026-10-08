// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 六部命中式剧本的定义（§5.4）。设计动因：问答式 Demo 的最大风险是
/// 「演示效果依赖观众会不会提问」——剧本把值得展示的链路固化为可一键复现的引导语。
/// </summary>
/// <remarks>
/// <para>
/// <b>自动应答（<see cref="AgentScenario.AutoAnswers"/>）的语义边界</b>：
/// 只对<b>只读工具链路</b>自动应答；一旦本轮产生挂起审批，<c>AgentConsoleLoop</c>
/// <b>立即停止自动应答</b>并提示——<b>写动作一律要人确认</b>（自动批准会绕过
/// 「批准只能来自宿主且由人给出」的核心教学点，也违背 ADR-04）。
/// </para>
/// <para>
/// <b>占位符</b>：S4/S5 的引导语含 <c>{sheet_token}</c> / <c>{document_id}</c> 等字面占位符，
/// 刻意<b>不</b>新增环境变量（§12 的 13 个变量是冻结清单）——引导语里显式要求模型"没拿到就问我"，
/// 由操作者在会话中输入真实 token。S1 的空间 ID 走 <c>FEISHU_DEMO_WIKI_SPACE_ID</c>（已登记变量）。
/// </para>
/// </remarks>
internal static class DocAgentScenarios
{
    /// <summary>剧本 S1：知识库体检（只读）。</summary>
    /// <param name="settings">配置（取可选的知识库空间 ID）。</param>
    /// <returns>剧本定义。</returns>
    public static AgentScenario KnowledgeBaseDigest(DocAgentSettings settings)
    {
        var spaceHint = string.IsNullOrWhiteSpace(settings.WikiSpaceId)
            ? "（我没有给你空间 ID，请先用知识库工具列出可见空间与节点，再挑选目标文档。）"
            : $"知识库空间 ID 是 {settings.WikiSpaceId}。";

        return new AgentScenario(
            Name: "kb-digest",
            Aliases: ["kb"],
            Description: "知识库体检（只读，0 次审批）：wiki 解析 → 读正文 → 带来源摘要",
            Prompt:
                "帮我梳理知识库里「产品规范」空间中与「灰度发布」有关的文档，给我一份要点摘要并标注来源。"
                + spaceHint
                + " 注意：知识库链接不能直接喂给文档工具——请先解析出文档 token 再读正文。",
            AutoAnswers:
            [
                "再帮我确认这些文档里有没有提到「回滚」的内容，并说明出处。",
            ]);
    }

    /// <summary>剧本 S2：文档生成（写，2 次审批）。</summary>
    /// <returns>剧本定义。</returns>
    public static AgentScenario DocumentAuthoring()
        => new(
            Name: "doc-authoring",
            Aliases: ["author"],
            Description: "文档生成（写，2 次审批）：建文档 → 追加块 → 回读校验",
            Prompt:
                "在云空间新建一篇《灰度发布演示文档》，包含标题层级、要点列表、一段代码块和一句引用；"
                + "写完请回读一次确认内容已落文档，并告诉我回读结论。",
            AutoAnswers: []);

    /// <summary>剧本 S3：Markdown 导入（读 + 写，2 次审批）。</summary>
    /// <returns>剧本定义。</returns>
    public static AgentScenario MarkdownImport()
        => new(
            Name: "md-import",
            Aliases: ["md"],
            Description: "Markdown 导入（读+写，2 次审批）：转换（免审批，不落文档）→ 落文档",
            Prompt:
                """
                把下面这段 Markdown 写进云文档（新建一篇《Markdown 导入演示》）：

                # 发布流程
                ## 前置检查
                - 单元测试全绿
                - 灰度开关已就绪
                ## 回滚策略
                1. 关闭灰度开关
                2. 回滚上一个版本

                | 环境 | 责任人 |
                | --- | --- |
                | 预发 | 张三 |
                | 生产 | 李四 |

                要求：先做一次 Markdown 转换确认会生成哪些块（这一步不会落文档），再把内容真正写进新建的文档，最后回读校验。
                """,
            AutoAnswers: []);

    /// <summary>剧本 S4：表格数据回填（读 + 写，1 次审批）。</summary>
    /// <returns>剧本定义。</returns>
    public static AgentScenario SheetSync()
        => new(
            Name: "sheet-sync",
            Aliases: ["sheet"],
            Description: "表格数据回填（读+写，1 次审批）：区域读 → 追加行 → 回读尾行",
            Prompt:
                "读取电子表格 {sheet_token} 的「{sheet_id}」工作表 A1:D20 区域，把其中信息不完整的行补齐后追加到表格末尾，"
                + "写完请回读一次尾行确认。如果你还没有拿到表格 token 或工作表 ID，请直接问我，不要猜测。",
            AutoAnswers: []);

    /// <summary>剧本 S5：安全闸与不可撤销删除（写，1 次审批；本 Demo 的高光场景）。</summary>
    /// <returns>剧本定义。</returns>
    public static AgentScenario SafeDelete()
        => new(
            Name: "safe-delete",
            Aliases: ["del"],
            Description: "安全闸与不可撤销删除（写，1 次审批）：块树定位 → dry_run 预演 → 审批 → strict 拦截 → 放行",
            Prompt:
                "帮我删掉文档 {document_id} 的第 2 到第 6 个块。请先读取块树确认要删的区间，"
                + "然后用 dry_run 预演给我看，等我确认之后才能真删。如果你还没有拿到文档 ID，请直接问我。",
            AutoAnswers: []);

    /// <summary>剧本 S6：错误自愈与多轮记忆（只读，0 次审批）。</summary>
    /// <returns>剧本定义。</returns>
    public static AgentScenario SelfHeal()
        => new(
            Name: "self-heal",
            Aliases: ["heal"],
            Description: "错误自愈与多轮记忆（只读，0 次审批）：错误 token → 错误分类 → 自愈 → 记忆",
            Prompt:
                "读取文档 doccnThisTokenDoesNotExist1234567890 的正文，然后告诉我该怎么找到正确的文档。",
            AutoAnswers:
            [
                "刚才你找到的那篇文档，作者是谁？",
            ]);
}
