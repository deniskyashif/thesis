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
        var idAll = FsaExtensions.All(new HashSet<string> { "a", "b", "c", "d" }).Identity();

        var (opt, e) = idAll.Concat(fst.Concat(idAll).Star()).ToRealTime();

        // Assert.True(opt.Transitions.All(tr => !string.IsNullOrEmpty(tr.In)));
        // Assert.Equal(string.Empty, e.Single());

        // var opt = idAll.Concat(fst.Concat(idAll).Star());
        
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
        var all = FsaExtensions.All(new HashSet<string> { "a", "b", "c", "d" });

        var notInDomain = all
            .Difference(all.Concat(fst.Domain()).Concat(all))
            .Identity()
            .Union(FstExtensions.FromWordPair(string.Empty, string.Empty));

        var (obl, e) = notInDomain
            .Concat(fst.Concat(notInDomain).Star())
            .ToRealTime();

        Assert.True(obl.Transitions.All(tr => !string.IsNullOrEmpty(tr.In)));
        Assert.Equal(string.Empty, e.Single());

        // var obl = notInDomain.Concat(fst.Concat(notInDomain).Star());

        Assert.Equal("d", obl.Process("ab").Single());
        Assert.Equal("dacda", obl.Process("abacbca").Single());
        Assert.Equal(new[] { "ad", "dc"}, obl.Process("abc").OrderBy(w => w));
    }
}