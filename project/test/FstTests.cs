using System;
using System.Collections.Generic;
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
    public void FstWithEpsilonProcessTest1()
    {
        var fst = new Fst(
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
            new[] { 0 },
            new[] { 5, 6 },
            new[]
            {
                (0, "", "", 1),
                (0, "", "", 6),
                (1, "a", "a", 2),
                (1, "b", "b", 2),
                (2, "", "", 0),
                (2, "", "", 6),
                (3, "a", "d", 7),
                (4, "", "", 5),
                (6, "", "", 3),
                (7, "b", "", 4),
            });

        Assert.Equal(new[] { "ab", "d" }, fst.Process("ab").OrderBy(w => w));
        Assert.Equal(new[] { "abab", "abd" }, fst.Process("abab").OrderBy(w => w));
    }

    [Fact]
    public void FstWithEpsilonProcessTest2()
    {
        var states = new[] { 0, 1, 2 };
        var initial = new[] { 0 };
        var final = new[] { 2 };
        var transitions = new[]
        {
            (0, "a", "x", 1),
            (1, string.Empty, "y", 2),
        };
        var fst = new Fst(states, initial, final, transitions);

        Assert.Equal("xy", fst.Process("a").Single());
    }

    [Fact]
    public void UnionFstTest()
    {
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = first.UnionWith(second);

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
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = first.UnionWith(second).Star();

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
        var fst = first.UnionWith(second).Plus();

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
        var fst = FstBuilder.FromWordPair("a", "A").Plus().Option();

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
        Assert.Single(fst.Transitions.Where(t => t == (0, "a", "y", 1)));
        Assert.Single(fst.Transitions.Where(t => t == (2, "c", "z", 1)));

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
        var first = FstBuilder.FromWordPair("a", "A");
        var second = FstBuilder.FromWordPair("b", "B");
        var fst = first.UnionWith(second).Star().EpsilonFree();

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
    public void ExpandSimpleFstTest()
    {
        var fst = FstBuilder.FromWordPair("abc", "xy").Expand();

        Assert.Equal(4, fst.States.Count);
        Assert.Equal(3, fst.Transitions.Count);
        Assert.True(fst.Transitions.All(t => t.In.Length <= 1 && t.Out.Length <= 1));
        Assert.Equal("xy", fst.Process("abc").Single());
    }

    [Fact]
    public void ExpandFstTest()
    {
        var fst = FstBuilder.FromWordPair("abc", "xy")
            .UnionWith(FstBuilder.FromWordPair("p", "q"))
            .Expand();

        Assert.Equal(6, fst.States.Count);
        Assert.Equal(4, fst.Transitions.Count);
        Assert.True(fst.Transitions.All(t => t.In.Length <= 1 && t.Out.Length <= 1));
        Assert.Equal("xy", fst.Process("abc").Single());
        Assert.Equal("q", fst.Process("p").Single());
    }

    [Fact]
    public void ExpandFstTest1()
    {
        var fst = FstBuilder.FromWordPair("abc", "xy")
            .UnionWith(FstBuilder.FromWordPair("pp", "qq"))
            .Expand();

        Assert.Equal(7, fst.States.Count);
        Assert.Equal(5, fst.Transitions.Count);
        Assert.True(fst.Transitions.All(t => t.In.Length <= 1 && t.Out.Length <= 1));
        Assert.Equal("xy", fst.Process("abc").Single());
        Assert.Equal("qq", fst.Process("pp").Single());
    }

    [Fact]
    public void ComposeSimpleFstTest()
    {
        var first = FstBuilder.FromWordPair("ab", "xy");
        var second = FstBuilder.FromWordPair("xy", "zz");
        var composed = first.Compose(second);

        Assert.Equal(3, composed.States.Count);
        Assert.Equal(1, composed.Initial.Count);
        Assert.Equal(1, composed.Final.Count);
        Assert.Equal(2, composed.Transitions.Count);

        Assert.Equal("zz", composed.Process("ab").Single());
    }

    [Fact]
    public void ComposeMultipleFstTest()
    {
        var first = FstBuilder.FromWordPair("ab", "x").Star();
        var second = FstBuilder.FromWordPair("x", "1").Star();
        var third = FstBuilder.FromWordPair("1", "2").Star();
        var composed = first.Compose(second, third);

        Assert.Equal(string.Empty, composed.Process(string.Empty).Single());
        Assert.Equal("2", composed.Process("ab").Single());
        Assert.Equal("2222", composed.Process("abababab").Single());
        Assert.Empty(composed.Process("aba"));
    }

    [Fact]
    public void ComposeFstTest()
    {
        var first = FstBuilder.FromWordPair("ab", "x")
            .Plus()
            .Concat(FstBuilder.FromWordPair("c", "d").Option());
        var second = FstBuilder.FromWordPair("x", "1")
            .UnionWith(FstBuilder.FromWordPair("d", "d"))
            .Plus();
        var composed = first.Compose(second);

        Assert.Equal("1d", composed.Process("abc").Single());
        Assert.Equal("1111", composed.Process("abababab").Single());
        Assert.Equal("1111d", composed.Process("ababababc").Single());
        Assert.Empty(composed.Process("aba"));
        Assert.Empty(composed.Process(string.Empty));
    }

    [Fact]
    public void ToRealTimeFstTest()
    {
        var fst = FstBuilder.FromWordPair(string.Empty, "u")
            .Concat(FstBuilder.FromWordPair("a", "v"))
            .Concat(FstBuilder.FromWordPair(string.Empty, "w"));
        var realTime = fst.ToRealTime();

        Assert.True(realTime.Transducer.Transitions.All(tr => !string.IsNullOrEmpty(tr.In)));
        Assert.Empty(realTime.EpsilonOutputs);
        Assert.Equal("uvw", realTime.Transducer.Process("a").Single());
    }

    [Fact]
    public void ToRealTimeFstTest1()
    {
        var fst = new Fst(
            new[] { 0, 1, 2, 3 },
            new[] { 0 },
            new[] { 3 },
            new[]
            {
                (0, string.Empty, "u", 1),
                (1, "a", "v", 2),
                (2, string.Empty, "w", 3),
                (0, string.Empty, "xyz", 3)
            });
        var realTime = fst.ToRealTime();

        Assert.True(realTime.Transducer.Transitions.All(tr => !string.IsNullOrEmpty(tr.In)));
        Assert.Equal("xyz", realTime.EpsilonOutputs.Single());
        Assert.Equal("uvw", realTime.Transducer.Process("a").Single());
    }

    [Fact]
    public void InfAmbiguousFstToRealTimeTest()
    {
        var fst = new Fst(
            new[] { 0, 1, 2 },
            new[] { 0 },
            new[] { 2 },
            new[] { (0, "a", "x", 1), (1, string.Empty, "y", 2), (2, string.Empty, "y", 1) });

        Assert.Throws<ArgumentException>(() => fst.ToRealTime());
    }
}