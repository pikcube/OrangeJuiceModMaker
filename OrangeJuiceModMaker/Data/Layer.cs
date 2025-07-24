using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class Layer
{
    public static readonly int[] VariantLayers = [0, 1, 2, 3, 4, 5];

    private int variant;

    [JsonProperty("variant")]
    public int Variant
    {
        get => variant;
        set
        {
            if (VariantLayers.Contains(value))
            {
                variant = value;
            }
            else
            {
                throw new InvalidDataException("Variant must be between 0 and 5");
            }
        }
    }

    public enum Type
    {
        Base = 0,
        Shadow = 1,
        Lineart = 2,
    }

    private Type layerType;
    [JsonProperty("layer")]
    public string LayerType
    {
        get => layerType.ToString();
        set
        {
            if (!Enum.TryParse(value, true, out layerType))
            {
                throw new InvalidDataException("Not valid layer");
            }
        }
    }

    [JsonProperty("color")]
    public string Color;

    [JsonProperty("multiply")]
    public bool? Multiply;

    public Layer(int variant, string layerType, string color, bool multiply = false)
    {
        Variant = variant;
        LayerType = layerType;
        Color = color;
        Multiply = multiply;
    }
}