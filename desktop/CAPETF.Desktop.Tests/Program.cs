using CAPETF.Desktop.Tests;

var completedSuites = TestSuiteRunner.Run(
    args,
    SyntheticTradingTests.RunAll,
    SyntheticBasketBuilderTests.RunAll,
    SyntheticBasketBuilderTests.RunCryptoUniverse,
    SyntheticBasketBuilderTests.RunManualFormula);
foreach (var suite in completedSuites)
{
    Console.WriteLine($"{suite} tests passed");
}

internal static class TestSuiteRunner
{
    public static IReadOnlyList<string> Run(
        IReadOnlyList<string> arguments,
        Action runTrading,
        Action runBuilder) =>
        Run(arguments, runTrading, runBuilder, () => { });

    public static IReadOnlyList<string> Run(
        IReadOnlyList<string> arguments,
        Action runTrading,
        Action runBuilder,
        Action runCryptoUniverse)
        => Run(arguments, runTrading, runBuilder, runCryptoUniverse, () => { });

    public static IReadOnlyList<string> Run(
        IReadOnlyList<string> arguments,
        Action runTrading,
        Action runBuilder,
        Action runCryptoUniverse,
        Action runManualFormula)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(runTrading);
        ArgumentNullException.ThrowIfNull(runBuilder);
        ArgumentNullException.ThrowIfNull(runCryptoUniverse);
        ArgumentNullException.ThrowIfNull(runManualFormula);

        var selection = arguments.Count == 0 ? "full" : arguments.Count == 1 ? arguments[0].Trim().ToLowerInvariant() : "invalid";
        var completed = new List<string>(3);
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
        if (selection is "crypto-universe" or "crypto-ui")
        {
            runCryptoUniverse();
            completed.Add("CryptoUniverse");
        }
        if (selection == "manual-formula")
        {
            runManualFormula();
            completed.Add("ManualFormula");
        }
        if (completed.Count == 0)
        {
            throw new ArgumentException("Use no filter for the full suite, or specify 'trading', 'builder', 'crypto-universe', or 'manual-formula'.", nameof(arguments));
        }
        return completed;
    }
}
