namespace OrangeJuiceModMaker.Data;

public class UnitRef
{
    public required string UnitId { get; init; }
    public required string UnitName { get; init; }
    public required string[] HyperCards { get; init; }
    public required string[] CharacterCards { get; init; }
}