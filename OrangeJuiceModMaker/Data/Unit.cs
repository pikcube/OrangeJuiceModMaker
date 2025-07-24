using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrangeJuiceModMaker.Data;

public class Unit
{
    public string UnitId { get; }
    public string UnitName { get; }

    public CardRef[] HyperCards { get; }
    public CardRef[] CharacterCards { get; }
    public string[] CharacterArt => getCharacterArt.Result;

    private readonly Task<string[]> getCharacterArt;

    //[JsonConstructor]
    //public Unit(string unitId, string unitName, string[] hyperIds, string[] hyperNames, string[] hyperFlavor, string[] hyperCardPaths, string[] hyperCardPathsLow, string[] characterCards, string[] characterCardNames, string[] characterCardPaths, string[] characterCardPathsLow, string[] characterArt)
    //{
    //    UnitId = unitId;
    //    UnitName = unitName;
    //    HyperIds = hyperIds;
    //    HyperNames = hyperNames;
    //    HyperFlavor = hyperFlavor;
    //    HyperCardPaths = hyperCardPaths;
    //    HyperCardPathsLow = hyperCardPathsLow;
    //    CharacterCards = characterCards;
    //    CharacterCardNames = characterCardNames;
    //    CharacterCardPaths = characterCardPaths;
    //    CharacterCardPathsLow = characterCardPathsLow;
    //    getCharacterArt = Task.Run(() => characterArt);
    //}

    public static implicit operator Unit(UnitRef unitRef) => new Unit(MainWindow.Instance!, unitRef.UnitId);


    public Unit(MainWindow mainWindow, string unitId)
    {
        UnitId = unitId;
        UnitRef unitRef = mainWindow.Units.Single(u => u.UnitId == unitId);
        UnitName = unitRef.UnitName;
        HyperCards = [.. mainWindow.Cards.Where(c => unitRef.HyperCards.Contains(c.CardId))];
        CharacterCards = [.. mainWindow.Cards.Where(c => unitRef.CharacterCards.Contains(c.CardId))];


        getCharacterArt = Task.Run(() =>
        {
            try
            {
                string[] characterArt = [.. Directory.GetFiles(@"pakFiles\units").Where(z =>
                {
                    string s = Path.GetFileNameWithoutExtension((string?)z) ?? throw new NoNullAllowedException();
                    return s.StartsWith($"{UnitId}_00_") && $"{UnitId}_00_".Length + 2 == s.Length;
                })];

                if (characterArt.Length == 0)
                {
                    characterArt = [.. Directory.GetFiles(@"pakFiles\units").Where(z =>
                    {
                        string s = Path.GetFileNameWithoutExtension(z);
                        return s.StartsWith($"{UnitId}_00_00_") && $"{UnitId}_00_00_".Length + 2 == s.Length;
                    })];
                }

                return characterArt;
            }
            catch (Exception exception)
            {
                string[] error =
                    [DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? ""];
                Console.WriteLine(error.AsString());
                File.WriteAllLines("unit_class_error.txt", error);
                MainWindow.ExitTime = true;
                throw;
            }
        });
    }
}