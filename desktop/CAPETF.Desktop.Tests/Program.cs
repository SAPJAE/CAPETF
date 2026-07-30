using CAPETF.Desktop.Tests;

if (args is ["trading"])
{
    SyntheticTradingTests.RunAll();
    Console.WriteLine("SyntheticTrading tests passed");
    return;
}

SyntheticBasketBuilderTests.RunAll();
Console.WriteLine("SyntheticBasketBuilder tests passed");
