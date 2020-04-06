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

        var bm = fst.ToBimachine(new HashSet<char> { 'a', 'b' }).PseudoMinimal();

        Assert.True(bm.Forward.States.Count == 3 && bm.Reverse.States.Count == 3);
        Assert.True(bm.Forward.Transitions.Count == 2 && bm.Reverse.Transitions.Count == 2);
        Assert.Equal(2, bm.Output.Count);

        Assert.Equal("xy", bm.Process("ab"));
        Assert.Throws<ArgumentException>(() => bm.Process("a"));
        Assert.Throws<ArgumentException>(() => bm.Process("abb"));
    }

    [Fact]
    public void ConstructionFromFstTest1()
    {
        // { (a, x), (ab, y) }
        var fst = new Fst(
            new[] { 0, 1, 2, 3 },
            new[] { 0 },
            new[] { 1, 3 },
            new[] { (0, "a", "x", 1), (0, "a", "yyyy", 2), (2, "b", "", 3) });

        var bm = fst.ToBimachine(new HashSet<char> { 'a', 'b' }).PseudoMinimal();

        Assert.Equal("x", bm.Process("a"));
        Assert.Equal("yyyy", bm.Process("ab"));
        Assert.Throws<ArgumentException>(() => bm.Process("aa"));
        Assert.Throws<ArgumentException>(() => bm.Process("abb"));
    }

    [Fact]
    public void ConstructionFromFstTest2()
    {
        // <a,a>* U (<a,''>* . <b,b>)
        var fst = FstBuilder.FromWordPair("a", "a")
            .Star()
            .Union(
                FstBuilder.FromWordPair("a", string.Empty)
                    .Star()
                    .Concat(FstBuilder.FromWordPair("b", "b")));

        var bm = fst.ToBimachine(new HashSet<char> { 'a', 'b' }).PseudoMinimal();

        Assert.Equal("b", bm.Process("aab"));
        Assert.Equal("aa", bm.Process("aa"));
        Assert.Equal("b", bm.Process("aaaaaab"));
    }
}