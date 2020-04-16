using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class PfsaTests
{
    [Fact]
    public void EpsilonPfsaBuilderTest()
    {
        var fsa = PfsaBuilder.FromEpsilon();

        Assert.Single(fsa.States);
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize(string.Empty));
    }

    [Fact]
    public void WordPfsaBuilderTest()
    {
        var fsa = PfsaBuilder.FromWord("abc");

        Assert.Equal(4, fsa.States.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abca"));
        Assert.True(fsa.Recognize("abc"));
    }

    [Fact]
    public void FromSymbolSetPfsaTest()
    {
        var fsa = PfsaBuilder.FromSymbolSet(new HashSet<char> { 'a', 'b', 'c' });

        Assert.Equal(2, fsa.States.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("d"));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "b", "a", "c" }.All(fsa.Recognize));
    }

    [Fact]
    public void ConcatPfsaTest()
    {
        var fsa1 = PfsaBuilder.FromWord("abc");
        var fsa2 = PfsaBuilder.FromWord("de");
        var fsa = fsa1.Concat(fsa2);

        Assert.Equal(7, fsa.States.Count);
        Assert.Single(fsa.Initial);
        Assert.Single(fsa.Final);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abc"));
        Assert.False(fsa.Recognize("de"));
        Assert.True(fsa.Recognize("abcde"));
    }

    [Fact]
    public void ConcatMultiplePfsaTest()
    {
        var fsa1 = FsaBuilder.FromWord("ab");
        var fsa2 = FsaBuilder.FromWord("cde");
        var fsa3 = FsaBuilder.FromWord("f").Star();
        var fsa = fsa1.Concat(fsa2, fsa3);

        Assert.True(fsa.Recognize("abcdef"));
        Assert.True(fsa.Recognize("abcdefffffff"));
        Assert.False(fsa.Recognize("abcdff"));
    }

    [Fact]
    public void UnionPfsaTest()
    {
        var fsa1 = PfsaBuilder.FromWord("abc");
        var fsa2 = PfsaBuilder.FromWord("de");
        var fsa = fsa1.Union(fsa2);

        Assert.Equal(7, fsa.States.Count);
        Assert.Equal(2, fsa.Initial.Count);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.True(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize("de"));
        Assert.False(fsa.Recognize("abcde"));
    }

    [Fact]
    public void UnionEpsilonPfsaTest()
    {
        var fsa1 = PfsaBuilder.FromWord("abc");
        var fsa2 = PfsaBuilder.FromEpsilon();
        var fsa = fsa1.Union(fsa2);

        Assert.Equal(5, fsa.States.Count);
        Assert.Equal(2, fsa.Initial.Count);
        Assert.Equal(2, fsa.Final.Count);
        Assert.True(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.True(fsa.Recognize("abc"));
        Assert.False(fsa.Recognize("abca"));
    }

    [Fact]
    public void StarPfsaTest()
    {
        var fsa = PfsaBuilder.FromWord("a").Star();

        Assert.Equal(3, fsa.States.Count);
        Assert.Single(fsa.Initial);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "aaaa", "a", "aa", string.Empty, "aaaaaaaa" }.All(fsa.Recognize));
    }

    [Fact]
    public void StarPfsaTest1()
    {
        var fsa = PfsaBuilder.FromWord("abc").Star();

        Assert.Equal(5, fsa.States.Count);
        Assert.Single(fsa.Initial);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize("abcabcabcb"));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(fsa.Recognize(string.Empty));
        Assert.True(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize("abcabcabc"));
    }

    [Fact]
    public void PlusPfsaTest()
    {
        var fsa = PfsaBuilder.FromWord("a").Plus();

        Assert.Equal(3, fsa.States.Count);
        Assert.Single(fsa.Initial);
        Assert.Equal(1, fsa.Final.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "a", "aaa", "aaaaaaaaa", "aa" }.All(fsa.Recognize));
    }

    [Fact]
    public void OptionPfsaTest()
    {
        var fsa = PfsaBuilder.FromWord("ab").Optional();

        Assert.Equal(4, fsa.States.Count);
        Assert.Equal(2, fsa.Initial.Count);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize("b"));
        Assert.False(fsa.Recognize("a"));
        Assert.True(new[] { "ab", string.Empty }.All(fsa.Recognize));
    }

    [Fact]
    public void AllPfsaTest()
    {
        var fsa = PfsaBuilder.All().Star();

        Assert.Equal(3, fsa.States.Count);
        Assert.Equal(1, fsa.Initial.Count);
        Assert.Equal(2, fsa.Final.Count);
        Assert.True(fsa.Recognize("d"));
        Assert.True(fsa.Recognize("ad"));
        Assert.True(new[] { "ab", string.Empty, "abc", "bbbac", "cba", "cbcbbcaaaaacb" }.All(fsa.Recognize));
    }

    [Fact]
    public void ComplexPfsaConstructionTest()
    {
        // ab*c
        var fsa =
            PfsaBuilder.FromWord("a").Concat(
                PfsaBuilder.FromWord("b").Star(),
                PfsaBuilder.FromWord("c"));

        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize("ac"));
        Assert.True(fsa.Recognize("abbbbc"));
    }

    [Fact]
    public void ComplexPfsaConstructionTest1()
    {
        // (a|b)*c
        var fsa = PfsaBuilder.FromWord("a")
            .Union(PfsaBuilder.FromWord("b"))
            .Star()
            .Concat(PfsaBuilder.FromWord("c"));

        Assert.DoesNotContain(new[] { "ca", "aaba", string.Empty, "cc" }, fsa.Recognize);
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "c" }.All(fsa.Recognize));
    }

    [Fact]
    public void ComplexPfsaConstructionTest2()
    {
        // .+@.+\.com
        var allPlus = PfsaBuilder.All().Plus();
        var fsa = allPlus
            .Concat(
                PfsaBuilder.FromWord("@"),
                allPlus,
                PfsaBuilder.FromWord(".com"))
            ; // .Determinize();

        Assert.DoesNotContain(new[] { "me@yahoo.co", "you_gmail.com", "info@aol.cc", "about@.mail.comm" }, fsa.Recognize);
        Assert.True(new[] { "me@yahoo.com", "you@gmail.com", "info@aol.com" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeSimpleConstructionTest()
    {
        // a*
        var fsa = PfsaBuilder.FromWord("a").Star().EpsilonFree();

        Assert.DoesNotContain(fsa.Transitions, t => t.Pred == default);
        Assert.DoesNotContain(new[] { "ca", "aaba", "b", "cc" }, fsa.Recognize);
        Assert.True(new[] { "aaaa", "a", "aa", string.Empty, "aaaaaaaa" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeConstructionTest()
    {
        // (a|b)+c
        var fsa = PfsaBuilder.FromWord("a")
            .Union(PfsaBuilder.FromWord("b"))
            .Plus()
            .Concat(PfsaBuilder.FromWord("c"))
            .EpsilonFree();

        Assert.DoesNotContain(fsa.Transitions, t => t.Pred == default);
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "aac" }.All(fsa.Recognize));
        Assert.DoesNotContain(new[] { "ca", "aaba", string.Empty, "cc", "c" }, fsa.Recognize);
    }
}