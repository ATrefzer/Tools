namespace ReturnCalc.Models;

public class SimulationResult
{
    public string Strategy { get; init; } = "";
    public decimal InitialCapital { get; init; }
    public decimal FinalCapital { get; init; }
    public decimal ReturnPercent { get; init; }
    public IReadOnlyList<SimulationStep> Steps { get; init; } = [];
}