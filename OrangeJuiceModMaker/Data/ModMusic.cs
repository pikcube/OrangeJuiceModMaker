using System;
using System.IO;

namespace OrangeJuiceModMaker.Data;

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

    public void SaveToMod(string modPath, ModDefinition definition, ModReplacements replacements)
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