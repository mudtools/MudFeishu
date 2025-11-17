namespace Mud.CodeGenerator;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
internal class HttpClientApiAttribute : Attribute
{
    public HttpClientApiAttribute()
    {
    }

    public HttpClientApiAttribute(string baseAddress)
    {
        BaseAddress = baseAddress;
    }

    public string ContentType { get; set; } = "application/json; charset=utf-8";

    public string BaseAddress { get; set; }

    public int Timeout { get; set; } = 50;
}
