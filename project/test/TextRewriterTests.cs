using System.Collections.Generic;
using System.Linq;
using Xunit;

public class TextRewriterTests
{
    [Fact]
    public void OptionalRewriteRelTest()
    {
        // ab|bc -> d
        var fst = FstExtensions.FromWordPair("ab", "d").Union(FstExtensions.FromWordPair("bc", "d"));
        
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
        var fst = FstExtensions.FromWordPair("ab", "d").Union(FstExtensions.FromWordPair("bc", "d"));
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
        var rule1 = FstExtensions.FromWordPair("ab", "d")
            .Union(FstExtensions.FromWordPair("bc", "d"))
            .Expand();
        var rule2 = FstExtensions.FromWordPair("cd", "CD");

        var transducer = rule1.ToLmlRewriter(alphabet)
            .Compose(rule2.ToLmlRewriter(alphabet));

        Assert.Equal(string.Empty, transducer.Process(string.Empty).Single());
        Assert.Equal("dc", transducer.Process("abc").Single());
        Assert.Equal("dCDdc", transducer.Process("abcbcabc").Single());
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
    }
}