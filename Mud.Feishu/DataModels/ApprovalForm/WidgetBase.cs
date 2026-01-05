// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalForm;

/// <summary>
/// 非泛型接口，用于处理混合类型的集合
/// </summary>
public interface IWidget
{
    /// <summary>
    /// 获取或设置控件的唯一标识符
    /// </summary>
    string Id { get; set; }
    /// <summary>
    /// 获取或设置控件的类型
    /// </summary>
    string Type { get; set; }
}

/// <summary>
/// 泛型接口，继承自非泛型接口
/// </summary>
public interface IWidget<TValue> : IWidget
{
    /// <summary>
    /// 获取或设置控件的值
    /// </summary>
    TValue Value { get; set; }
}

/// <summary>
/// 控件数据泛型基类
/// </summary>
/// <typeparam name="TValue">值类型</typeparam>
public abstract class WidgetBase<TValue>(string type) : IWidget<TValue>
{
    /// <summary>
    /// 获取或设置控件的唯一标识符
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置控件的类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = type;

    /// <summary>
    /// 获取或设置控件的值
    /// </summary>
    [JsonPropertyName("value")]
    public TValue Value { get; set; } = default;
}
