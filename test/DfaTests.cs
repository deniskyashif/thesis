using System.Collections.Generic;
using System.Linq;
using Xunit;

public class DfaTests
{
    [Fact]
    public void DfaRecognizeTest()
    {
        // a|bc+
        var states = new[] { 0, 1, 2, 3 };
        var initial = 0;
        var final = new[] { 1, 3 };
        var transitions = new Dictionary<(int, string), int>()
        {
            { (0, "a"), 1 },
            { (0, "b"), 2 },
            { (2, "c"), 3 },
            { (3, "c"), 3 }
        };
        var dfsa = new Dfsa(states, initial, final, transitions);

        Assert.True(new[] { "a", "bc", "bcccc" }.All(dfsa.Recognize));
        Assert.DoesNotContain(new[] { "aa", "ab", "abc", "b", "abcc", "c" }, dfsa.Recognize);
    }
}