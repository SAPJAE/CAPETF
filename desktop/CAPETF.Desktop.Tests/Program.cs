using CAPETF.Desktop.Tests;

var completedSuites = TestSuiteRunner.Run(
    args,
    SyntheticTradingTests.RunAll,
    SyntheticBasketBuilderTests.RunAll);
foreach (var suite in completedSuites)
{
    Console.WriteLine($"{suite} tests passed");
}

internal static class TestSuiteRunner
{
    public static IReadOnlyList<string> Run(
        IReadOnlyList<string> arguments,
        Action runTrading,
        Action runBuilder)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(runTrading);
        ArgumentNullException.ThrowIfNull(runBuilder);

        var selection = arguments.Count == 0 ? "full" : arguments.Count == 1 ? arguments[0].Trim().ToLowerInvariant() : "invalid";
        var completed = new List<string>(2);
        if (selection is "full" or "trading")
        {
            runTrading();
            completed.Add("SyntheticTrading");
        }
        if (selection is "full" or "builder")
        {
            runBuilder();
            completed.Add("SyntheticBasketBuilder");
        }
        if (completed.Count == 0)
        {
            throw new ArgumentException("Use no filter for the full suite, or specify 'trading' or 'builder'.", nameof(arguments));
        }
        return completed;
    }
}
