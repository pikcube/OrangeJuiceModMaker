using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker
{
    class ModTexture : Texture
    {
        public string ID;
        public string CurrentArtPath;
        public string CurrentLowArtPath;

        public ModTexture(string path) : base(path)
        {
            ID = this.path.Substring(6);
            CurrentArtPath = $@"{MainWindow.LoadedModPath}\{path}256.png";
            CurrentLowArtPath = $@"{MainWindow.LoadedModPath}\{path}128.png";
            Texture? texture = MainWindow.LoadedModReplacements.textures.FirstOrDefault(z => z.path == path);
            if (texture is null)
            {
                CurrentArtPath = $@"pakfiles\{path}256.png";
                CurrentLowArtPath = $@"pakfiles\{path}128.png";
                return;
            }
            face_x = texture.face_x;
            face_y = texture.face_y;
            costume_id = texture.costume_id;
            custom_name = texture.custom_name;
            custom_flavor = texture.custom_flavor;
            single_file = texture.single_file;
        }

        public void SaveToMod()
        {
            MainWindow.LoadedModReplacements.textures.RemoveAll(z => z.path == path);
            Texture t = new(path)
            {
                face_x = face_x,
                face_y = face_y,
                costume_id = costume_id,
                custom_name = custom_name,
                custom_flavor = custom_flavor,
                single_file = single_file,
            };
            MainWindow.LoadedModReplacements.textures.Add(t);
            if (CurrentArtPath == $@"{MainWindow.LoadedModPath}\{path}256.png")
            {
                return;
            }
            File.Copy(CurrentArtPath, $@"{MainWindow.LoadedModPath}\{path}256.png");
            File.Copy(CurrentLowArtPath, $@"{MainWindow.LoadedModPath}\{path}128.png");
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

        public ModifiedUnit(Unit BaseUnit, bool IncludeModData = true)
        {
            baseUnit = BaseUnit;
            UnitId = BaseUnit.UnitId;
            UnitName = BaseUnit.UnitName;
            HyperIds = BaseUnit.HyperIds.Select(z => z).ToArray();
            HyperNames = BaseUnit.HyperNames.Select(z => z).ToArray();
            HyperFlavor = BaseUnit.HyperFlavor.Select(z => z).ToArray();
            HyperCardPaths = BaseUnit.HyperCardPaths.Select(z => z).ToArray();
            HyperCardPathsLow = BaseUnit.HyperCardPathsLow.Select(z => z).ToArray();
            CharacterCards = BaseUnit.CharacterCards.Select(z => z).ToArray();
            CharacterCardNames = BaseUnit.CharacterCardNames.Select(z => z).ToArray();
            CharacterCardPaths = BaseUnit.CharacterCardPaths.Select(z => z).ToArray();
            CharacterCardPathsLow = BaseUnit.CharacterCardPathsLow.Select(z => z).ToArray();
            CharacterArt = BaseUnit.CharacterArt.Select(z => z).ToArray();
            Music = null;
            FaceX = new int[CharacterArt.Length];
            FaceY = new int[CharacterArt.Length];

            string BaseResourcePath = $@"{MainWindow.LoadedModPath}";

            if (!IncludeModData)
            {
                return;
            }

            ModReplacements? replacements = MainWindow.LoadedModReplacements;

            if (replacements == null)
            {
                return;
            }

            foreach (Music m in replacements.music.Where(z => z.unit_id is not null && z.unit_id == UnitId))
            {
                Music = new Music(m.file)
                {
                    loop_point = m.loop_point,
                    volume = m.volume,
                    unit_id = m.unit_id,
                    file = $@"{BaseResourcePath}\{m.file}.ogg"
                };
            }

            for (int n = 0; n < HyperCardPaths.Length; ++n)
            {
                Texture? r = replacements.textures.FirstOrDefault(z => $@"pakFiles\{z.path}256.png" == HyperCardPaths[n]);

                if (r is null)
                {
                    continue;
                }
                File.Copy($@"{BaseResourcePath}\{r.path}256.png", $@"temp\{HyperIds[n]}256.png", true);
                File.Copy($@"{BaseResourcePath}\{r.path}128.png", $@"temp\{HyperIds[n]}128.png", true);
                HyperCardPaths[n] = $@"temp\{HyperIds[n]}256.png";
                HyperCardPathsLow[n] = $@"temp\{HyperIds[n]}128.png";
                HyperFlavor[n] = r.custom_flavor ?? HyperFlavor[n];
                HyperNames[n] = r.custom_name ?? HyperNames[n];
            }

            for (int n = 0; n < CharacterCardPaths.Length; ++n)
            {
                Texture? r = replacements.textures.FirstOrDefault(z => $@"pakFiles\{z.path}256.png" == CharacterCardPaths[n]);
                if (r is null)
                {
                    continue;
                }
                CharacterCardPaths[n] = $@"{BaseResourcePath}\{r.path}256.png";
                CharacterCardPathsLow[n] = $@"{BaseResourcePath}\{r.path}128.png";
                CharacterCardNames[n] = r.custom_name ?? CharacterCardNames[n];
            }

            for (int n = 0; n < CharacterArt.Length; ++n)
            {
                Texture? r = replacements.textures.FirstOrDefault(z => $@"pakFiles\{z.path}.png" == CharacterArt[n]);
                if (r is null) continue;
                CharacterArt[n] = $@"{BaseResourcePath}\{r.path}.png";
                FaceX[n] = r.face_x ?? 0;
                FaceY[n] = r.face_y ?? 0;
            }
        }

        public void SaveToMod()
        {
            string BaseResourcePath = $@"{MainWindow.LoadedModPath}";
            ModReplacements replacements = MainWindow.LoadedModReplacements;

            replacements.music.RemoveAll(z => z.unit_id is not null && z.unit_id == UnitId);

            if (Music is not null)
            {
                int stripNumber = BaseResourcePath.Length + 1; //+1 to avoid the accumulation of slashes
                Music m = new(Music.file)
                {
                    loop_point = Music.loop_point,
                    unit_id = Music.unit_id,
                    volume = Music.volume,
                    file = Music.file[stripNumber..^4]
                };
                replacements.music.Add(m);
            }

            for (int n = 0; n < HyperCardPaths.Length; ++n)
            {
                if (HyperCardPaths[n] == baseUnit.HyperCardPaths[n] && HyperNames[n] == baseUnit.HyperNames[n] && HyperFlavor[n] == baseUnit.HyperFlavor[n])
                {
                    continue;
                }

                replacements.textures.RemoveAll(z => z.path == $@"cards\{HyperIds[n]}");

                Texture t = new($@"cards\{HyperIds[n]}")
                {
                    custom_flavor = HyperFlavor[n],
                    custom_name = HyperNames[n],
                };
                replacements.textures.Add(t);

                if (File.Exists($@"{BaseResourcePath}\cards\{HyperIds[n]}256.png"))
                {
                    File.Delete($@"{BaseResourcePath}\cards\{HyperIds[n]}256.png");
                }
                File.Copy(HyperCardPaths[n], $@"{BaseResourcePath}\cards\{HyperIds[n]}256.png");
                
                if (File.Exists($@"{BaseResourcePath}\cards\{HyperIds[n]}128.png"))
                {
                    File.Delete($@"{BaseResourcePath}\cards\{HyperIds[n]}128.png");
                }
                File.Copy(HyperCardPathsLow[n], $@"{BaseResourcePath}\cards\{HyperIds[n]}128.png");
            }

            for (int n = 0; n < CharacterCardPaths.Length; ++n)
            {
                if (CharacterCardPaths[n] == baseUnit.CharacterCardPaths[n] && CharacterCardNames[n] == baseUnit.CharacterCardNames[n])
                {
                    continue;
                }

                replacements.textures.RemoveAll(z => z.path == $@"cards\{CharacterCards[n]}");
                Texture t = new($@"cards\{CharacterCards[n]}")
                {
                    custom_name = CharacterCardNames[n],
                };
                replacements.textures.Add(t);

                if (File.Exists($@"{BaseResourcePath}\cards\{CharacterCards[n]}256.png"))
                {
                    File.Delete($@"{BaseResourcePath}\cards\{CharacterCards[n]}256.png");
                }
                File.Copy(CharacterCardPaths[n], $@"{BaseResourcePath}\cards\{CharacterCards[n]}256.png");

                if (File.Exists($@"{BaseResourcePath}\cards\{CharacterCards[n]}128.png"))
                {
                    File.Delete($@"{BaseResourcePath}\cards\{CharacterCards[n]}128.png");
                }
                File.Copy(CharacterCardPathsLow[n], $@"{BaseResourcePath}\cards\{CharacterCards[n]}128.png");

            }

            for (int n = 0; n < CharacterArt.Length; ++n)
            {
                if (CharacterArt[n] == baseUnit.CharacterArt[n])
                {
                    continue;
                }

                string ShortPath = $@"units\{Path.GetFileNameWithoutExtension(baseUnit.CharacterArt[n])}";

                replacements.textures.RemoveAll(z => z.path == ShortPath);
                Texture t = new(ShortPath)
                {
                    face_x = FaceX[n],
                    face_y = FaceY[n],
                };
                replacements.textures.Add(t);
                if (CharacterArt[n] == $@"{BaseResourcePath}\{ShortPath}")
                {
                    continue;
                }

                if (File.Exists($@"{BaseResourcePath}\{ShortPath}"))
                {
                    File.Delete($@"{BaseResourcePath}\{ShortPath}");
                }

                File.Copy(CharacterArt[n], $@"{BaseResourcePath}\{ShortPath}");

                Root.WriteJson();
            }
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
        public string[] CharacterArt => _getCharacterArt.Result;

        private readonly Task<string[]> _getCharacterArt;

        public Unit(string[] row, CsvHolder characterCards)
        {
            if (row.Length < 3)
            {
                throw new FormatException("Bad Row");
            }

            UnitId = row[1];
            UnitName = row[0];

            _getCharacterArt = Task.Run(() =>
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
                    string[] Error =
                        { DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                    File.WriteAllLines("unit_class_error.txt", Error);
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
                    CsvHolder file = MainWindow.CSVFiles.First(z => z.Name == row[3]);
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
            for (int n = 0; n < HyperIds.Length; ++n)
            {
                HyperFlavor[n] = MainWindow.FlavorLookUp.Rows.FirstOrDefault(z => z[1] == HyperIds[n])?[3] ?? "";
            }
        }

        private static string FindUnitHyperNameById(string cardId)
        {
            return MainWindow.CSVFiles.First(z => z.Name == "HyperCards").Rows.First(z => z[1] == cardId)[0];
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
            "lookup",
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
        public string? name { get; set; }
        public string? description { get; set; }
        public string? author { get; set; }
        public int system_version { get; set; }
        public bool contest { get; set; } = false;
        public string? changelog { get; set; }
        public string? color { get; set; }

    }

    public class ModReplacements
    {
        public List<Texture> textures { get; set; }
        public List<Music> music { get; set; }

        public ModReplacements()
        {
            textures = new List<Texture>();
            music = new List<Music>();
        }
    }

    public class Music
    {
        public Music(string file)
        {
            this.file = file;
        }

        public string? unit_id { get; set; }
        public string file { get; set; }

        public int? loop_point { get; set; }
        public int? volume { get; set; }
    }

    public class Root
    {
        public Root(ModDefinition modDefinition)
        {
            ModDefinition = modDefinition;
        }

        public ModDefinition ModDefinition { get; set; }
        public ModReplacements? ModReplacements { get; set; }

        public static void WriteJson()
        {
            string s = JsonConvert.SerializeObject(
                new Root(MainWindow.LoadedModDefinition!)
                {
                    ModReplacements = MainWindow.LoadedModReplacements
                },
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }).Replace(@"\\", "/").Replace(Environment.NewLine, @"\n");
            File.WriteAllText($@"{MainWindow.LoadedModPath}\mod.json", s);
        }
    }

    public class Texture
    {
        public Texture(string path)
        {
            this.path = path;
        }
        public string path { get; set; }
        public int? face_x { get; set; }
        public int? face_y { get; set; }
        public int? costume_id { get; set; }
        public string? custom_name { get; set; }
        public string? custom_flavor { get; set; }
        public bool? single_file { get; set; }
    }

}
