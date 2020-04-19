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
        var fsa = PfsaBuilder.Any().Star();

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
        var allPlus = PfsaBuilder.Any().Plus();
        var fsa = allPlus
            .Concat(
                PfsaBuilder.FromWord("@"),
                allPlus,
                PfsaBuilder.FromWord(".com"))
            .Determinize();

        Assert.DoesNotContain(new[] { "me@yahoo.co", "you_gmail.com", "info@aol.cc", "about@.mail.comm" }, fsa.Recognize);
        Assert.True(new[] { "me@yahoo.com", "you@gmail.com", "info@aol.com" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeSimpleConstructionTest()
    {
        // a*
        var fsa = PfsaBuilder.FromWord("a").Star().EpsilonFree();

        Assert.DoesNotContain(fsa.Transitions, t => t.Label == null);
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

        Assert.DoesNotContain(fsa.Transitions, t => t.Label == null);
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "aac" }.All(fsa.Recognize));
        Assert.DoesNotContain(new[] { "ca", "aaba", string.Empty, "cc", "c" }, fsa.Recognize);
    }

    [Fact]
    public void TrimPfsaTest()
    {
        var states = new[] { 1, 2, 3, 4 };
        var initial = new[] { 2, 3 };
        var final = new[] { 4 };
        var transitions = new (int, Range, int)[] { (3, new Range('a'), 4), (2, new Range('b'), 1) };
        var fsa = new Pfsa(states, initial, final, transitions).Trim();

        Assert.Equal(2, fsa.States.Count);
        Assert.Single(fsa.Transitions);
        Assert.Single(fsa.Initial);
        Assert.Single(fsa.Final);
        Assert.True(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("ab"));
    }

    [Fact]
    public void IntersectPfsaTest()
    {
        // even number of "a"'s & any number of "b"'s
        var first = new Pfsa(
            new[] { 0, 1 },
            new[] { 0 },
            new[] { 0 },
            new List<(int, Range, int)>
            {
                (0, new Range('b'), 0),
                (0, new Range('a'), 1 ),
                (1, new Range('b'), 1 ),
                (1, new Range('a'), 0),
            });
        // even number of "b"'s & any number of "a"'s
        var second = new Pfsa(
            new[] { 2, 3 },
            new[] { 2 },
            new[] { 2 },
            new List<(int, Range, int)>
            {
                (2, new Range('a'), 2 ),
                (2, new Range('b'), 3 ),
                (3, new Range('a'), 3 ),
                (3, new Range('b'), 2 ),
            });

        Assert.True(new[] { string.Empty, "aaaa", "aab", "baabaa", "baba", "abab", "bbbaa" }.All(first.Recognize));
        Assert.True(new[] { string.Empty, "aa", "bb", "abab", "aabb", "baba", "bbaa", "bbaaaaaaabb" }.All(second.Recognize));

        // even number of "a"'s and "b"'s
        var intersected = first.Intersect(second);
        Assert.True(new[] { "abab", "aaaabbbb", "bbabba", "bbaa", "aabbaa", "aabb" }.All(intersected.Recognize));
        Assert.DoesNotContain(new[] { "aaa", "aaabb", "aaabaabb", "aaabb", "baaba", "bbbaa" }, intersected.Recognize);
    }

    [Fact]
    public void ComplementPfsaTest()
    {
        var pfsa = new RegExp2("a+b").Automaton.Complement();

        var notInL = new[] { "ab", "aab", "aaaab", "aaaaaaaaab" };
        var inL = new[] { "b", "aabb", "aa", "aaaaaaaaaba", string.Empty, "ov", "123v" };

        Assert.True(!notInL.All(pfsa.Recognize));
        Assert.True(inL.All(pfsa.Recognize));
    }

    [Fact]
    public void ComplementPfsaTest1()
    {
        var pfsa = new RegExp2("[0-9]*").Automaton.Complement();

        var notInL = new[] { "123", string.Empty, "4", "435900491" };
        var inL = new[] { "123213_", "b", "aabb", "aa", "1ba", "ov", "123v" };

        Assert.True(!notInL.All(pfsa.Recognize));
        Assert.True(inL.All(pfsa.Recognize));
    }
}