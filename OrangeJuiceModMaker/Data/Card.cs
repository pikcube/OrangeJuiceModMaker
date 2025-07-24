using System.Diagnostics.CodeAnalysis;

namespace OrangeJuiceModMaker.Data;

public class Card
{
    public required string CardId { get; init; }
    public required string CardName { get; set; }
    public string? CardDescription { get; set; }
    public string? FlavorText { get; set; }
    public required string Path { get; set; }
    public required string PathLow { get; set; }

    [SetsRequiredMembers]
    private Card(CardRef cardRef)
    {
        CardId = cardRef.CardId;
        CardName = cardRef.CardName;
        CardDescription = cardRef.CardDescription;
        FlavorText = cardRef.FlavorText;
        Path = cardRef.Path;
        PathLow = cardRef.PathLow;
    }

    public static implicit operator Card(CardRef cardRef) => new Card(cardRef);
}