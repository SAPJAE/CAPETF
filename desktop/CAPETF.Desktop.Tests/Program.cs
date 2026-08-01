using CAPETF.Desktop.Tests;

var completedSuites = TestSuiteRunner.Run(
    args,
    SyntheticTradingTests.RunAll,
    SyntheticBasketBuilderTests.RunAll,
    SyntheticBasketBuilderTests.RunCryptoUniverse);
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
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(runTrading);
        ArgumentNullException.ThrowIfNull(runBuilder);
        ArgumentNullException.ThrowIfNull(runCryptoUniverse);

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
        if (selection == "crypto-universe")
        {
            runCryptoUniverse();
            completed.Add("CryptoUniverse");
        }
        if (completed.Count == 0)
        {
            throw new ArgumentException("Use no filter for the full suite, or specify 'trading', 'builder', or 'crypto-universe'.", nameof(arguments));
        }
        return completed;
    }
}
