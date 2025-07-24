using System.IO;
using System.Linq;

namespace OrangeJuiceModMaker.Data;

internal class ModTexture : Texture
{
    public readonly string Id;
    public string CurrentArtPath;
    public string CurrentLowArtPath;

    public ModTexture(string path, ModReplacements replacements, string modPath) : base(path)
    {
        Id = Path[6..];
        Texture? texture = replacements.Textures.FirstOrDefault(z => z.Path == path);
        CurrentArtPath = $@"{modPath}\{path}256.png";
        CurrentLowArtPath = $@"{modPath}\{path}128.png";
        string defaultArtPath = $@"pakfiles\{path}256.png";
        string defaultLowPath = $@"pakfiles\{path}128.png";
        if (texture is null)
        {
            CurrentArtPath = defaultArtPath;
            CurrentLowArtPath = defaultLowPath;
            return;
        }
        EnsureCardExists(path, CurrentArtPath, CurrentLowArtPath);

        FaceX = texture.FaceX;
        FaceY = texture.FaceY;
        CostumeId = texture.CostumeId;
        CustomName = texture.CustomName;
        CustomFlavor = texture.CustomFlavor;
        SingleFile = texture.SingleFile;
    }

    public void SaveToMod(string modPath, ModDefinition definition, ref ModReplacements replacements)
    {
        replacements.Textures.RemoveAll(z => z.Path == Path);
        Texture t = new(Path)
        {
            FaceX = FaceX,
            FaceY = FaceY,
            CostumeId = CostumeId,
            CustomName = CustomName,
            CustomFlavor = CustomFlavor,
            SingleFile = SingleFile
        };
        replacements.Textures.Add(t);
        if (CurrentArtPath != $@"{modPath}\{Path}256.png")
        {
            File.Copy(CurrentArtPath, $@"{modPath}\{Path}256.png", true);
            File.Copy(CurrentLowArtPath, $@"{modPath}\{Path}128.png", true);
        }

        Root.WriteJson(modPath, definition, replacements);
    }
}