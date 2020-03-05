using System;
using System.Collections.Generic;
using Xunit;

public class BimachineTests
{
    [Fact]
    public void ConstructionFromFstTest()
    {
        var fst = new Fst(
            new[] { 0, 1, 2 },
            new[] { 0 },
            new[] { 2 },
            new[] { (0, "a", "x", 1), (1, "b", "y", 2) });

        var bm = fst.ToBimachine(new HashSet<char> { 'a', 'b' });

        Assert.True(bm.Left.States.Count == 3 && bm.Right.States.Count == 3);
        Assert.True(bm.Left.Transitions.Count == 2 && bm.Right.Transitions.Count == 2);
        Assert.Equal(2, bm.Output.Count);

        Assert.Equal("xy", bm.Process("ab"));
        Assert.Throws<ArgumentException>(() => bm.Process("a"));
        Assert.Throws<ArgumentException>(() => bm.Process("abb"));
    }
}