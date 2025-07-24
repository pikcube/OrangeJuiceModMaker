using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class RootRepair(ModDefinition modDefinition)
{
    [JsonProperty(nameof(ModDefinition))]
    public ModDefinition ModDefinition { get; set; } = modDefinition;

    [JsonProperty(nameof(ModReplacements))]
    public ModReplacementsRepair? ModReplacements { get; set; }

    public class ModReplacementsRepair
    {
        [JsonProperty("textures")]
        public List<string> Textures { get; set; } = [];

        [JsonProperty("music")]
        public List<Music> Music { get; set; } = [];

        [JsonProperty("sound_effects")]
        public List<string> SoundEffects { get; set; } = [];

        [JsonProperty("pets")]
        public List<Pet> Pets = [];
        [JsonProperty("voices")]
        public Voices Voices { get; set; } = new();
    }
}