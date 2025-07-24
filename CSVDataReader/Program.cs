using Csv;
using ITable;
using Newtonsoft.Json;

namespace CSVDataReader;

internal class Program
{
    static void Main(string[] args)
    {
        List<Music> allMusic = [];
        List<Sound> allSound = [];
        LoadMusicFile(allMusic, "./csvFiles/Music/Events.csv", row => new Music
        {
            Event = row[0],
            Description = row[1]
        });
        LoadMusicFile(allMusic, "./csvFiles/Music/UnitThemes.csv", row => new Music
        {
            UnitId = row[0],
            Description = row[1]
        });
        LoadSoundFile(allSound, "./csvFiles/Music/SoundEffects.csv", row => new Sound
        {
            File = row[0],
            Description = row[1]
        });

        File.WriteAllText("music.json", JsonConvert.SerializeObject(allMusic));
        File.WriteAllText("sound.json", JsonConvert.SerializeObject(allSound));


    }

    public static void LoadMusicFile(List<Music> allMusic, string path, Func<List<string>, Music> func)
    {
        CsvCells cells = new(File.ReadAllText(path));
        List<List<string>> rows = [.. cells.ToListOfLists().Select(z => z.ToList())];
        rows.RemoveAt(0);
        rows.RemoveAt(0);

        foreach (List<string> row in rows.Where(r => r.Count > 0))
        {
            Music m = func(row);
            allMusic.Add(m);
        }
    }

    public static void LoadSoundFile(List<Sound> allSounds, string path, Func<List<string>, Sound> func)
    {
        CsvCells cells = new(File.ReadAllText(path));
        List<List<string>> rows = [.. cells.ToListOfLists().Select(z => z.ToList())];
        rows.RemoveAt(0);
        rows.RemoveAt(0);

        foreach (List<string> row in rows.Where(r => r.Count > 0))
        {
            Sound m = func(row);
            allSounds.Add(m);
        }
    }

    public static void LoadUnitFile(List<Unit> allUnits, string path, Func<List<string>, Unit> func)
    {
        CsvCells cells = new(File.ReadAllText(path));
        List<List<string>> rows = [.. cells.ToListOfLists().Select(z => z.ToList())];
        rows.RemoveAt(0);
        rows.RemoveAt(0);
        foreach (List<string> row in rows.Where(r => r.Count > 1))
        {
            Unit u = func(row);
            int index = allUnits.FindIndex(z => z.UnitId == u.UnitId);
            if (index == -1)
            {
                allUnits.Add(u);
            }
        }
    }

    private static void LoadCardFile(List<Card> allCards, string path, Func<List<string>, Card> func)
    {
        CsvCells cells = new(File.ReadAllText(path));
        List<List<string>> rows = [.. cells.ToListOfLists().Select(z => z.ToList())];
        rows.RemoveAt(0);
        rows.RemoveAt(0);
        foreach (List<string> row in rows.Where(r => r.Count > 1))
        {
            Card c = func(row);
            int index = allCards.FindIndex(z => z.CardId == c.CardId);
            if (index == -1)
            {
                c.Tags.Add(Path.GetFileNameWithoutExtension(path));
                allCards.Add(c);
            }
            else
            {
                allCards[index].CardName ??= c.CardName;
                allCards[index].CardDescription ??= c.CardDescription;
                allCards[index].FlavorText ??= c.FlavorText;
                allCards[index].Tags.Add(Path.GetFileNameWithoutExtension(path));
            }
        }
    }
}

public class Music
{
    public string? UnitId { get; set; }
    public string? Event { get; set; }
    public string Description { get; set; }
}

public class Sound
{
    public string File { get; set; }
    public string Description { get; set; }
}

public class Unit
{
    public string UnitId { get; set; }
    public string UnitName { get; set; }
    public List<string> HyperCards { get; set; }
    public List<string> CharacterCards { get; set; }
}

public class Card
{
    public required string CardId { get; set; }
    public string? CardName { get; set; }
    public string? CardDescription { get; set; }
    public string? FlavorText { get; set; }
    [JsonIgnore]
    public List<string> Tags { get; init; } = [];
}

public class CharacterCard : Card
{
    public string UnitId { get; set; }
}