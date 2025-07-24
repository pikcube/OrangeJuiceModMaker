using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker.Data;

public class Root(ModDefinition modDefinition)
{
    [JsonProperty(nameof(ModDefinition))]
    public ModDefinition ModDefinition { get; set; } = modDefinition;

    [JsonProperty(nameof(ModReplacements))]
    public ModReplacements? ModReplacements { get; set; }

    public static void WriteJson(string modPath, ModDefinition definition, ModReplacements replacements)
    {
        RepairMod(replacements, modPath);
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

        bool validateAndRepair = rModReplacements.BasicTextures.Count != 0;
        rModReplacements.Textures = [.. rModReplacements.Textures.DistinctBy(z => z.Path)];
        rModReplacements.BasicTextures = [.. rModReplacements.BasicTextures.Distinct()];
        rModReplacements.BasicTextures.RemoveAll(z => rModReplacements.Textures.Any(x => x.Path == z));

        if (validateAndRepair)
        {
            RepairMod(rModReplacements, Path.GetDirectoryName(path) ?? throw new ArgumentNullException(nameof(path), "Can't get directory"));
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
        missing =
        [
            .. replacements.Textures.Where(z => BadUnit(z, modPath)).Select(z => $"{z.Path}.png"),
            .. replacements.Textures.Where(z => BadCard128(z, modPath)).Select(z => $"{z.Path}.png"),
            .. replacements.Textures.Where(z => BadCard256(z, modPath)).Select(z => $"{z.Path}.png"),
            .. replacements.Music.Where(z => BadMusic(z, modPath)).Select(z => $"{z.File}.ogg"),
        ];

        return missing.Count == 0;
    }

    public static bool IsValidMod(string modPath, out List<string> missing)
    {
        var r = ReadJson(modPath + @"\mod.json");
        if (r is null || r.ModReplacements is null)
        {
            missing = [];
            return false;
        }

        return IsValidMod(r.ModReplacements, modPath, out missing);
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

    public static void RepairMod(ModReplacements replacements, string modPath)
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