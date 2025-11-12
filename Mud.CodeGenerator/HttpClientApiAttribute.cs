namespace Mud.CodeGenerator;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class HttpClientApiAttribute : Attribute
{
    public HttpClientApiAttribute()
    {
    }

    public HttpClientApiAttribute(string baseAddress)
    {
        BaseAddress = baseAddress;
    }
    public string BaseAddress { get; set; }

    public int Timeout { get; set; } = 50;
}
