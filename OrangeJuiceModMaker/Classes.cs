using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Octokit;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace OrangeJuiceModMaker
{
    public enum PlayState
    {
        Stop = 0,
        Play = 1,
        Pause = 2,
    }

    public static class DebugLogger
    {
        public static void LogLine(string o) => log(o);

        public static void LogLine(object o) => LogLine(o.ToString() ?? string.Empty);

        private static Action<string> log = _ => throw new Exception("DebugLogger not initialized");

        public static void Initialize(bool debug)
        {
            log = debug ? Console.WriteLine : _ => { };
        }
    }

    public static class MyExtensions
    {
        public static bool IsNumber(this string text) => new Regex("[^0-9]+").IsMatch(text);

        public static bool IsInteger(this string text) => int.TryParse(text, out _);
        public static int ToInt(this string text) => int.Parse(text);
        public static int? ToIntOrNull(this string text) => int.TryParse(text, out int value) ? value : null;
        public static int ToIntOrDefault(this string text) => int.TryParse(text, out int value) ? value : 0;

        public static bool IsLong(this string text) => long.TryParse(text, out _);
        public static long ToLong(this string text) => long.Parse(text);
        public static long? ToLongOrNull(this string text) => long.TryParse(text, out long value) ? value : null;
        public static long ToLongOrDefault(this string text) => long.TryParse(text, out long value) ? value : 0;

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T item in list)
            {
                action(item);
            }
        }

        public static async void ForEachAsync<T>(this IAsyncEnumerable<T> list, Action<T> action)
        {
            await foreach (T item in list)
            {
                action(item);
            }
        }

        public static string StripStart(this string s, int length) => s.Length > length ? s[length..] : "";
        public static string StripEnd(this string s, int length) => s.Length > length ? s[..^length] : "";

        public static string AsString(this IEnumerable<string> list) => string.Join(Environment.NewLine, list);

        public static bool CompareFiles(string path1, string path2)
        {
            if (!File.Exists(path1) || !File.Exists(path2))
            {
                return false;
            }

            byte[] f1 = File.ReadAllBytes(path1);
            byte[] f2 = File.ReadAllBytes(path2);

            return f1.SequenceEqual(f2);
        }

        public static bool CompareFiles(FileInfo info1, FileInfo info2) => CompareFiles(info1.FullName, info2.FullName);
    }

    public class UpdateApp
    {
        private readonly App app;

        public UpdateApp(App app)
        {
            this.app = app;
        }

        private static bool SkipVersion(string newVersion)
        {
            string skipFile =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\release.version";

            if (!File.Exists(skipFile))
            {
                return false;
            }

            if (File.ReadAllText(skipFile) == newVersion)
            {
                return true;
            }

            File.Delete(skipFile);
            return false;
        }

        private static async Task<string> DownloadExeAsync(HttpClient client, Release release, bool isBeta, string downloadPath)
        {
            string path = downloadPath;
            Directory.CreateDirectory(path);
            path += @"\OJSetup.exe";
            HttpResponseMessage response = client.GetAsync(release.Assets[isBeta ? 0 : 1].BrowserDownloadUrl).Result;
            byte[] byteArray = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(path, byteArray);
            return path;
        }

        public async Task<bool> CheckForUpdate(string downloadPath, bool debug, string exeLocation)
        {
            using HttpClient client = new();
            Task<string> a = client.GetStringAsync(@"https://raw.githubusercontent.com/pikcube/OrangeJuiceModMaker/main/release.version");
            string[] version = (await File.ReadAllTextAsync($@"{exeLocation}\release.version")).Split(":");
            string[] checkedVersion = (await a).Split(":");
            bool isBeta = version[0] == "Beta";
            string checkedVersionString = isBeta ? checkedVersion[1] : checkedVersion[3];
            bool upToDate = version[1] == checkedVersionString;

            if (SkipVersion(checkedVersionString))
            {
                return true;
            }

            if (upToDate)
            {
                return true;
            }

            IReadOnlyList<Release>? releases = await new GitHubClient(new ProductHeaderValue("OrangeJuiceModUpdateChecker"))
                .Repository.Release.GetAll("pikcube", "OrangeJuiceModMaker");
            Release? release = releases.Where(z => !z.Prerelease || isBeta).MaxBy(z => z.CreatedAt) ?? null;
            if (release is null)
            {
                return true;
            }

            int? option = GetOption(debug);

            switch (option)
            {
                case 1:
                    string path = await DownloadExeAsync(client, release, isBeta, downloadPath);
                    Process.Start(path);
                    Environment.Exit(0);
                    return false;
                case 2:
                    app.PostAction = DownloadExeAsync(client, release, isBeta, downloadPath);
                    return true;
                case 3:
                    return true;
                case 4:
                    await File.WriteAllTextAsync(
                        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\release.version",
                        checkedVersionString);
                    return true;
                default:
                    Console.WriteLine("Invalid option");
                    return false;
            }
        }

        private static int? GetOption(bool debug)
        {
            if (!debug)
            {
                return new UpdateWindow().GetOption();
            }

            Console.WriteLine("New version of application released:");
            Console.WriteLine("1. Update now");
            Console.WriteLine("2. Update on exit");
            Console.WriteLine("3. Remind me later");
            Console.WriteLine("4. Skip this version");
            return Console.ReadLine()?.ToIntOrNull();
        }
    }

    internal class MusicList
    {
        public readonly string Name;
        public readonly string[] Id;
        public readonly string[] Description;

        public MusicList(CsvHolder csv)
        {
            if (csv.Type != CsvHolder.TypeList.Music)
            {
                throw new ArgumentException("CSV wasn't a music list");
            }

            Name = csv.Name;
            Id = csv.Rows.Select(z => z[0]).ToArray();
            Description = csv.Rows.Select(z => z[1]).ToArray();
        }
    }

    internal class ModMusic : Music
    {
        public enum SongType
        {
            UnitTheme = 1,
            EventTheme = 2,
        }

        public readonly SongType Song;
        public new string? File;
        public string Id
        {
            get
            {
                return Song switch
                {
                    SongType.EventTheme => Event,
                    SongType.UnitTheme => UnitId,
                    _ => throw new Exception("SongTypeNotSet"),
                } ?? string.Empty;
            }
            init
            {
                switch (Song)
                {
                    case SongType.UnitTheme:
                        UnitId = value;
                        break;
                    case SongType.EventTheme:
                        Event = value;
                        break;
                    default:
                        throw new ArgumentException("SongType not specified");
                }
            }
        }

        public ModMusic(string? file, SongType song) : base(file ?? string.Empty)
        {
            Song = song;
            File = file;
        }

        public ModMusic(Music music) : base(music.File)
        {
            File = music.File;
            Song = music.UnitId is not null ? SongType.UnitTheme : SongType.EventTheme;
            UnitId = music.UnitId;
            Event = music.Event;
            LoopPoint = music.LoopPoint;
            Volume = music.Volume;
        }

        public void SaveToMod(string modPath, ModDefinition definition, ref ModReplacements replacements)
        {
            if (File is null)
            {
                return;
            }

            Music newMusic = new($"music/{Path.GetFileNameWithoutExtension(File)}")
            {
                Event = Song == SongType.EventTheme ? Id : null,
                UnitId = Song == SongType.UnitTheme ? Id : null,
                LoopPoint = LoopPoint,
                Volume = Volume
            };
            switch (Song)
            {
                case SongType.EventTheme:
                    replacements.Music.RemoveAll(z => z.Event is not null && z.Event == newMusic.Event);
                    break;
                case SongType.UnitTheme:
                    replacements.Music.RemoveAll(z => z.UnitId is not null && z.UnitId == newMusic.UnitId);
                    break;
                default:
                    throw new Exception("Invalid Song Type");
            }

            replacements.Music.Add(newMusic);
            Root.WriteJson(modPath, definition, replacements);
        }
    }

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
                File.Copy(CurrentArtPath, $@"{modPath}\{Path}256.png");
                File.Copy(CurrentLowArtPath, $@"{modPath}\{Path}128.png");
            }

            Root.WriteJson(modPath, definition, replacements);
        }
    }

    public class ModifiedUnit
    {
        private readonly Unit baseUnit;
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
        public Music? Music { get; set; }
        public string[] CharacterArt { get; }
        public int[] FaceX { get; }
        public int[] FaceY { get; }

        public ModifiedUnit(Unit baseUnit, string baseResourcePath, ModReplacements? replacements, bool includeModData = true)
        {
            //Initialize Default Values for the no replacement case
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
            }

            //Search for Hyper Replacements
            for (int n = 0; n < HyperCardPaths.Length; ++n)
            {
                Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}256.png" == HyperCardPaths[n]);

                if (r is null)
                {
                    continue;
                }
                HyperCardPaths[n] = $@"{baseResourcePath}\{r.Path}256.png";
                HyperCardPathsLow[n] = $@"{baseResourcePath}\{r.Path}128.png";
                Texture.EnsureCardExists(r.Path, HyperCardPaths[n], HyperCardPathsLow[n]);
                HyperFlavor[n] = r.CustomFlavor ?? HyperFlavor[n];
                HyperNames[n] = r.CustomName ?? HyperNames[n];
            }

            //Search for Character Card Replacements
            for (int n = 0; n < CharacterCardPaths.Length; ++n)
            {
                Texture? r = replacements.Textures.FirstOrDefault(z => $@"pakFiles\{z.Path}256.png" == CharacterCardPaths[n]);
                if (r is null)
                {
                    continue;
                }
                CharacterCardPaths[n] = $@"{baseResourcePath}\{r.Path}256.png";
                CharacterCardPathsLow[n] = $@"{baseResourcePath}\{r.Path}128.png";
                Texture.EnsureCardExists(r.Path, CharacterCardPaths[n], CharacterCardPathsLow[n]);
                CharacterCardNames[n] = r.CustomName ?? CharacterCardNames[n];
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
            }
        }

        public void SaveToMod(string baseResourcePath, ModDefinition definition, ref ModReplacements replacements)
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

            for (int n = 0; n < HyperCardPaths.Length; ++n)
            {
                if (HyperCardPaths[n] == baseUnit.HyperCardPaths[n] && HyperNames[n] == baseUnit.HyperNames[n] &&
                    HyperFlavor[n] == baseUnit.HyperFlavor[n])
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

                string highPath = $@"{baseResourcePath}\cards\{HyperIds[n]}256.png";
                string lowPath = $@"{baseResourcePath}\cards\{HyperIds[n]}128.png";

                CopyFile(HyperCardPaths[n], highPath);

                CopyFile(HyperCardPathsLow[n], lowPath);
            }

            for (int n = 0; n < CharacterCardPaths.Length; ++n)
            {
                if (CharacterCardPaths[n] == baseUnit.CharacterCardPaths[n] &&
                    CharacterCardNames[n] == baseUnit.CharacterCardNames[n])
                {
                    continue;
                }

                replacements.Textures.RemoveAll(z => z.Path == $@"cards\{CharacterCards[n]}");
                Texture t = new($@"cards\{CharacterCards[n]}")
                {
                    CustomName = CharacterCardNames[n]
                };
                replacements.Textures.Add(t);

                string highDestPath = $@"{baseResourcePath}\cards\{CharacterCards[n]}256.png";
                CopyFile(CharacterCardPaths[n], highDestPath);

                string lowDestPath = $@"{baseResourcePath}\cards\{CharacterCards[n]}128.png";
                CopyFile(CharacterCardPathsLow[n], lowDestPath);

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

        public Unit(string[] row, CsvHolder characterCards, MainWindow mainWindow)
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
                    Console.WriteLine(error.AsString());
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
                    HyperIds = Array.Empty<string>();
                    HyperNames = Array.Empty<string>();
                    break;
                case "1":
                    //Normal People
                    HyperIds = new[] { row[3] };
                    HyperNames = new[] { FindUnitHyperNameById(row[3], mainWindow.CsvFiles) };
                    break;
                case "2":
                    //TWO WHOLE HYPERS?
                    HyperIds = new[] { row[3], row[4] };
                    HyperNames = new[] { FindUnitHyperNameById(row[3], mainWindow.CsvFiles), FindUnitHyperNameById(row[4], mainWindow.CsvFiles) };
                    break;
                case "-1":
                    //Way Too Many Hypers. Probably a boss
                    CsvHolder file = mainWindow.CsvFiles.First(z => z.Name == row[3]);
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
                            HyperNames[i] = FindUnitHyperNameById(row[i + 3], mainWindow.CsvFiles);
                        }
                    }
                    else
                    {
                        throw new FormatException("Bad Row");
                    }
                    break;
            }

            CharacterCardPaths = CharacterCards
                .Select(c => mainWindow.Cards.First(z => Path.GetFileNameWithoutExtension(z) == $"{c}256")).ToArray();
            HyperCardPaths = HyperIds
                .Select(z => mainWindow.Cards.First(x => Path.GetFileNameWithoutExtension(x) == $"{z}256")).ToArray();
            CharacterCardPathsLow = CharacterCards
                .Select(c => mainWindow.Cards.First(z => Path.GetFileNameWithoutExtension(z) == $"{c}128")).ToArray();
            HyperCardPathsLow = HyperIds
                .Select(z => mainWindow.Cards.First(x => Path.GetFileNameWithoutExtension(x) == $"{z}128")).ToArray();

            HyperFlavor = new string[HyperIds.Length];
            if (mainWindow.FlavorLookUp is null)
            {
                return;
            }
            for (int n = 0; n < HyperIds.Length; ++n)
            {
                HyperFlavor[n] = mainWindow.FlavorLookUp.Rows.FirstOrDefault(z => z[1] == HyperIds[n])?[3] ?? "";
            }
        }

        private static string FindUnitHyperNameById(string cardId, List<CsvHolder> csvFiles)
        {
            return csvFiles.First(z => z.Name == "HyperCards").Rows.First(z => z[1] == cardId)[0];
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

            string typeName = parser.ReadLine()!;
            if (!Enum.TryParse(typeName, true, out Type))
            {
                Type = TypeList.Undefined;
            }


            Headers = parser.ReadFields() ?? throw new InvalidOperationException();

            while (!parser.EndOfData)
            {
                rawRows.Add(parser.ReadFields() ?? throw new InvalidOperationException());
            }

            Rows = rawRows.ToArray();

        }

        public enum TypeList
        {
            Unit = 0,
            Card = 1,
            Lookup = 2,
            Music = 3,
            Voice = 4,
            Sound = 5,
            Undefined = -1
        }

        public readonly TypeList Type;

        public readonly string Name;

        public string[][] Rows { get; }

        public string[] GetRow(int rowNumber)
        {
            return Rows[rowNumber];
        }

        public string[] GetColumn(int columnNumber)
        {
            return Rows.Select(z => z[columnNumber]).ToArray();
        }

        private string[] Headers { get; }
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
    public class Pet
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("floating")]
        public bool Floating;

        [JsonProperty("face_x")]
        public int FaceX;

        [JsonProperty("face_y")]
        public int FaceY;

        [JsonProperty("textures")]
        public List<PetTexture> Textures;

        [JsonProperty("layers")]
        public List<Layer> Layers;

        [JsonProperty("draw_offset_x")]
        public int DrawOffsetX;

        [JsonProperty("draw_offset_y")]
        public int DrawOffsetY;

        public Pet(string id, List<PetTexture> textures, List<Layer> layers, bool floating = false, int faceX = 128, int faceY = 128, int drawOffsetX = 0, int drawOffsetY = 0)
        {
            Id = id;
            Textures = textures;
            Layers = layers;
            FaceX = faceX;
            FaceY = faceY;
            Floating = floating;
            DrawOffsetX = drawOffsetX;
            DrawOffsetY = drawOffsetY;
        }
    }

    public class PetTexture
    {
        [JsonProperty("layer")]
        public string Layer;
        [JsonProperty("path")]
        public string Path;

        public PetTexture(string layer, string path)
        {
            Layer = layer;
            Path = path;
        }
    }

    public class ModReplacements
    {
        [JsonProperty("textures")]
        public List<object> AllTextures { get; set; }

        [JsonIgnore]
        public List<string> BasicTextures { get; set; }
        
        [JsonIgnore]
        public List<Texture> Textures { get; set; }

        [JsonProperty("music")]
        public List<Music> Music { get; set; }
        [JsonProperty("sound_effects")]
        public List<string> SoundEffects { get; set; }
        [JsonProperty("pets")]
        public List<Pet> Pets;
        [JsonProperty("voices")]
        public Voices Voices { get; set; }

        public ModReplacements()
        {
            AllTextures = new List<object>();
            Music = new List<Music>();
            SoundEffects = new List<string>();
            Pets = new List<Pet>();
            Voices = new Voices();
            Textures = new List<Texture>();
            BasicTextures = new List<string>();
        }
    }

    public class Voices
    {
        [JsonProperty("character")]
        public List<string> Character { get; set; }

        [JsonProperty("system")]
        public List<string> System { get; set; }

        public Voices()
        {
            Character = new List<string>();
            System = new List<string>();
        }
    }

    public class Layer
    {
        public static readonly int[] VariantLayers = { 0, 1, 2, 3, 4, 5 };
        
        private int variant;

        [JsonProperty("variant")] 
        public int Variant
        {
            get => variant;
            set
            {
                if(VariantLayers.Contains(value))
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

    public class Music
    {
        public Music(string file)
        {
            File = file;
        }

        [JsonProperty("unit_id")]
        public string? UnitId { get; set; }
        [JsonProperty("event")] 
        public string? Event { get; set; }

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

        public static void WriteJson(string modPath, ModDefinition definition, ModReplacements replacements)
        {
            RepairMod(ref replacements, modPath);
            WriteJson(new Root(definition)
            {
                ModReplacements = replacements
            }, $@"{modPath}\mod.json");
        }

        public static void WriteJson(Root root, string path)
        {
            root.ModReplacements?.AllTextures.Clear();
            root.ModReplacements?.AllTextures.AddRange(root.ModReplacements.Textures);
            root.ModReplacements?.AllTextures.AddRange(root.ModReplacements.BasicTextures);
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
            Root? r = JsonConvert.DeserializeObject<Root>(File.ReadAllText(path).Replace("/", @"\\"));
            if (r?.ModReplacements is null)
            {
                return r;
            }

            ModReplacements rModReplacements = r.ModReplacements;
            foreach (string o in rModReplacements.AllTextures.Select(z => z.ToString() ?? ""))
            {
                try
                {
                    Texture? t = JsonConvert.DeserializeObject<Texture>(o);
                    if (t is null)
                    {
                        continue;
                    }

                    if (rModReplacements.Textures.Any(z => z.Path == t.Path))
                    {
                        Texture existing = rModReplacements.Textures.First(z => z.Path == t.Path);
                        existing.CostumeId ??= t.CostumeId;
                        existing.CustomFlavor ??= t.CustomFlavor;
                        existing.CustomName ??= t.CustomName;
                        existing.FaceX ??= t.FaceX;
                        existing.FaceY ??= t.FaceY;
                        existing.SingleFile ??= t.SingleFile;
                    }
                    else
                    {
                        rModReplacements.Textures.Add(t);
                    }
                }
                catch
                {
                    ConvertToModFormat(o);
                }
            }

            bool validateAndRepair = rModReplacements.BasicTextures.Any();
            rModReplacements.Textures = rModReplacements.Textures.DistinctBy(z => z.Path).ToList();
            rModReplacements.BasicTextures = rModReplacements.BasicTextures.Distinct().ToList();
            rModReplacements.BasicTextures.RemoveAll(z => rModReplacements.Textures.Any(x => x.Path == z));

            if (validateAndRepair)
            {
                RepairMod(ref rModReplacements, Path.GetDirectoryName(path) ?? throw new ArgumentNullException(nameof(path), "Can't get directory"));
            }

            r.ModReplacements = rModReplacements;
            return r;

            void ConvertToModFormat(string o)
            {
                if (MainWindow.Instance is null)
                {
                    return;
                }
                string type = o.Split(@"\").First();
                switch (type)
                {
                    case "units":
                        if (File.Exists($@"{MainWindow.Instance.AppData}\pakFiles\{o}.png"))
                        {
                            rModReplacements.Textures.Add(new Texture(o));
                            return;
                        }

                        if (File.Exists($@"{MainWindow.Instance.AppData}\pakFiles\{o}_00.png"))
                        {
                            for (int n = 0;
                                 File.Exists(
                                     $@"{MainWindow.Instance.AppData}\pakFiles\{o}_{n:00}.png");
                                 ++n)
                            {
                                rModReplacements.Textures.Add(new Texture($"{o}_{n:00}"));
                            }

                            return;
                        }

                        rModReplacements.BasicTextures.Add(o);
                        return;
                    case "cards":
                        if (File.Exists($@"{MainWindow.Instance.AppData}\pakFiles\{o}.png"))
                        {
                            rModReplacements.Textures.Add(new Texture(o));
                            return;
                        }

                        if (File.Exists($@"{MainWindow.Instance.AppData}\pakFiles\{o}256.png"))
                        {
                            rModReplacements.Textures.Add(new Texture($"{o}"));
                            return;
                        }

                        rModReplacements.BasicTextures.Add(o);
                        return;
                    case "alphamasks":
                    case "hats":
                    case "hairs":
                    case "field":
                        rModReplacements.BasicTextures.Add(o);
                        return;
                    default:
                        return;
                }
            }
        }

        public static bool IsValidMod(ModReplacements replacements, string modPath, out List<string> missing)
        {
            missing = new List<string>();

            missing.AddRange(replacements.Textures.Where(z => BadUnit(z, modPath)).Select(z => $"{z.Path}.png"));
            missing.AddRange(replacements.Textures.Where(z => BadCard128(z, modPath)).Select(z => $"{z.Path}.png"));
            missing.AddRange(replacements.Textures.Where(z => BadCard256(z, modPath)).Select(z => $"{z.Path}.png"));

            missing.AddRange(replacements.Music.Where(z => BadMusic(z, modPath)).Select(z => $"{z.File}.ogg"));

            return missing.Count == 0;
        }

        private static bool BadMusic(Music z, string modPath) => BadItem(z.File, @"music\", modPath, ".ogg");
        private static bool BadUnit(Texture z, string modPath) => BadItem(z.Path, @"units\", modPath, ".png");
        private static bool BadCard256(Texture z, string modPath) => BadItem(z.Path, @"cards\", modPath, "256.png");
        private static bool BadCard128(Texture z, string modPath) => BadItem(z.Path, @"cards", modPath, "128.png");
        private static bool BadItem(string path, string directory, string modPath, string suffix) =>
            path.StartsWith(directory) && !File.Exists($@"{modPath}\{path}{suffix}");

        private static bool RedundantItem(string testPath, ModReplacements replacements)
        {
            return testPath[..5] switch
            {
                "music" => replacements.Music.All(z => z.File != testPath),
                "cards" => replacements.Textures.All(z => z.Path != testPath.StripEnd(3)),
                "units" => replacements.Textures.All(z => z.Path != testPath),
                _ => true
            };
        }

        public static void RepairMod(ref ModReplacements replacements, string modPath)
        {
            replacements.Music.RemoveAll(z => BadMusic(z, modPath));
            replacements.Textures.RemoveAll(z => BadCard128(z, modPath));
            replacements.Textures.RemoveAll(z => BadCard256(z, modPath));
            replacements.Textures.RemoveAll(z => BadUnit(z, modPath));
            replacements.Textures.RemoveAll(z => IsUnmodifiedItem(z, modPath));
        }

        private static bool IsUnmodifiedItem(Texture t, string modPath)
        {
            switch (t.Path[..5].ToLower())
            {
                case "units":
                    return FilesMatch(File.ReadAllBytes($@"{modPath}\{t.Path}.png"), File.ReadAllBytes($@"pakFiles\{t.Path}.png"));
                case "cards":
                    if (t.CustomName is not null)
                    {
                        return false;
                    }
                    if (t.CustomFlavor is not null)
                    {
                        return false;
                    }
                    return FilesMatch(File.ReadAllBytes($@"{modPath}\{t.Path}.png"), File.ReadAllBytes($@"pakFiles\{t.Path}.png"));
                default:
                    return false;
            }
        }

        private static bool FilesMatch(byte[] fileA, byte[] fileB)
        {
            return fileA.Length == fileB.Length && fileA.Zip(fileB).All(bytes => bytes.First == bytes.Second);
        }

        public static int CleanMod(ModReplacements replacements, string modLocation)
        {
            int redundantFiles = 0;
            foreach (string filePath in Directory.GetFiles($@"{modLocation}\music"))
            {
                string testName = $@"music\{Path.GetFileNameWithoutExtension(filePath)}";
                if (!RedundantItem(testName, replacements))
                {
                    continue;
                }
                File.Delete(filePath);
                ++redundantFiles;
            }

            foreach (string filePath in Directory.GetFiles($@"{modLocation}\cards"))
            {
                string testName = $@"cards\{Path.GetFileNameWithoutExtension(filePath)}";
                if (!RedundantItem(testName, replacements))
                {
                    continue;
                }
                File.Delete(filePath);
                ++redundantFiles;
            }

            foreach (string filePath in Directory.GetFiles($@"{modLocation}\units"))
            {
                string testName = $@"units\{Path.GetFileNameWithoutExtension(filePath)}";
                if (!RedundantItem(testName, replacements))
                {
                    continue;
                }
                File.Delete(filePath);
                ++redundantFiles;
            }
            return redundantFiles;
        }
    }

    public class RootRepair
    {
        public RootRepair(ModDefinition modDefinition)
        {
            ModDefinition = modDefinition;
        }

        [JsonProperty("ModDefinition")]
        public ModDefinition ModDefinition { get; set; }

        [JsonProperty("ModReplacements")]
        public ModReplacementsRepair? ModReplacements { get; set; }

        public class ModReplacementsRepair
        {
            [JsonProperty("textures")]
            public List<string> Textures { get; set; }

            [JsonProperty("music")]
            public List<Music> Music { get; set; }
            [JsonProperty("sound_effects")]
            public List<string> SoundEffects { get; set; }
            [JsonProperty("pets")]
            public List<Pet> Pets;
            [JsonProperty("voices")]
            public Voices Voices { get; set; }

            public ModReplacementsRepair()
            {
                Textures = new List<string>();
                Music = new List<Music>();
                SoundEffects = new List<string>();
                Pets = new List<Pet>();
                Voices = new Voices();
            }
        }
    }

    public class Texture
    {
        public Texture(string path)
        {
            Path = path;
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

            void ResizeImage(string initialPath, int newSize, string finalPath)
            {
                MagickImage highQualityArt = new(initialPath);
                highQualityArt.Resize(newSize, newSize);
                highQualityArt.Write(finalPath);
            }

            void ReplaceMissingFile()
            {
                MessageBoxManager.Yes = "Find Image";
                MessageBoxManager.No = "Use Default";
                MessageBoxManager.Cancel = "Upscale";
                MessageBoxManager.Register();
                DialogResult option = MessageBox.Show(
                    "Card texture is missing. Would you like to find the file, " +
                    "upscale the small file, or replace with the default?",
                    "Missing 256.png file", MessageBoxButtons.YesNoCancel);
                MessageBoxManager.Unregister();
                switch (option)
                {
                    case DialogResult.No:
                        File.Copy(defaultArtPath, currentArtPath);
                        break;
                    case DialogResult.Cancel:
                        ResizeImage(currentLowArtPath, 256, currentArtPath);
                        break;
                    case DialogResult.Yes:
                        OpenFileDialog o = new()
                        {
                            Title = "Select 256x256 png or dds",
                            Filter =
                                "Portable Network Graphics (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
                        };
                        if (o.ShowDialog() is not true)
                        {
                            goto default;
                        }

                        using (MagickImage image = new(o.FileName))
                        {
                            image.Format = MagickFormat.Png;
                            image.Resize(256, 256);
                            image.Write(currentArtPath);
                        }

                        break;
                    case DialogResult.None:
                    case DialogResult.OK:
                    case DialogResult.Abort:
                    case DialogResult.Retry:
                    case DialogResult.Ignore:
                    case DialogResult.TryAgain:
                    case DialogResult.Continue:
                    default:
                        goto case DialogResult.No;
                }
            }
        }
    }
}
