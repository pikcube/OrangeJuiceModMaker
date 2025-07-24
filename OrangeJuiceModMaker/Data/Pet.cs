using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class Pet(
    string id,
    List<PetTexture> textures,
    List<Layer> layers,
    bool floating = false,
    int faceX = 128,
    int faceY = 128,
    int drawOffsetX = 0,
    int drawOffsetY = 0)
{
    [JsonProperty("id")]
    public string Id = id;

    [JsonProperty("floating")]
    public bool Floating = floating;

    [JsonProperty("face_x")]
    public int FaceX = faceX;

    [JsonProperty("face_y")]
    public int FaceY = faceY;

    [JsonProperty("textures")]
    public List<PetTexture> Textures = textures;

    [JsonProperty("layers")]
    public List<Layer> Layers = layers;

    [JsonProperty("draw_offset_x")]
    public int DrawOffsetX = drawOffsetX;

    [JsonProperty("draw_offset_y")]
    public int DrawOffsetY = drawOffsetY;
}