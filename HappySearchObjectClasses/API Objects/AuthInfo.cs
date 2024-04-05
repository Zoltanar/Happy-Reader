// ReSharper disable once CheckNamespace
namespace Happy_Apps_Core.API_Objects;

public struct AuthInfo
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string[] Permissions { get; set; }
}