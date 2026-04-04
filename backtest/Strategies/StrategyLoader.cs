using System.Reflection;
using StockBacktest.Contracts;

namespace StockBacktest.Strategies;

internal static class StrategyLoader
{
    private static readonly IReadOnlyList<Type> StrategyTypes =
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IStrategy).IsAssignableFrom(t))
            .ToList();

    // Fresh instance per call — strategies may hold per-run state
    public static IStrategy? Find(string name) =>
        StrategyTypes
            .Select(t => (IStrategy)Activator.CreateInstance(t)!)
            .FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<string> ListAvailable() =>
        StrategyTypes.Select(t => ((IStrategy)Activator.CreateInstance(t)!).Name);
}
