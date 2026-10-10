// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Mud.Feishu.EventCallback;

namespace Mud.Feishu.AI.Tools.Events;

/// <summary>
/// 事件目录（R7 / C7 的"事件自省"面）：列出本 SDK 支持的<b>全部事件键</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据源 = <see cref="FeishuEventTypes"/> 的公开常量</b>（唯一真相源）——
/// 不在 SDK 里维护第二份"事件清单"（那是最典型的双源漂移：加了事件忘了改表）。
/// </para>
/// <para>
/// <b>刻意不提供"载荷 schema 摘要"（与方案 §4.7 的偏差，诚实登记）</b>：载荷类型与事件键的对应关系
/// 只存在于<b>源生成阶段</b>（`[GenerateEventHandler]` 特性 + 生成产物），运行期元数据里
/// <b>没有</b>这层映射（特性由生成器条件发射、处理器基类是 abstract 且事件类型是实例属性）。
/// 要在运行期拼出这张表，只能①手抄（必然漂移）或②反射实例化（需构造参数/不安全 API）。两者都比
/// "只列事件键"更糟，故本类型<b>只做可确定的那一半</b>：事件键清单。载荷结构请查各
/// <c>IEventResult</c> 实现的 XML 注释（含事件类型与官方文档地址）或
/// <c>Mud.Feishu.EventCallback/README.md</c>。
/// </para>
/// <para>
/// 用途：外桥侧配置<b>路由正则</b>时需要一份"事件键到底有哪些"的权威清单（写错正则的表现是
/// 静默不落盘），以及宿主自查"我订阅的事件本 SDK 认不认识"。
/// </para>
/// </remarks>
public static class FeishuEventCatalog
{
    /// <summary>
    /// 列出全部事件键（Ordinal 排序；去重）。
    /// </summary>
    public static IReadOnlyList<string> ListEventKeys()
    {
        var keys = new List<string>();
        foreach (var field in typeof(FeishuEventTypes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!field.IsLiteral || field.FieldType != typeof(string))
            {
                continue;
            }

            var value = field.GetRawConstantValue() as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                keys.Add(value!);
            }
        }

        keys.Sort(StringComparer.Ordinal);
        return keys.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// 输出 NDJSON 形态的事件清单（一行一个事件键），便于 <c>event list</c> 式管道消费。
    /// </summary>
    public static IEnumerable<string> ListEventKeysAsNdjson()
        => ListEventKeys().Select(key => "{\"event_key\":\"" + key + "\"}");
}