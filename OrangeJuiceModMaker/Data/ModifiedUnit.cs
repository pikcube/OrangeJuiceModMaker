using System;
using System.IO;
using System.Linq;

namespace OrangeJuiceModMaker.Data;

public class ModifiedUnit
{
    private readonly Unit baseUnit;
    public string UnitId { get; }
    public string UnitName { get; }
    public Card[] HyperCards { get; }
    public Card[] CharacterCards { get; }

    public Music? Music { get; set; }
    public string[] CharacterArt { get; }
    public int[] FaceX { get; }
    public int[] FaceY { get; }
    public bool IsModified { get; }

    public ModifiedUnit(Unit baseUnit, string baseResourcePath, ModReplacements? replacements, bool includeModData = true)
    {
        //Initialize Default Values for the no replacement case
        this.baseUnit = baseUnit;
        UnitId = baseUnit.UnitId;
        UnitName = baseUnit.UnitName;
        CharacterCards = [.. baseUnit.CharacterCards.Select(z => z)];
        HyperCards = [.. baseUnit.HyperCards.Select(z => z)];
        CharacterArt = [.. baseUnit.CharacterArt.Select(z => z)];
        Music = null;
        FaceX = new int[CharacterArt.Length];
        FaceY = new int[CharacterArt.Length];
        IsModified = false;

        if (!includeModData)
        {
            return;
        }

        if (replacements == null)
        {
            return;
        }

        //Load in Music Replacements
        foreach (Music m in replacements.Music.Where(z => z.UnitId is not null && z.UnitId == UnitId))
        {
            Music = new Music(m.File)
            {
                LoopPoint = m.LoopPoint,
                Volume = m.Volume,
                UnitId = m.UnitId,
                File = $@"{baseResourcePath}\{m.File}.ogg"
            };
            IsModified = true;
        }

        //Search for Hyper Replacements
        for (int n = 0; n < HyperCards.Length; ++n)
        {
            Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}256.png" == HyperCards[n].Path);

            if (r is null)
            {
                continue;
            }
            HyperCards[n].Path = $@"{baseResourcePath}\{r.Path}256.png";
            HyperCards[n].PathLow = $@"{baseResourcePath}\{r.Path}128.png";
            Texture.EnsureCardExists(r.Path, HyperCards[n].Path, HyperCards[n].PathLow);
            IsModified = true;
        }

        //Search for Character Card Replacements
        for (int n = 0; n < CharacterCards.Length; ++n)
        {
            Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}256.png" == CharacterCards[n].Path);
            if (r is null)
            {
                continue;
            }
            CharacterCards[n].Path = $@"{baseResourcePath}\{r.Path}256.png";
            CharacterCards[n].PathLow = $@"{baseResourcePath}\{r.Path}128.png";
            Texture.EnsureCardExists(r.Path, CharacterCards[n].Path, CharacterCards[n].PathLow);
            CharacterCards[n].CardName = r.CustomName ?? CharacterCards[n].CardName;
            IsModified = true;
        }

        //Search for Unit Replacements
        for (int n = 0; n < CharacterArt.Length; ++n)
        {
            Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}.png" == CharacterArt[n]);
            if (r is null)
            {
                continue;
            }

            CharacterArt[n] = $@"{baseResourcePath}\{r.Path}.png";
            Texture.EnsureUnitExists(r.Path, CharacterArt[n]);
            FaceX[n] = r.FaceX ?? 0;
            FaceY[n] = r.FaceY ?? 0;
            IsModified = true;
        }
    }

    public void SaveToMod(string baseResourcePath, ModDefinition definition, ModReplacements replacements)
    {
        replacements.Music.RemoveAll(z => z.UnitId is not null && z.UnitId == UnitId);

        if (Music is not null)
        {
            int stripNumber = baseResourcePath.Length + 1; //+1 to avoid the accumulation of slashes
            Music m = new(Music.File)
            {
                LoopPoint = Music.LoopPoint,
                UnitId = Music.UnitId,
                Volume = Music.Volume,
                File = Music.File[stripNumber..^4]
            };
            replacements.Music.Add(m);
        }

        for (int n = 0; n < HyperCards.Length; ++n)
        {
            if (HyperCards[n].Path == baseUnit.HyperCards[n].Path && HyperCards[n].CardName == baseUnit.HyperCards[n].CardName &&
                HyperCards[n].FlavorText == baseUnit.HyperCards[n].FlavorText)
            {
                continue;
            }

            replacements.Textures.RemoveAll(z => z.Path == $@"cards\{HyperCards[n].CardId}");

            Texture t = new($@"cards\{HyperCards[n].CardId}")
            {
                CustomFlavor = HyperCards[n].FlavorText,
                CustomName = HyperCards[n].CardName
            };
            replacements.Textures.Add(t);

            string highPath = $@"{baseResourcePath}\cards\{HyperCards[n].CardId}256.png";
            string lowPath = $@"{baseResourcePath}\cards\{HyperCards[n].CardId}128.png";

            CopyFile(HyperCards[n].Path, highPath);

            CopyFile(HyperCards[n].PathLow, lowPath);
        }

        for (int n = 0; n < CharacterCards.Length; ++n)
        {
            if (CharacterCards[n].Path == baseUnit.CharacterCards[n].Path &&
                CharacterCards[n].CardName == baseUnit.CharacterCards[n].CardName)
            {
                continue;
            }

            replacements.Textures.RemoveAll(z => z.Path == $@"cards\{CharacterCards[n].CardId}");
            Texture t = new($@"cards\{CharacterCards[n].CardId}")
            {
                CustomName = CharacterCards[n].CardName
            };
            replacements.Textures.Add(t);

            string highDestPath = $@"{baseResourcePath}\cards\{CharacterCards[n].CardId}256.png";
            CopyFile(CharacterCards[n].Path, highDestPath);

            string lowDestPath = $@"{baseResourcePath}\cards\{CharacterCards[n].CardId}128.png";
            CopyFile(CharacterCards[n].PathLow, lowDestPath);

        }

        for (int n = 0; n < CharacterArt.Length; ++n)
        {
            if (CharacterArt[n] == baseUnit.CharacterArt[n])
            {
                continue;
            }

            string shortPath = $@"units\{Path.GetFileNameWithoutExtension(baseUnit.CharacterArt[n])}";

            replacements.Textures.RemoveAll(z => z.Path == shortPath);
            Texture t = new(shortPath)
            {
                FaceX = FaceX[n],
                FaceY = FaceY[n]
            };
            replacements.Textures.Add(t);

            string destFileName = $@"{baseResourcePath}\{shortPath}.png";
            CopyFile(CharacterArt[n], destFileName);
        }

        Root.WriteJson(baseResourcePath, definition, replacements);
    }

    private static void CopyFile(string imagePath, string destPath)
    {
        if (imagePath == destPath)
        {
            return;
        }

        if (File.Exists(destPath))
        {
            File.Delete(destPath);
        }

        File.Copy(imagePath, destPath);
    }
}