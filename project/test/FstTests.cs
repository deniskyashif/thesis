using System.Linq;
using Xunit;

public class FstTests
{
    [Fact]
    public void FstProcessTest()
    {
        var states = new[] { 0, 1, 2, 3 };
        var initial = new[] { 0 };
        var final = new[] { 2, 3 };
        var transitions = new[]
        {
            (0, "a", "x", 1),
            (0, "a", "y", 2),
            (1, "b", "y", 3),
            (2, "c", "z", 2),
        };
        var fst = new Fst(states, initial, final, transitions);

        Assert.Equal("xy", fst.Process("ab").Single());
        Assert.Equal("y", fst.Process("a").Single());
        Assert.Equal("yzzzz", fst.Process("acccc").Single());
    }

    [Fact]
    public void FstWithEpsilonProcessTest()
    {
        var states = new[] { 0, 1, 2, 3 };
        var initial = new[] { 0 };
        var final = new[] { 2, 3 };
        var transitions = new[]
        {
            (0, "a", "x", 1),
            (0, "a", "y", 2),
            (1, "b", "y", 3),
            (2, "c", "z", 2),
            (2, "", "E", 1),
        };
        var fst = new Fst(states, initial, final, transitions);

        var res1 = fst.Process("ab");
        Assert.Equal(2, res1.Count);
        Assert.Contains("xy", res1);
        Assert.Contains("yEy", res1);

        Assert.Equal("y", fst.Process("a").Single());
        Assert.Equal("yzzzz", fst.Process("acccc").Single());
        Assert.Equal("yzzzEy", fst.Process("acccb").Single());
    }

    [Fact]
    public void UnionFstTest()
    {
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = FstBuilder.Union(first, second);

        Assert.Equal(4, fst.States.Count);
        Assert.Equal(2, fst.Initial.Count);
        Assert.Equal(2, fst.Final.Count);
        Assert.Equal(2, fst.Transitions.Count);
        Assert.Equal("A", fst.Process("a").Single());
        Assert.Equal("B", fst.Process("b").Single());
        Assert.Empty(fst.Process("ab"));
    }

    [Fact]
    public void ConcatFstTest()
    {
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = FstBuilder.Concat(first, second);

        Assert.Equal(4, fst.States.Count);
        Assert.Equal(1, fst.Initial.Count);
        Assert.Equal(1, fst.Final.Count);
        Assert.Equal(3, fst.Transitions.Count);
        Assert.Equal("AB", fst.Process("ab").Single());
        Assert.Empty(fst.Process("a"));
        Assert.Empty(fst.Process("b"));
    }

    [Fact]
    public void StarFstTest()
    {
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = FstBuilder.Star(FstBuilder.Union(first, second));

        Assert.Equal(5, fst.States.Count);
        Assert.Equal(1, fst.Initial.Count);
        Assert.Equal(3, fst.Final.Count);
        Assert.Equal(6, fst.Transitions.Count);
        Assert.Equal(string.Empty, fst.Process(string.Empty).Single());
        Assert.Equal("AB", fst.Process("ab").Single());
        Assert.Equal("AAA", fst.Process("aaa").Single());
        Assert.Equal("AABAB", fst.Process("aabab").Single());
        Assert.Equal("ABBBAAAA", fst.Process("abbbaaaa").Single());
        Assert.Empty(fst.Process("c"));
        Assert.Empty(fst.Process("abc"));
    }

    [Fact]
    public void PlusFstTest()
    {
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = FstBuilder.Plus(FstBuilder.Union(first, second));

        Assert.Equal(5, fst.States.Count);
        Assert.Equal(1, fst.Initial.Count);
        Assert.Equal(2, fst.Final.Count);
        Assert.Equal(6, fst.Transitions.Count);
        Assert.Equal("AB", fst.Process("ab").Single());
        Assert.Equal("AAA", fst.Process("aaa").Single());
        Assert.Equal("AABAB", fst.Process("aabab").Single());
        Assert.Equal("ABBBAAAA", fst.Process("abbbaaaa").Single());
        Assert.Empty(fst.Process("c"));
        Assert.Empty(fst.Process("abc"));
        Assert.Empty(fst.Process(string.Empty));
    }

    [Fact]
    public void OptionFstTest()
    {
        var fst = FstBuilder.Option(
            FstBuilder.Plus(FstBuilder.FromWordPair("a", "A")));

        Assert.Equal(4, fst.States.Count);
        Assert.Equal(2, fst.Initial.Count);
        Assert.Equal(2, fst.Final.Count);
        Assert.Equal(3, fst.Transitions.Count);
        Assert.Equal("A", fst.Process("a").Single());
        Assert.Equal("AAA", fst.Process("aaa").Single());
        Assert.Equal("AAAAA", fst.Process("aaaaa").Single());
        Assert.Equal(string.Empty, fst.Process(string.Empty).Single());
    }
}