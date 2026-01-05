// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.DataModels.ApprovalForm;

/// <summary>
/// 审批表单组件工厂类
/// </summary>
public class WidgetFactory
{
    /// <summary>
    /// 根据类型枚举创建组件实例
    /// </summary>
    public static IWidget CreateWidget(WidgetType type, string id = null)
    {
        string rId = "";
        if (string.IsNullOrEmpty(id))
        {
            var random = new Random();
            rId = random.Next(100000, 999999).ToString();
        }

        return type switch
        {
            WidgetType.Input => new InputWidget { Id = id ?? "input" + "_" + rId },
            WidgetType.Textarea => new TextareaWidget { Id = id ?? "textarea" + "_" + rId },
            WidgetType.Date => new DateWidget { Id = id ?? "date" + "_" + rId },
            WidgetType.DateInterval => new DateIntervalWidget { Id = id ?? "dateInterval" + "_" + rId },
            WidgetType.RadioV2 => new RadioV2Widget { Id = id ?? "radio" + "_" + rId },
            WidgetType.CheckboxV2 => new CheckboxV2Widget { Id = id ?? "checkbox" + "_" + rId },
            WidgetType.Number => new NumberWidget { Id = id ?? "number" + "_" + rId },
            WidgetType.Amount => new AmountWidget { Id = id ?? "amount" + "_" + rId },
            WidgetType.Formula => new FormulaWidget { Id = id ?? "formula" + "_" + rId },
            WidgetType.Connect => new ConnectWidget { Id = id ?? "connect" + "_" + rId },
            WidgetType.Document => new DocumentWidget { Id = id ?? "document" + "_" + rId },
            WidgetType.AttachmentV2 => new AttachmentV2Widget { Id = id ?? "attachment" + "_" + rId },
            WidgetType.Image => new ImageWidget { Id = id ?? "image" + "_" + rId },
            WidgetType.FieldList => new FieldListWidget { Id = id ?? "fieldlist" + "_" + rId },
            WidgetType.Department => new DepartmentWidget { Id = id ?? "department" + "_" + rId },
            WidgetType.Telephone => new TelephoneWidget { Id = id ?? "telephone" + "_" + rId },
            WidgetType.Address => new AddressWidget { Id = id ?? "address" + "_" + rId },
            WidgetType.ShiftGroup => new ShiftGroupWidget { Id = id ?? "shiftgroup" + "_" + rId },
            _ => throw new ArgumentException($"未知的组件类型: {type}")
        };
    }

    /// <summary>
    /// 将组件序列化为JSON字符串
    /// </summary>
    public static string SerializeToJson(IWidget widget, bool writeIndented = false)
    {
        var options = new JsonSerializerOptions(WidgetSerializerOptions.Options)
        {
            WriteIndented = writeIndented
        };
        return JsonSerializer.Serialize(widget, options);
    }

    /// <summary>
    /// 将组件列表序列化为JSON字符串
    /// </summary>
    public static string SerializeToJson(List<IWidget> widgets, bool writeIndented = false)
    {
        var options = new JsonSerializerOptions(WidgetSerializerOptions.Options)
        {
            WriteIndented = writeIndented
        };
        return JsonSerializer.Serialize(widgets, options);
    }
}

/// <summary>
/// 序列化选项配置
/// </summary>
internal static class WidgetSerializerOptions
{
    public static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}