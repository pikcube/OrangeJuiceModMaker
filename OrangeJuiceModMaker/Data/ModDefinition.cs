using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class ModDefinition(string name, string desc, string auth, int sysVer)
{
    [JsonProperty("name")]
    public string Name { get; set; } = name;

    [JsonProperty("description")]
    public string Description { get; set; } = desc;

    [JsonProperty("author")]
    public string Author { get; set; } = auth;

    [JsonProperty("system_version")]
    public int SystemVersion { get; set; } = sysVer;

    [JsonProperty("changelog")]
    public string? Changelog { get; set; }

    [JsonProperty("contest")]
    public bool? Contest { get; set; }

    [JsonProperty("color")]
    public string? Color { get; set; }

}