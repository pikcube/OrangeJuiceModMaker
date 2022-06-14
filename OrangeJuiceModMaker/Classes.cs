using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker
{
    class ModTexture : Texture
    {
        public string Id;
        public string CurrentArtPath;
        public string CurrentLowArtPath;

        public ModTexture(string path) : base(path)
        {
            Id = this.Path.Substring(6);
            CurrentArtPath = $@"{MainWindow.LoadedModPath}\{path}256.png";
            CurrentLowArtPath = $@"{MainWindow.LoadedModPath}\{path}128.png";
            Texture? texture = MainWindow.LoadedModReplacements.Textures.FirstOrDefault(z => z.Path == path);
            if (texture is null)
            {
                CurrentArtPath = $@"pakfiles\{path}256.png";
                CurrentLowArtPath = $@"pakfiles\{path}128.png";
                return;
            }
            FaceX = texture.FaceX;
            FaceY = texture.FaceY;
            CostumeId = texture.CostumeId;
            CustomName = texture.CustomName;
            CustomFlavor = texture.CustomFlavor;
            SingleFile = texture.SingleFile;
        }

        public void SaveToMod()
        {
            MainWindow.LoadedModReplacements.Textures.RemoveAll(z => z.Path == Path);
            Texture t = new(Path)
            {
                FaceX = FaceX,
                FaceY = FaceY,
                CostumeId = CostumeId,
                CustomName = CustomName,
                CustomFlavor = CustomFlavor,
                SingleFile = SingleFile
            };
            MainWindow.LoadedModReplacements.Textures.Add(t);
            if (CurrentArtPath == $@"{MainWindow.LoadedModPath}\{Path}256.png")
            {
                return;
            }
            File.Copy(CurrentArtPath, $@"{MainWindow.LoadedModPath}\{Path}256.png");
            File.Copy(CurrentLowArtPath, $@"{MainWindow.LoadedModPath}\{Path}128.png");
            Root.WriteJson();
        }
    }

    public class ModifiedUnit
    {
        private Unit baseUnit;
        public string UnitId { get; }
        public string UnitName { get; set; }
        public string[] HyperIds { get; }
        public string[] HyperNames { get; set; }
        public string[] HyperFlavor { get; set; }
        public string[] HyperCardPaths { get; set; }
        public string[] HyperCardPathsLow { get; set; }
        public string[] CharacterCards { get; }
        public string[] CharacterCardNames { get; set; }
        public string[] CharacterCardPaths { get; set; }
        public string[] CharacterCardPathsLow { get; set; }
        public Music? Music { get; set; }
        public string[] CharacterArt { get; set; }
        public int[] FaceX { get; set; }
        public int[] FaceY { get; set; }

        public ModifiedUnit(Unit baseUnit, bool includeModData = true)
        {
            this.baseUnit = baseUnit;
            UnitId = baseUnit.UnitId;
            UnitName = baseUnit.UnitName;
            HyperIds = baseUnit.HyperIds.Select(z => z).ToArray();
            HyperNames = baseUnit.HyperNames.Select(z => z).ToArray();
            HyperFlavor = baseUnit.HyperFlavor.Select(z => z).ToArray();
            HyperCardPaths = baseUnit.HyperCardPaths.Select(z => z).ToArray();
            HyperCardPathsLow = baseUnit.HyperCardPathsLow.Select(z => z).ToArray();
            CharacterCards = baseUnit.CharacterCards.Select(z => z).ToArray();
            CharacterCardNames = baseUnit.CharacterCardNames.Select(z => z).ToArray();
            CharacterCardPaths = baseUnit.CharacterCardPaths.Select(z => z).ToArray();
            CharacterCardPathsLow = baseUnit.CharacterCardPathsLow.Select(z => z).ToArray();
            CharacterArt = baseUnit.CharacterArt.Select(z => z).ToArray();
            Music = null;
            FaceX = new int[CharacterArt.Length];
            FaceY = new int[CharacterArt.Length];

            string baseResourcePath = $@"{MainWindow.LoadedModPath}";

            if (!includeModData)
            {
                return;
            }

            ModReplacements? replacements = MainWindow.LoadedModReplacements;

            if (replacements == null)
            {
                return;
            }

            foreach (Music m in replacements.Music.Where(z => z.UnitId is not null && z.UnitId == UnitId))
            {
                Music = new Music(m.File)
                {
                    LoopPoint = m.LoopPoint,
                    Volume = m.Volume,
                    UnitId = m.UnitId,
                    File = $@"{baseResourcePath}\{m.File}.ogg"
                };
            }

            for (int n = 0; n < HyperCardPaths.Length; ++n)
            {
                Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}256.png" == HyperCardPaths[n]);

                if (r is null)
                {
                    continue;
                }
                HyperCardPaths[n] = $@"{baseResourcePath}\{r.Path}256.png";
                HyperCardPathsLow[n] = $@"{baseResourcePath}\{r.Path}128.png";
                HyperFlavor[n] = r.CustomFlavor ?? HyperFlavor[n];
                HyperNames[n] = r.CustomName ?? HyperNames[n];
            }

            for (int n = 0; n < CharacterCardPaths.Length; ++n)
            {
                Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}256.png" == CharacterCardPaths[n]);
                if (r is null)
                {
                    continue;
                }
                CharacterCardPaths[n] = $@"{baseResourcePath}\{r.Path}256.png";
                CharacterCardPathsLow[n] = $@"{baseResourcePath}\{r.Path}128.png";
                CharacterCardNames[n] = r.CustomName ?? CharacterCardNames[n];
            }

            for (int n = 0; n < CharacterArt.Length; ++n)
            {
                Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}.png" == CharacterArt[n]);
                if (r is null) continue;
                CharacterArt[n] = $@"{baseResourcePath}\{r.Path}.png";
                FaceX[n] = r.FaceX ?? 0;
                FaceY[n] = r.FaceY ?? 0;
            }
        }

        public void SaveToMod()
        {
            string baseResourcePath = $@"{MainWindow.LoadedModPath}";
            ModReplacements replacements = MainWindow.LoadedModReplacements;

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

            for (int n = 0; n < HyperCardPaths.Length; ++n)
            {
                if (HyperCardPaths[n] == baseUnit.HyperCardPaths[n] && HyperNames[n] == baseUnit.HyperNames[n] && HyperFlavor[n] == baseUnit.HyperFlavor[n])
                {
                    continue;
                }

                replacements.Textures.RemoveAll(z => z.Path == $@"cards\{HyperIds[n]}");

                Texture t = new($@"cards\{HyperIds[n]}")
                {
                    CustomFlavor = HyperFlavor[n],
                    CustomName = HyperNames[n]
                };
                replacements.Textures.Add(t);

                if (File.Exists($@"{baseResourcePath}\cards\{HyperIds[n]}256.png"))
                {
                    File.Delete($@"{baseResourcePath}\cards\{HyperIds[n]}256.png");
                }
                File.Copy(HyperCardPaths[n], $@"{baseResourcePath}\cards\{HyperIds[n]}256.png");
                
                if (File.Exists($@"{baseResourcePath}\cards\{HyperIds[n]}128.png"))
                {
                    File.Delete($@"{baseResourcePath}\cards\{HyperIds[n]}128.png");
                }
                File.Copy(HyperCardPathsLow[n], $@"{baseResourcePath}\cards\{HyperIds[n]}128.png");
            }

            for (int n = 0; n < CharacterCardPaths.Length; ++n)
            {
                if (CharacterCardPaths[n] == baseUnit.CharacterCardPaths[n] && CharacterCardNames[n] == baseUnit.CharacterCardNames[n])
                {
                    continue;
                }

                replacements.Textures.RemoveAll(z => z.Path == $@"cards\{CharacterCards[n]}");
                Texture t = new($@"cards\{CharacterCards[n]}")
                {
                    CustomName = CharacterCardNames[n]
                };
                replacements.Textures.Add(t);

                if (File.Exists($@"{baseResourcePath}\cards\{CharacterCards[n]}256.png"))
                {
                    File.Delete($@"{baseResourcePath}\cards\{CharacterCards[n]}256.png");
                }
                File.Copy(CharacterCardPaths[n], $@"{baseResourcePath}\cards\{CharacterCards[n]}256.png");

                if (File.Exists($@"{baseResourcePath}\cards\{CharacterCards[n]}128.png"))
                {
                    File.Delete($@"{baseResourcePath}\cards\{CharacterCards[n]}128.png");
                }
                File.Copy(CharacterCardPathsLow[n], $@"{baseResourcePath}\cards\{CharacterCards[n]}128.png");

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
                if (CharacterArt[n] == $@"{baseResourcePath}\{shortPath}")
                {
                    continue;
                }

                if (File.Exists($@"{baseResourcePath}\{shortPath}.png"))
                {
                    File.Delete($@"{baseResourcePath}\{shortPath}.png");
                }

                File.Copy(CharacterArt[n], $@"{baseResourcePath}\{shortPath}.png");

            }

            Root.WriteJson();
        }
    }

    public class Unit
    {
        public string UnitId { get; }
        public string UnitName { get; }
        public string[] HyperIds { get; }
        public string[] HyperNames { get; }
        public string[] HyperFlavor { get; }
        public string[] HyperCardPaths { get; }
        public string[] HyperCardPathsLow { get; }
        public string[] CharacterCards { get; }
        public string[] CharacterCardNames { get; }
        public string[] CharacterCardPaths { get; }
        public string[] CharacterCardPathsLow { get; }
        public string[] CharacterArt => getCharacterArt.Result;

        private readonly Task<string[]> getCharacterArt;

        public Unit(string[] row, CsvHolder characterCards)
        {
            if (row.Length < 3)
            {
                throw new FormatException("Bad Row");
            }

            UnitId = row[1];
            UnitName = row[0];

            getCharacterArt = Task.Run(() =>
            {
                try
                {
                    string[] characterArt = Directory.GetFiles(@"pakFiles\units").Where(z =>
                    {
                        string s = Path.GetFileNameWithoutExtension(z);
                        return s.StartsWith($"{UnitId}_00_") && $"{UnitId}_00_".Length + 2 == s.Length;
                    }).ToArray();

                    if (characterArt.Length == 0)
                    {
                        characterArt = Directory.GetFiles(@"pakFiles\units").Where(z =>
                        {
                            string s = Path.GetFileNameWithoutExtension(z);
                            return s.StartsWith($"{UnitId}_00_00_") && $"{UnitId}_00_00_".Length + 2 == s.Length;
                        }).ToArray();
                    }

                    return characterArt;
                }
                catch (Exception exception)
                {
                    string[] error =
                        { DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                    File.WriteAllLines("unit_class_error.txt", error);
                    MainWindow.ExitTime = true;
                    throw;
                }
            });

            CharacterCards = characterCards.Rows.Where(z => z[0] == UnitId).Select(z => z[2]).ToArray();
            CharacterCardNames = characterCards.Rows.Where(z => z[0] == UnitId).Select(z => z[1]).ToArray();

            switch (row[2])
            {
                case "0":
                    //No Hypers?
                    HyperIds = new string[] { };
                    HyperNames = new string[] { };
                    break;
                case "1":
                    //Normal People
                    HyperIds = new[] { row[3] };
                    HyperNames = new[] { FindUnitHyperNameById(row[3]) };
                    break;
                case "2":
                    //TWO WHOLE HYPERS?
                    HyperIds = new[] { row[3], row[4] };
                    HyperNames = new[] { FindUnitHyperNameById(row[3]), FindUnitHyperNameById(row[4]) };
                    break;
                case "-1":
                    //Way Too Many Hypers. Probably a boss
                    CsvHolder file = MainWindow.CsvFiles.First(z => z.Name == row[3]);
                    HyperIds = file.Rows.Select(z => z[1]).ToArray();
                    HyperNames = file.Rows.Select(z => z[0]).ToArray();
                    CharacterCards = new[] { UnitId };
                    CharacterCardNames = new[] { UnitName };
                    break;
                case "":
                    throw new FormatException("Bad Row");
                default:
                    if (int.TryParse(row[2], out int numHypers))
                    {
                        HyperIds = new string[numHypers];
                        HyperNames = new string[numHypers];
                        for (int i = 0; i < numHypers; ++i)
                        {
                            HyperIds[i] = row[i + 3];
                            HyperNames[i] = FindUnitHyperNameById(row[i + 3]);
                        }
                    }
                    else
                    {
                        throw new FormatException("Bad Row");
                    }
                    break;
            }

            CharacterCardPaths = CharacterCards
                .Select(c => MainWindow.Cards.First(z => Path.GetFileNameWithoutExtension(z) == $"{c}256")).ToArray();
            HyperCardPaths = HyperIds
                .Select(z => MainWindow.Cards.First(x => Path.GetFileNameWithoutExtension(x) == $"{z}256")).ToArray();
            CharacterCardPathsLow = CharacterCards
                .Select(c => MainWindow.Cards.First(z => Path.GetFileNameWithoutExtension(z) == $"{c}128")).ToArray();
            HyperCardPathsLow = HyperIds
                .Select(z => MainWindow.Cards.First(x => Path.GetFileNameWithoutExtension(x) == $"{z}128")).ToArray();

            HyperFlavor = new string[HyperIds.Length];
            if (MainWindow.FlavorLookUp is null)
            {
                return;
            }
            for (int n = 0; n < HyperIds.Length; ++n)
            {
                HyperFlavor[n] = MainWindow.FlavorLookUp.Rows.FirstOrDefault(z => z[1] == HyperIds[n])?[3] ?? "";
            }
        }

        private static string FindUnitHyperNameById(string cardId)
        {
            return MainWindow.CsvFiles.First(z => z.Name == "HyperCards").Rows.First(z => z[1] == cardId)[0];
        }
    }

    public class CsvHolder
    {
        public CsvHolder(string filepath)
        {
            Name = Path.GetFileNameWithoutExtension(filepath);
            using TextFieldParser parser = new(filepath);
            parser.Delimiters = new[] { "," };
            parser.HasFieldsEnclosedInQuotes = true;
            List<string[]> rawRows = new();

            TypeId = Array.IndexOf(TypeList, parser.ReadLine());
            Headers = parser.ReadFields() ?? throw new InvalidOperationException();

            while (!parser.EndOfData)
            {
                rawRows.Add(parser.ReadFields() ?? throw new InvalidOperationException());
            }

            Rows = rawRows.ToArray();

        }

        private static readonly string[] TypeList =
        {
            "unit",
            "card",
            "lookup"
        };

        public int TypeId;

        public string Type => TypeId == -1 ? "undefined" : TypeList[TypeId];

        public string Name;

        public string[][] Rows { get; set; }

        public string[] GetRow(int rowNumber)
        {
            return Rows[rowNumber];
        }

        public string[] GetColumn(int columnNumber)
        {
            return Rows.Select(z => z[columnNumber]).ToArray();
        }

        public string[] Headers;

    }

    public class ModDefinition
    {
        public ModDefinition(string name, string desc, string auth, int sysVer)
        {
            Name = name;
            Description = desc;
            Author = auth;
            SystemVersion = sysVer;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("system_version")]
        public int SystemVersion { get; set; }

        [JsonProperty("changelog")]
        public string? Changelog { get; set; }

        [JsonProperty("contest")]
        public bool? Contest { get; set; }

        [JsonProperty("color")]
        public string? Color { get; set; }

    }

    public class ModReplacements
    {
        [JsonProperty("textures")]
        public List<Texture> Textures { get; set; }

        [JsonProperty("music")]
        public List<Music> Music { get; set; }

        public ModReplacements()
        {
            Textures = new List<Texture>();
            Music = new List<Music>();
        }
    }

    public class Music
    {
        public Music(string file)
        {
            File = file;
        }

        [JsonProperty("unit_id")]
        public string? UnitId { get; set; }

        [JsonProperty("file")]
        public string File { get; set; }

        [JsonProperty("loop_point")]
        public int? LoopPoint { get; set; }

        [JsonProperty("volume")]
        public int? Volume { get; set; }
    }

    public class Root
    {
        public Root(ModDefinition modDefinition)
        {
            ModDefinition = modDefinition;
        }

        [JsonProperty("ModDefinition")]
        public ModDefinition ModDefinition { get; set; }

        [JsonProperty("ModReplacements")]
        public ModReplacements? ModReplacements { get; set; }

        public static void WriteJson()
        {
            WriteJson(new Root(MainWindow.LoadedModDefinition!)
            {
                ModReplacements = MainWindow.LoadedModReplacements
            }, $@"{MainWindow.LoadedModPath}\mod.json");
        }

        public static void WriteJson(Root root, string path)
        {
            string s = JsonConvert.SerializeObject(
                root,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }).Replace(@"\\", "/");
            File.WriteAllText(path, s);
        }

        public static Root? ReadJson(string path)
        {
            return JsonConvert.DeserializeObject<Root>(File.ReadAllText(path).Replace("/", @"\\"));
        }

        public static bool IsValidMod(ModReplacements replacements, out List<string> missing)
        {
            missing = new List<string>();

            missing.AddRange(replacements.Textures.Where(z => !File.Exists($@"{MainWindow.LoadedModPath}\{z.Path}.png")).Select(z => $"{z.Path}.png"));

            missing.AddRange(replacements.Music.Where(z => !File.Exists($@"{MainWindow.LoadedModPath}\{z.File}.ogg")).Select(z => $"{z.File}.ogg"));

            return missing.Count == 0;
        }

        public static void RepairMod(ref ModReplacements replacements)
        {
            replacements.Music.RemoveAll(z => !File.Exists($@"{MainWindow.LoadedModPath}\{z.File}.ogg"));
            replacements.Textures.RemoveAll(z => !File.Exists($@"{MainWindow.LoadedModPath}\{z.Path}.png"));
        }
    }

    public class Texture
    {
        public Texture(string path)
        {
            this.Path = path;
        }
        [JsonProperty("path")]
        public string Path { get; set; }

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
    }

}
