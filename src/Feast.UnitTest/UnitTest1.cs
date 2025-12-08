using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace Feast.UnitTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test()
    {
        var source = new CancellationTokenSource();

        source.Token.Register(_ =>
        {
            Console.WriteLine("Cancellation requested");
        }, null);
        
        await source.CancelAsync();

        source.Token.Register(_ =>
        {
            Console.WriteLine("Cancellation requested");
        }, null);

        
        await source.CancelAsync();
    }

    [Test]
    public void Match()
    {
        var wrap = new Wrap("A", "B");
        var eq   = wrap.Equals(new Wrap("A", "B"));
        Debugger.Break();
    }

    record Wrap(string A, string B);
    
}