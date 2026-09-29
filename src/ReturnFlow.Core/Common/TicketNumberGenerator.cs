namespace ReturnFlow.Core.Common;

public static class TicketNumberGenerator
{
    private static readonly Random _rng = new();

    /// <summary>
    /// Generates a unique, human-readable RMA ticket number formatted like "RMA-9004".
    /// </summary>
    public static string Generate()
    {
        lock (_rng)
        {
            int number = _rng.Next(1000, 9999);
            return $"RMA-{number}";
        }
    }
}
