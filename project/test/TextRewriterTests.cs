using System.Collections.Generic;
using System.Linq;
using Xunit;

public class TextRewriterTests
{
    [Fact]
    public void OptionalRewriteRelTest()
    {
        var fst = FstExtensions.FromWordPair("ab", "d").Union(FstExtensions.FromWordPair("bc", "d"));
        var idAll = FsaExtensions.All(new HashSet<string> { "a", "b", "c", "d" }).Identity();
        var t = fst.Concat(idAll).Star();
        var opt = idAll.Concat(t).ToRealTime();

        Assert.True(opt.Transducer.Transitions.All(tr => !string.IsNullOrEmpty(tr.In)));
        Assert.Equal(string.Empty, opt.EpsilonOutputs.Single());
        
        Assert.Equal(new[] { "ab", "d" }, opt.Transducer.Process("ab").OrderBy(s => s));
        Assert.Equal(
            new[] { "abacbca", "abacda", "dacbca", "dacda" },
            opt.Transducer.Process("abacbca").OrderBy(s => s));
    }
}