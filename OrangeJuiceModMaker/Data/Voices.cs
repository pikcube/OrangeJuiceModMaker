using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class Voices
{
    [JsonProperty("character")]
    public List<string> Character { get; set; } = [];

    [JsonProperty("system")]
    public List<string> System { get; set; } = [];
}