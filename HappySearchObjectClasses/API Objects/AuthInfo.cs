using System;
// ReSharper disable once CheckNamespace
using Newtonsoft.Json;

namespace Happy_Apps_Core.API_Objects;

public struct AuthInfo
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string[] Permissions { get; set; }
    [JsonIgnore]
    public int IdAsInteger {get
    {
        if (Id[0] != 'u') throw new InvalidOperationException($"Unexpected User Id string: {Id}");
        return int.Parse(Id.Substring(1));

    }}
}