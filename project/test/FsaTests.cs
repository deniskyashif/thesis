using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class FsaTests
{
    [Fact]
    public void EpsilonFsaBuilderTest()
    {
        var fsa = FsaExtensions.FromEpsilon();

        Assert.Single(fsa.States);
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abc"));
        Assert.True(fsa.Recognize(string.Empty));
    }

    [Fact]
    public void WordFsaBuilderTest()
    {
        var fsa = FsaExtensions.FromWord("abc");

        Assert.Equal(4, fsa.States.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("abca"));
        Assert.True(fsa.Recognize("abc"));
    }

    [Fact]
    public void FromSymbolSetFsaTest()
    {
        var fsa = FsaExtensions.FromSymbolSet(new HashSet<char> { 'a', 'b', 'c' });

        Assert.Equal(2, fsa.States.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("d"));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "b", "a", "c" }.All(fsa.Recognize));
    }

    [Fact]
    public void ConcatFsaTest()
    {
        var fsa1 = FsaExtensions.FromWord("abc");
        var fsa2 = FsaExtensions.FromWord("de");
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
    public void ConcatMultipleFsaTest()
    {
        var fsa1 = FsaExtensions.FromWord("ab");
        var fsa2 = FsaExtensions.FromWord("cde");
        var fsa3 = FsaExtensions.FromWord("f").Star();
        var fsa = fsa1.Concat(fsa2, fsa3);

        Assert.True(fsa.Recognize("abcdef"));
        Assert.True(fsa.Recognize("abcdefffffff"));
        Assert.False(fsa.Recognize("abcdff"));
    }

    [Fact]
    public void UnionFsaTest()
    {
        var fsa1 = FsaExtensions.FromWord("abc");
        var fsa2 = FsaExtensions.FromWord("de");
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
    public void UnionEpsilonFsaTest()
    {
        var fsa1 = FsaExtensions.FromWord("abc");
        var fsa2 = FsaExtensions.FromEpsilon();
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
    public void StarFsaTest()
    {
        var fsa = FsaExtensions.FromWord("a").Star();

        Assert.Equal(3, fsa.States.Count);
        Assert.Single(fsa.Initial);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "aaaa", "a", "aa", string.Empty, "aaaaaaaa" }.All(fsa.Recognize));
    }

    [Fact]
    public void StarFsaTest1()
    {
        var fsa = FsaExtensions.FromWord("abc").Star();

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
    public void PlusFsaTest()
    {
        var fsa = FsaExtensions.FromWord("a").Plus();

        Assert.Equal(3, fsa.States.Count);
        Assert.Single(fsa.Initial);
        Assert.Equal(1, fsa.Final.Count);
        Assert.False(fsa.Recognize(string.Empty));
        Assert.False(fsa.Recognize("ab"));
        Assert.True(new[] { "a", "aaa", "aaaaaaaaa", "aa" }.All(fsa.Recognize));
    }

    [Fact]
    public void OptionFsaTest()
    {
        var fsa = FsaExtensions.FromWord("ab").Option();

        Assert.Equal(4, fsa.States.Count);
        Assert.Equal(2, fsa.Initial.Count);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize("b"));
        Assert.False(fsa.Recognize("a"));
        Assert.True(new[] { "ab", string.Empty }.All(fsa.Recognize));
    }

    [Fact]
    public void AllFsaTest()
    {
        var fsa = FsaExtensions.All(new HashSet<char> { 'a', 'b', 'c' });

        Assert.Equal(3, fsa.States.Count);
        Assert.Equal(1, fsa.Initial.Count);
        Assert.Equal(2, fsa.Final.Count);
        Assert.False(fsa.Recognize("d"));
        Assert.False(fsa.Recognize("ad"));
        Assert.True(new[] { "ab", string.Empty, "abc", "bbbac", "cba", "cbcbbcaaaaacb" }.All(fsa.Recognize));
    }

    [Fact]
    public void ComplexFsaConstructionTest()
    {
        // ab*c
        var fsa =
            FsaExtensions.FromWord("a").Concat(
                FsaExtensions.FromWord("b").Star(),
                FsaExtensions.FromWord("c"));

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
        var fsa = FsaExtensions.FromWord("a")
            .Union(FsaExtensions.FromWord("b"))
            .Star()
            .Concat(FsaExtensions.FromWord("c"));

        Assert.DoesNotContain(new[] { "ca", "aaba", string.Empty, "cc" }, fsa.Recognize);
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "c" }.All(fsa.Recognize));
    }

    [Fact]
    public void ComplexFsaConstructionTest2()
    {
        // .*@.*\.com
        var all = FsaExtensions.All(
            Enumerable.Range(97, 27).Select(Convert.ToChar).ToHashSet());
        var fsa = all
            .Concat(
                FsaExtensions.FromWord("@"),
                all,
                FsaExtensions.FromWord(".com"))
            .Determinize();

        Assert.DoesNotContain(new[] { "me@yahoo.co", "you@@gmail.com", "info@aol.cc", "about@.mail.comm" }, fsa.Recognize);
        Assert.True(new[] { "me@yahoo.com", "you@gmail.com", "info@aol.com" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeSimpleConstructionTest()
    {
        // a*
        var fsa = FsaExtensions.FromWord("a").Star().EpsilonFree();

        Assert.DoesNotContain(fsa.Transitions, t => string.IsNullOrEmpty(t.Via));
        Assert.DoesNotContain(new[] { "ca", "aaba", "b", "cc" }, fsa.Recognize);
        Assert.True(new[] { "aaaa", "a", "aa", string.Empty, "aaaaaaaa" }.All(fsa.Recognize));
    }

    [Fact]
    public void EpsilonFreeConstructionTest()
    {
        // (a|b)+c
        var fsa = FsaExtensions.FromWord("a")
            .Union(FsaExtensions.FromWord("b"))
            .Plus()
            .Concat(FsaExtensions.FromWord("c"))
            .EpsilonFree();

        Assert.DoesNotContain(fsa.Transitions, t => string.IsNullOrEmpty(t.Via));
        Assert.True(new[] { "abbac", "ac", "bc", "ababbbbac", "aac" }.All(fsa.Recognize));
        Assert.DoesNotContain(new[] { "ca", "aaba", string.Empty, "cc", "c" }, fsa.Recognize);
    }

    [Fact]
    public void TrimFsaTest()
    {
        var states = new[] { 1, 2, 3, 4 };
        var initial = new[] { 2, 3 };
        var final = new[] { 4 };
        var transitions = new[] { (3, "a", 4), (2, "b", 1) };
        var fsa = new Fsa(states, initial, final, transitions).Trim();

        Assert.Equal(2, fsa.States.Count);
        Assert.Single(fsa.Transitions);
        Assert.Single(fsa.Initial);
        Assert.Single(fsa.Final);
        Assert.True(fsa.Recognize("a"));
        Assert.False(fsa.Recognize("ab"));
    }

    [Fact]
    public void DfaRecognizeTest()
    {
        // a|bc+
        var states = new[] { 0, 1, 2, 3 };
        var initial = 0;
        var final = new[] { 1, 3 };
        var transitions = new Dictionary<(int, char), int>()
        {
            { (0, 'a'), 1 },
            { (0, 'b'), 2 },
            { (2, 'c'), 3 },
            { (3, 'c'), 3 }
        };
        var dfsa = new Dfsa(states, initial, final, transitions);

        Assert.True(new[] { "a", "bc", "bcccc" }.All(dfsa.Recognize));
        Assert.DoesNotContain(new[] { "aa", "ab", "abc", "b", "abcc", "c" }, dfsa.Recognize);
    }

    [Fact]
    public void DfaRecognitionPathTest()
    {
        // a|bc+
        var states = new[] { 0, 1, 2, 3 };
        var initial = 0;
        var final = new[] { 1, 3 };
        var transitions = new Dictionary<(int, char), int>()
        {
            { (0, 'a'), 1 },
            { (0, 'b'), 2 },
            { (2, 'c'), 3 },
            { (3, 'c'), 3 }
        };
        var dfsa = new Dfsa(states, initial, final, transitions);

        Assert.Equal(new[] { 0, 1 }, dfsa.RecognitionPath("a").Path);
        Assert.Equal(new[] { 0, 2, 3, 3 }, dfsa.RecognitionPath("bcc").Path);
        Assert.Equal(new[] { 0, 2, 3, 3, 3, 3 }, dfsa.RecognitionPath("bcccc").Path);
    }

    [Fact]
    public void TrimDfsaTest()
    {
        // a|b+
        var states = new[] { 0, 1, 2, 3 };
        var initial = 0;
        var final = new[] { 1, 2 };
        var transitions = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (0, 'b'), 2 },
            { (2, 'b'), 2 },
        };
        var dfsa = new Dfsa(states, initial, final, transitions).Trim();

        Assert.Equal(3, dfsa.States.Count);
        Assert.Equal(3, dfsa.Transitions.Count);
        Assert.True(dfsa.Recognize("a"));
        Assert.True(dfsa.Recognize("b"));
        Assert.True(dfsa.Recognize("bbbbb"));
        Assert.False(dfsa.Recognize("ab"));
    }

    [Fact]
    public void ExpandFsaTest()
    {
        var fsa = new Fsa(
            new[] { 0, 1 },
            new[] { 0 },
            new[] { 1 },
            new[] { (0, "abcd", 1) }
        ).Expand();

        Assert.Equal(5, fsa.States.Count);
        Assert.Equal(4, fsa.Transitions.Count);
        Assert.Single(fsa.Initial);
        Assert.Single(fsa.Final);
        Assert.True(fsa.Recognize("abcd"));
        Assert.False(fsa.Recognize("abc"));
        Assert.False(fsa.Recognize("ab"));
    }

    [Fact]
    public void DetermFsaTest()
    {
        // (a|b)*a(a|b)
        var states = new[] { 0, 1, 2 };
        var initial = new[] { 0 };
        var final = new[] { 2 };
        var transitions = new (int, string, int)[]
        {
            (0, "a", 0),
            (1, "a", 2),
            (1, "b", 2),
            (0, "a", 1),
            (0, "b", 0),
        };
        var fsa = new Fsa(states, initial, final, transitions);
        var dfsa = fsa.Determinize();

        Assert.Equal(4, dfsa.States.Count);
        Assert.True(new[] { 0, 1, 2, 3 }.All(dfsa.States.Contains));
        Assert.True(new[] { "aa", "aab", "bbabab", "bab" }.All(dfsa.Recognize));
        Assert.DoesNotContain(new[] { string.Empty, "a", "caa", "bb", "ba" }, dfsa.Recognize);
    }

    [Fact]
    public void DetermEpsilonFsaTest()
    {
        // (a|abc)*
        var states = new[] { 0, 1, 2, 3 };
        var initial = new[] { 0 };
        var final = new[] { 1, 3 };
        var transitions = new (int, string, int)[]
        {
            (0, "a", 1),
            (2, "bc", 3),
            (0, "a", 2),
        };
        var fsa = new Fsa(states, initial, final, transitions).Star();
        var dfsa = fsa.Determinize();

        Assert.Equal(4, dfsa.States.Count);
        Assert.True(new[] { 0, 1, 2, 3 }.All(dfsa.States.Contains));
        Assert.True(new[] { string.Empty, "a", "aabc", "aaabcaaabc", "aaaaa", "abcabcabc" }.All(dfsa.Recognize));
        Assert.DoesNotContain(new[] { "ab", "ac", "bca", "aab", "baaca" }, dfsa.Recognize);
    }

    [Fact]
    public void DetermEpsilonFsaTest1()
    {
        // (a|abc)*
        var states = new[] { 0, 1 };
        var initial = new[] { 0 };
        var final = new[] { 1 };
        var transitions = new (int, string, int)[]
        {
            (0, "a", 0),
            (0, "b", 0),
            (0, "aaa", 1),
            (1, "a", 1),
            (1, "b", 1),
        };
        var fsa = new Fsa(states, initial, final, transitions);
        var dfsa = fsa.Determinize();

        Assert.True(new[] { "aaa", "aaaab", "aaabb", "aaabaaa", "bbaaabababb", "baaabaaabaaabb" }.All(dfsa.Recognize));
        Assert.DoesNotContain(new[] { "aab", "abaab", string.Empty, "baaac", "ababaab" }, dfsa.Recognize);
    }

    [Fact]
    public void ProductDfsaTest()
    {
        // even number of "b"'s & any number of "a"'s
        var first = new Dfsa(
            new[] { 0, 1 },
            0,
            new[] { 0 },
            new Dictionary<(int, char), int>
            {
                {(0, 'b'), 0 },
                {(0, 'a'), 1 },
                {(1, 'b'), 1 },
                {(1, 'a'), 0 },
            });
        // even number of "a"'s & any number of "b"'s
        var second = new Dfsa(
            new[] { 2, 3 },
            2,
            new[] { 2 },
            new Dictionary<(int, char), int>
            {
                    {(2, 'a'), 2 },
                    {(2, 'b'), 3 },
                    {(3, 'a'), 3 },
                    {(3, 'b'), 2 },
            });

        var (states, transitions) = FsaExtensions.Product(
            (first.Initial, first.Transitions),
            (second.Initial, second.Transitions));

        Assert.Equal(4, states.Count);
        Assert.True(new[] { (0, 2), (0, 3), (1, 2), (1, 3) }.All(states.Contains));
        Assert.Equal(8, transitions.Count);

        var stateIndices = Enumerable.Range(0, states.Count);
        Assert.True(stateIndices.All(s => transitions[(s, 'a')] != transitions[(s, 'b')]));
    }

    [Fact]
    public void IntersectDfsaTest()
    {
        // even number of "a"'s & any number of "b"'s
        var first = new Dfsa(
            new[] { 0, 1 },
            0,
            new[] { 0 },
            new Dictionary<(int, char), int>
            {
                {(0, 'b'), 0 },
                {(0, 'a'), 1 },
                {(1, 'b'), 1 },
                {(1, 'a'), 0 },
            });
        // even number of "b"'s & any number of "a"'s
        var second = new Dfsa(
            new[] { 2, 3 },
            2,
            new[] { 2 },
            new Dictionary<(int, char), int>
            {
                    {(2, 'a'), 2 },
                    {(2, 'b'), 3 },
                    {(3, 'a'), 3 },
                    {(3, 'b'), 2 },
            });

        Assert.True(new[] { string.Empty, "aaaa", "aab", "baabaa", "baba", "abab", "bbbaa" }.All(first.Recognize));
        Assert.True(new[] { string.Empty, "aa", "bb", "abab", "aabb", "baba", "bbaa", "bbaaaaaaabb" }.All(second.Recognize));

        // even number of "a"'s and "b"'s
        var dfsa = first.Intersect(second);
        Assert.True(new[] { "abab", "aaaabbbb", "bbabba", "bbaa", "aabbaa", "aabb" }.All(dfsa.Recognize));
        Assert.DoesNotContain(new[] { "aaa", "aaabb", "aaabaabb", "aaabb", "baaba", "bbbaa" }, dfsa.Recognize);
    }

    [Fact]
    public void DifferenceOfFsaTest()
    {
        var fsa = FsaExtensions.FromSymbolSet(new HashSet<char> { 'a', 'b', 'c' });
        var first = FsaExtensions.Star(fsa);
        var second = FsaExtensions.Plus(fsa);
        var words = new[] { "abc", "a", "aa", "abc", "c", "ccba" };

        Assert.True(words.All(first.Recognize));
        Assert.True(words.All(second.Recognize));
        Assert.True(first.Recognize(string.Empty));
        Assert.False(second.Recognize(string.Empty));

        var diff = first.Difference(second);

        Assert.True(diff.Recognize(string.Empty));
        Assert.DoesNotContain(words, diff.Recognize);
    }

    [Fact]
    public void DifferenceOfFsaTest1()
    {
        // a*b
        var first =
            FsaExtensions.Concat(
                FsaExtensions.Star(
                    FsaExtensions.FromWord("a")),
                FsaExtensions.FromWord("b"));
        // ab|b
        var second = FsaExtensions.FromWord("b");

        Assert.True(new[] { "ab", "b", "aaaaab", "aab" }.All(first.Recognize));
        Assert.True(new[] { "b" }.All(second.Recognize));
        Assert.False(second.Recognize("ab"));

        var diff = first.Difference(second);

        Assert.True(new[] { "ab", "aaaaab", "aab" }.All(diff.Recognize));
        Assert.False(diff.Recognize("b"));
    }

    [Fact]
    public void DifferenceOfFsaTest2()
    {
        var universal = FsaExtensions.Star(
            FsaExtensions.FromSymbolSet(new HashSet<char> { 'a', 'b', 'c' }));

        // ab+c
        var fsa = FsaExtensions.FromWord("a")
            .Concat(FsaExtensions.FromWord("b").Plus())
            .Concat(FsaExtensions.FromWord("c"));

        // not in ab+c
        var diff = universal.Difference(fsa);

        Assert.True(new[] { string.Empty, "ab", "ac", "cab" }.All(diff.Recognize));
        Assert.False(diff.Recognize("abc"));
        Assert.False(diff.Recognize("abbbbc"));
    }

    [Fact]
    public void DfsaToFsaTest()
    {
        // a|b+
        var states = new[] { 0, 1, 2, 3 };
        var final = new[] { 1, 2 };
        var transitions = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (0, 'b'), 2 },
            { (2, 'b'), 2 },
        };
        var dfsa = new Dfsa(states, 0, final, transitions);
        var fsa = dfsa.ToFsa();

        Assert.Equal(states.Length, fsa.States.Count);
        Assert.True(final.All(fsa.Final.Contains));

        Assert.Equal(new[] { 0 }, fsa.Initial);

        Assert.Equal(final.Length, fsa.Final.Count);
        Assert.All(final, s => fsa.Final.Contains(s));

        Assert.Equal(3, fsa.Transitions.Count);
        Assert.All(
            new[] { (0, "a", 1), (0, "b", 2), (2, "b", 2) },
            t => fsa.Transitions.Contains(t));
    }
}