using System.IO;
using ImageMagick;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class Texture(string path)
{
    [JsonProperty("path")]
    public string Path { get; set; } = path;

    [JsonProperty("face_x")]
    public int? FaceX { get; set; }

    [JsonProperty("face_y")]
    public int? FaceY { get; set; }

    [JsonProperty("costume_id")]
    public int? CostumeId { get; set; }

    [JsonProperty("custom_name")]
    public string? CustomName { get; set; }

    [JsonProperty("custom_flavor")]
    public string? CustomFlavor { get; set; }

    [JsonProperty("single_file")]
    public bool? SingleFile { get; set; }

    public static void EnsureUnitExists(string path, string currentUnitPath)
    {
        bool unitExists = File.Exists(currentUnitPath);
        string defaultUnitPath = $@"pakfiles\{path}.png";

        if (unitExists)
        {
            return;
        }

        File.Copy(defaultUnitPath, currentUnitPath);
    }

    public static void EnsureCardExists(string path, string currentArtPath, string currentLowArtPath)
    {
        bool lowExists = File.Exists(currentLowArtPath);
        bool highExists = File.Exists(currentArtPath);
        string defaultArtPath = $@"pakfiles\{path}256.png";
        string defaultLowPath = $@"pakfiles\{path}128.png";

        switch (highExists)
        {
            case false when !lowExists:
                //Well if they are both gone, let's just grab the originals. The mod creator would need to locate them anyways
                File.Copy(defaultArtPath, currentArtPath);
                File.Copy(defaultLowPath, currentLowArtPath);
                return;
            case false when lowExists:
                //This is the bad case. First let's check if this was just a text mod
                if (MyExtensions.CompareFiles(defaultLowPath, currentLowArtPath))
                {
                    File.Copy(defaultArtPath, currentArtPath);
                    return;
                }

                //Oh shit this wasn't just a text mod, we actually lost an important file
                ReplaceMissingFile();
                return;
            case true when !lowExists:
                //If only the low is missing, let's just make a low quality one.
                ResizeImage(currentArtPath, 128, currentLowArtPath);
                return;
            default:
                return;
        }

        void ResizeImage(string initialPath, uint newSize, string finalPath)
        {
            MagickImage highQualityArt = new(initialPath);
            highQualityArt.Resize(newSize, newSize);
            highQualityArt.Write(finalPath);
        }

        void ReplaceMissingFile()
        {
            File.Copy(defaultArtPath, currentArtPath);
        }
    }
}