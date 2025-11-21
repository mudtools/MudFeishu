
namespace Mud.Feishu.DataModels.Employees;
public class PhoneValue
{
     [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("extension_number")]
    public string ExtensionNumber { get; set; }
}