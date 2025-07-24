using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class PetTexture(string layer, string path)
{
    [JsonProperty("layer")]
    public string Layer = layer;
    [JsonProperty("path")]
    public string Path = path;
}