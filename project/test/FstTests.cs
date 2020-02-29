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
        var first = FstExtensions.FromWordPair("a", "A");
        var second = FstExtensions.FromWordPair("b", "B");
        var fst = first.Union(second);

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
        var first = FstExtensions.FromWordPair("a", "A");
        var second = FstExtensions.FromWordPair("b", "B");
        var fst = first.Concat(second);

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
        var first = FstExtensions.FromWordPair("a", "A");
        var second = FstExtensions.FromWordPair("b", "B");
        var fst = first.Union(second).Star();

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
        var first = FstExtensions.FromWordPair("a", "A");
        var second = FstExtensions.FromWordPair("b", "B");
        var fst = first.Union(second).Plus();

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
        var fst = FstExtensions.FromWordPair("a", "A").Plus().Option();

        Assert.Equal(4, fst.States.Count);
        Assert.Equal(2, fst.Initial.Count);
        Assert.Equal(2, fst.Final.Count);
        Assert.Equal(3, fst.Transitions.Count);
        Assert.Equal("A", fst.Process("a").Single());
        Assert.Equal("AAA", fst.Process("aaa").Single());
        Assert.Equal("AAAAA", fst.Process("aaaaa").Single());
        Assert.Equal(string.Empty, fst.Process(string.Empty).Single());
    }

    [Fact]
    public void EpsilonFreeConversionTest()
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
            (2, "", "", 1),
        };
        var fst = new Fst(states, initial, final, transitions).EpsilonFree();

        Assert.Equal(6, fst.Transitions.Count);
        Assert.NotNull(fst.Transitions.Single(t => t == (0, "a", "y", 1)));
        Assert.NotNull(fst.Transitions.Single(t => t == (2, "c", "z", 1)));

        var res1 = fst.Process("ab");
        Assert.Equal(2, res1.Count);
        Assert.Contains("xy", res1);
        Assert.Contains("yy", res1);

        Assert.Equal("y", fst.Process("a").Single());
        Assert.Equal("yzzzz", fst.Process("acccc").Single());
        Assert.Equal("yzzzy", fst.Process("acccb").Single());
    }

    [Fact]
    public void EpsilonFreeConversionTest1()
    {
        var first = FstExtensions.FromWordPair("a", "A");
        var second = FstExtensions.FromWordPair("b", "B");
        var fst = first.Union(second).Star().EpsilonFree();

        Assert.Equal(5, fst.States.Count);
        Assert.Equal(3, fst.Initial.Count);
        Assert.Equal(3, fst.Final.Count);
        Assert.Equal(8, fst.Transitions.Count);
        Assert.Equal(string.Empty, fst.Process(string.Empty).Single());
        Assert.Equal("AB", fst.Process("ab").Single());
        Assert.Equal("AAA", fst.Process("aaa").Single());
        Assert.Equal("AABAB", fst.Process("aabab").Single());
        Assert.Equal("ABBBAAAA", fst.Process("abbbaaaa").Single());
        Assert.Empty(fst.Process("c"));
        Assert.Empty(fst.Process("abc"));
    }

    [Fact]
    public void TrimFstTest()
    {
        var states = new[] { 0, 1, 2, 3, 4 };
        var initial = new[] { 0 };
        var final = new[] { 1, 3 };
        var transitions = new[]
        {
            (0, "a", "A", 1),
            (1, "b", "B", 3),
            (1, "c", "C", 4),
        };
        var fst = new Fst(states, initial, final, transitions).Trim();
        
        Assert.Equal(3, fst.States.Count);
        Assert.Equal(1, fst.Initial.Count);
        Assert.Equal(2, fst.Final.Count);
        Assert.Equal(2, fst.Transitions.Count);

        Assert.Equal("A", fst.Process("a").Single());
        Assert.Equal("AB", fst.Process("ab").Single());
        Assert.Empty(fst.Process("abc"));
        Assert.Empty(fst.Process("b"));
    }

    [Fact]
    public void ProductOfFsasToFstTest()
    {
        var fst = FstExtensions.Product(
            FsaExtensions.FromWord("a"), 
            FsaExtensions.FromWord("b"));

        Assert.Equal(4, fst.States.Count);
        Assert.Empty(fst.Transitions.Where(t => string.IsNullOrEmpty(t.In) && string.IsNullOrEmpty(t.Out)));

        Assert.Equal("b", fst.Process("a").Single());
        Assert.Empty(fst.Process(string.Empty));
        Assert.Empty(fst.Process("b"));
    }
}