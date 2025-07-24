using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class ModReplacements
{
    [JsonProperty("textures")]
    public List<object> AllTextures { get; set; } = [];

    [JsonIgnore]
    public List<string> BasicTextures { get; set; } = [];

    [JsonIgnore]
    public List<Texture> Textures { get; set; } = [];

    [JsonProperty("music")]
    public List<Music> Music { get; set; } = [];

    [JsonProperty("sound_effects")]
    public List<string> SoundEffects { get; set; } = [];

    [JsonProperty("pets")]
    public List<Pet> Pets = [];
    [JsonProperty("voices")]
    public Voices Voices { get; set; } = new();
}