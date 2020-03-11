using System.Collections.Generic;
using System.Linq;
using Xunit;

public class RewriterTests
{
    [Fact]
    public void OptionalRewriteRelTest()
    {
        // ab|bc -> d
        var fst = FstBuilder.FromWordPair("ab", "d").Union(FstBuilder.FromWordPair("bc", "d"));
        
        var opt = fst.ToOptionalRewriter(new HashSet<char> { 'a', 'b', 'c', 'd' });

        Assert.Equal(string.Empty, opt.Process(string.Empty).Single());
        Assert.Equal(new[] { "ab", "d" }, opt.Process("ab").OrderBy(s => s));
        Assert.Equal(
            new[] { "abacbca", "abacda", "dacbca", "dacda" },
            opt.Process("abacbca").OrderBy(s => s));
    }

    [Fact]
    public void ObligatoryRewriteRelTest()
    {
        // ab|bc -> d
        var fst = FstBuilder.FromWordPair("ab", "d").Union(FstBuilder.FromWordPair("bc", "d"));
        var obl = fst.ToRewriter(new HashSet<char> { 'a', 'b', 'c', 'd' });

        Assert.Equal(string.Empty, obl.Process(string.Empty).Single());
        Assert.Equal("d", obl.Process("ab").Single());
        Assert.Equal("dacda", obl.Process("abacbca").Single());
        Assert.Equal(new[] { "ad", "dc" }, obl.Process("abc").OrderBy(w => w));
    }

    [Fact]
    public void LmlRewriterTest()
    {
        var alphabet = new HashSet<char> {'a', 'b', 'c', 'd'};
        var rule1 = FstBuilder.FromWordPair("ab", "d")
            .Union(FstBuilder.FromWordPair("bc", "d"))
            .Expand();
        var rule2 = FstBuilder.FromWordPair("cd", "CD");

        var transducer = rule1.ToLmlRewriter(alphabet)
            .Compose(rule2.ToLmlRewriter(alphabet));

        Assert.Equal(string.Empty, transducer.Process(string.Empty).Single());
        Assert.Equal("dc", transducer.Process("abc").Single());
        Assert.Equal("dCDdc", transducer.Process("abcbcabc").Single());

        var bm = transducer.ToBimachine(alphabet);
        // Assert.Equal(string.Empty, bm.Process(string.Empty));
        Assert.Equal("dc", bm.Process("abc"));
        Assert.Equal("dCDdc", bm.Process("abcbcabc"));
    }

    [Fact]
    public void LmlRewriterTest1()
    {
        // a+b|aa -> X
        var alphabet = new HashSet<char> {'a', 'b', 'c', 'X' };
        var rule = new Fst(
            new[] { 0, 1, 2, 3, 4 },
            new[] { 0 },
            new[] { 2, 4 },
            new[] { 
                (0, "a", string.Empty, 1), 
                (1, "a", string.Empty, 1),
                (1, "b", "X", 2),
                (0, "a", string.Empty, 3),
                (3, "a", "X", 4),
            });
        var transducer = rule.ToLmlRewriter(alphabet);

        Assert.Equal("X", transducer.Process("aaab").Single());
        Assert.Equal("XX", transducer.Process("aaaa").Single());

        var bm = transducer.ToBimachine(alphabet);
        Assert.Equal("X", bm.Process("aaab"));
        Assert.Equal("XX", bm.Process("aaaa"));
    }

    [Fact]
    public void ComposedLmlRewriterTest()
    {
        // (a|b)* -> d -> D
        var alphabet = new HashSet<char> {'a', 'b', 'c', 'd', 'D' };
        var rule = FstBuilder.FromWordPair("a", "d")
            .Union(FstBuilder.FromWordPair("b", "d"))
            .Star()
            .Compose(FstBuilder.FromWordPair("d", "D"));

        var transducer = rule.ToLmlRewriter(alphabet);

        Assert.Equal("DDDDDDDDcDDD", transducer.Process("abababbbcbba").Single());
        Assert.Equal("DDDD", transducer.Process("aaaa").Single());
        Assert.Equal("cc", transducer.Process("cc").Single());

        var bm = transducer.ToBimachine(alphabet);
        Assert.Equal("DDDDDDDDcDDD", bm.Process("abababbbcbba"));
        Assert.Equal("DDDD", bm.Process("aaaa"));
        Assert.Equal("cc", bm.Process("cc"));
    }

    [Fact]
    public void ClearWhiteSpaceRewriterTest()
    {
        // WS+ -> ''
        var whitespaces = new[] { ' ', '\t' };
        var alphabet = new[] { 'a', 'b' }.Concat(whitespaces).ToHashSet();

        var clearWS = FsaBuilder.FromSymbolSet(whitespaces)
            .Plus()
            .Product(FsaBuilder.FromEpsilon())
            .ToLmlRewriter(alphabet)
            .ToBimachine(alphabet);

        Assert.Equal(string.Empty, clearWS.Process("          "));
        Assert.Equal("abbaabbbabbabb", clearWS.Process("a bbaab  bbab     bab b"));
        Assert.Equal("ababb", clearWS.Process("ababb"));
    }

    [Fact]
    public void InsertNewLineAfterSymbolTest()
    {
        // a+ -> a+'\n'
        var alphabet = new HashSet<char> { 'a', 'b', 'c', '\n' };

        var fst = FsaBuilder.FromSymbolSet(new[] { 'a' })
            .Plus()
            .Identity()
            .Concat(FstBuilder.FromWordPair(string.Empty, "\n"))
            .ToLmlRewriter(alphabet)
            .ToBimachine(alphabet);

        Assert.Equal("baa\nbca\nba\n", fst.Process("baabcaba"));
        Assert.Equal("bbcbcb", fst.Process("bbcbcb"));
        Assert.Equal("a\nba\nbb", fst.Process("ababb"));
    }
}