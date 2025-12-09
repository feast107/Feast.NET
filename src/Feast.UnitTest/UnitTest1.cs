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
        var arr = new string[1000];
        Array.Fill(arr, "abc");
        for (var i = 0; i < 5; i++)
        {
            var watch = Stopwatch.StartNew();
            //var conv = arr.Select(Transform).ToArray();
            var conv = new string[1000];
            for (var j = 0; j < arr.Length; j++)
            {
                conv[j] = Transform(arr[j]);
            }
            var cost = watch.ElapsedTicks;
            Console.WriteLine(cost);
        }

        string Transform(string s) => s.ToUpperInvariant();
    }
    
}