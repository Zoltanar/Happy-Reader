using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Happy_Apps_Core.API_Objects;

public struct UList
{
    private static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
    public string User { get; set; }
    public string Fields { get; set; }
    public object[] Filters { get; set; }
    public string Sort { get; set; }
    public bool Reverse { get; set; }
    public int Results { get; set; }

    public string GetAsJson()
    {
        return JsonConvert.SerializeObject(this, _serializerSettings);
    }
}

public struct UListPatch
{
    private static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int[] Labels_Unset { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int[] Labels_Set { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Include, PropertyName = "vote")]
    public int? Vote { get; set; }


    public string GetAsJson(bool includeVote)
    {
        //use JsonProperty values if we want to include vote when null (for removing vote), else, ignore all null values.
        return JsonConvert.SerializeObject(this, includeVote ? null :_serializerSettings);
    }
}
