using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class Music(string file)
{
    [JsonProperty("unit_id")]
    public string? UnitId { get; set; }
    [JsonProperty("event")]
    public string? Event { get; set; }

    [JsonProperty("file")]
    public string File { get; set; } = file;

    [JsonProperty("loop_point")]
    public int? LoopPoint { get; set; }

    [JsonProperty("volume")]
    public int? Volume { get; set; }
}