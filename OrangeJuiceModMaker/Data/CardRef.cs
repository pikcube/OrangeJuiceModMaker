namespace OrangeJuiceModMaker.Data;

public class CardRef
{
    public required string CardId { get; set; }
    public required string CardName { get; set; }
    public string? CardDescription { get; set; }
    public string? FlavorText { get; set; }
    public string Path => $"pakFiles/Cards/{CardId}256.png";
    public string PathLow => $"pakFiles/Cards/{CardId}256.png";
}