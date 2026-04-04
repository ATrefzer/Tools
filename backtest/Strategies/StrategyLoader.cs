using System.Reflection;
using StockBacktest.Contracts;

namespace StockBacktest.Strategies;

internal static class StrategyLoader
{
    private static readonly IReadOnlyList<IStrategy> All =
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IStrategy).IsAssignableFrom(t))
            .Select(t => (IStrategy)Activator.CreateInstance(t)!)
            .ToList();

    public static IStrategy? Find(string name)
    {
        return All.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> ListAvailable()
    {
        return All.Select(s => s.Name);
    }
}