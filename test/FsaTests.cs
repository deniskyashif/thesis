using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class FsaTests
{
    [Fact]
    public void EpsilonFsaBuilderTest()
    {
        var fsa = FsaBuilder.FromEpsilon();

        Assert.Single(fsa.States);
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize(string.Empty));
    }

    [Fact]
    public void WordFsaBuilderTest()
    {
        var fsa = FsaBuilder.FromWord("abc");

        Assert.Equal(4, fsa.States.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abca"));
        Assert.True(fsa.Recognize("abc"));
    }

    [Fact]
    public void FromSymbolSetFsaTest()
    {
        var fsa = FsaBuilder.FromSymbolSet(new HashSet<string> { "a", "b", "c" });

        Assert.Equal(2, fsa.States.Count);
        Assert.False(fsa.Recognize(""));
        Assert.False(fsa.Recognize("d"));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "b", "a", "c" }.All(fsa.Recognize));
    }

    [Fact]
    public void ConcatFsaTest()
    {
        var fsa1 = FsaBuilder.FromWord("abc");
        var fsa2 = FsaBuilder.FromWord("de");
        var fsa = FsaBuilder.Concat(fsa1, fsa2);

        Assert.Equal(7, fsa.States.Count);
        Assert.Single(fsa.InitialStates);
        Assert.Single(fsa.FinalStates);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abc"));
        Assert.False(fsa.Recognize("de"));
        Assert.True(fsa.Recognize("abcde"));
    }

    [Fact]
    public void UnionFsaTest()
    {
        var fsa1 = FsaBuilder.FromWord("abc");
        var fsa2 = FsaBuilder.FromWord("de");
        var fsa = FsaBuilder.Union(fsa1, fsa2);

        Assert.Equal(7, fsa.States.Count);
        Assert.Equal(2, fsa.InitialStates.Count);
        Assert.Equal(2, fsa.FinalStates.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.True(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize("de"));
        Assert.False(fsa.Recognize("abcde"));
    }

    [Fact]
    public void UnionEpsilonFsaTest()
    {
        var fsa1 = FsaBuilder.FromWord("abc");
        var fsa2 = FsaBuilder.FromEpsilon();
        var fsa = FsaBuilder.Union(fsa1, fsa2);

        Assert.Equal(5, fsa.States.Count);
        Assert.Equal(2, fsa.InitialStates.Count);
        Assert.Equal(2, fsa.FinalStates.Count);
        Assert.True(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.True(fsa.Recognize("abc"));
        Assert.False(fsa.Recognize("abca"));
    }

    [Fact]
    public void StarFsaTest()
    {
        var fsa = FsaBuilder.Star(FsaBuilder.FromWord("a"));

        Assert.Equal(3, fsa.States.Count);
        Assert.Single(fsa.InitialStates);
        Assert.Equal(2, fsa.FinalStates.Count);
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "aaaa", "a", "aa", "", "aaaaaaaa" }.All(fsa.Recognize));
    }

    [Fact]
    public void StarFsaTest1()
    {
        var fsa = FsaBuilder.Star(FsaBuilder.FromWord("abc"));

        Assert.Equal(5, fsa.States.Count);
        Assert.Single(fsa.InitialStates);
        Assert.Equal(2, fsa.FinalStates.Count);
        Assert.False(fsa.Recognize("abcabcabcb"));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(fsa.Recognize(string.Empty));
        Assert.True(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize("abcabcabc"));
    }

    [Fact]
    public void PlusFsaTest()
    {
        var fsa = FsaBuilder.Plus(FsaBuilder.FromWord("a"));

        Assert.Equal(3, fsa.States.Count);
        Assert.Single(fsa.InitialStates);
        Assert.Equal(1, fsa.FinalStates.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "a", "aaa", "aaaaaaaaa", "aa" }.All(fsa.Recognize));
    }

    [Fact]
    public void ComplexFsaConstructionTest()
    {
        // ab*c
        var fsa =
            FsaBuilder.Concat(
                FsaBuilder.Concat(
                    FsaBuilder.FromWord("a"),
                    FsaBuilder.Star(FsaBuilder.FromWord("b"))),
                FsaBuilder.FromWord("c"));

        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize("ac"));
        Assert.True(fsa.Recognize("abbbbc"));
    }

    [Fact]
    public void ComplexFsaConstructionTest1()
    {
        // (a|b)*c
        var fsa =
            FsaBuilder.Concat(
                FsaBuilder.Star(
                    FsaBuilder.Union(
                        FsaBuilder.FromWord("a"),
                        FsaBuilder.FromWord("b"))),
                FsaBuilder.FromWord("c"));

        Assert.DoesNotContain(new[] { "ca", "aaba", "", "cc" }, fsa.Recognize);
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "c" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeConstructionTest()
    {
        // a*
        var fsa =
            FsaBuilder.EpsilonFree(
                FsaBuilder.Star(FsaBuilder.FromWord("a")));

        Assert.DoesNotContain(fsa.Transitions, t => string.IsNullOrEmpty(t.Via));
        Assert.DoesNotContain(new[] { "ca", "aaba", "b", "cc" }, fsa.Recognize);
        Assert.True(new[] { "aaaa", "a", "aa", "", "aaaaaaaa" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeConstructionTest1()
    {
        // (a|b)+c
        var fsa =
            FsaBuilder.EpsilonFree(
                FsaBuilder.Concat(
                    FsaBuilder.Plus(
                        FsaBuilder.Union(
                            FsaBuilder.FromWord("a"),
                            FsaBuilder.FromWord("b"))),
                    FsaBuilder.FromWord("c")));

        Assert.DoesNotContain(fsa.Transitions, t => string.IsNullOrEmpty(t.Via));
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "aac" }.All(fsa.Recognize));
        Assert.DoesNotContain(new[] { "ca", "aaba", "", "cc", "c" }, fsa.Recognize);
    }
}