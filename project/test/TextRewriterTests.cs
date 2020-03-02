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
        var idAll = FsaExtensions.All(new HashSet<char> { 'a', 'b', 'c', 'd' }).Identity();
        var opt = idAll.Concat(fst.Concat(idAll).Star()).Expand();

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
        var all = FsaExtensions.All(new HashSet<char> { 'a', 'b', 'c', 'd' });

        var notInDomain = all
            .Difference(all.Concat(fst.Domain()).Concat(all))
            .Identity()
            .Union(FstExtensions.FromWordPair(string.Empty, string.Empty));

        var obl = notInDomain.Concat(fst.Concat(notInDomain).Star()).Expand().Trim();

        Assert.Equal(string.Empty, obl.Process(string.Empty).Single());
        Assert.Equal("d", obl.Process("ab").Single());
        Assert.Equal("dacda", obl.Process("abacbca").Single());
        Assert.Equal(new[] { "ad", "dc" }, obl.Process("abc").OrderBy(w => w));
    }

    [Fact]
    public void LMLRewriterTest()
    {
        const char lb = '<';
        const char rb = '>';
        const char cb = '|';

        var rule = FstExtensions.FromWordPair("ab", "d")
            .Union(FstExtensions.FromWordPair("bc", "d"))
            .Expand();

        var alphabet = new HashSet<char> { 'a', 'b', 'c', 'd' };
        var allAlphabet = FsaExtensions.All(alphabet);

        var allSymbols = alphabet.Concat(new[] { lb, rb, cb }).ToHashSet();
        var all = FsaExtensions.All(allSymbols);

        Fsa NotInLang(Fsa lang) => all.Difference(lang);
        Fsa ContainLang(Fsa lang) => all.Concat(lang, all);

        Fst Intro(ISet<char> symbols)
        {
            return FsaExtensions
                .FromSymbolSet(allSymbols.Except(symbols))
                .Identity()
                .Union(
                    FstExtensions.Product(
                        FsaExtensions.FromWord(string.Empty),
                        FsaExtensions.FromSymbolSet(symbols)))
                .Star();
        }

        Fst IntroX(ISet<char> symbols)
        {
            return Intro(symbols)
                .Concat(FsaExtensions
                    .FromSymbolSet(allSymbols.Except(symbols))
                    .Identity())
                .Option();
        }

        Fst Xintro(ISet<char> symbols)
        {
            return FsaExtensions
                .FromSymbolSet(allSymbols.Except(symbols))
                .Identity()
                .Concat(Intro(symbols))
                .Option();
        }

        Fsa Ignore(Fsa lang, ISet<char> symbols) =>
            lang.Identity().Compose(Intro(symbols)).Range();

        Fsa IgnoreX(Fsa lang, ISet<char> symbols) =>
            lang.Identity().Compose(IntroX(symbols)).Range();

        Fsa XIgnore(Fsa lang, ISet<char> symbols) =>
            lang.Identity().Compose(Xintro(symbols)).Range();

        Fsa IfPThenS(Fsa p, Fsa s) =>
            NotInLang(p.Concat(NotInLang(s)));

        Fsa IfSThenP(Fsa p, Fsa s) =>
            NotInLang(NotInLang(p).Concat(s));

        Fsa PiffS(Fsa p, Fsa s) =>
            IfPThenS(p, s).Intersect(IfSThenP(p, s));

        Fsa LiffR(Fsa l, Fsa r) => 
            PiffS(all.Concat(l), r.Concat(all));

        Fst Replace(ISet<char> symbols, Fst fst)
        {
            var all = FsaExtensions.All(symbols);

            var notInDomain = all
                .Difference(all.Concat(fst.Domain()).Concat(all))
                .Identity()
                .Union(FstExtensions.FromWordPair(string.Empty, string.Empty));

            return notInDomain.Concat(fst.Concat(notInDomain).Star()).Expand().Trim();
        }

        var domain = rule.Domain();

        var initialMatch = Intro(new HashSet<char> { cb })
            .Compose(
                LiffR(
                    FsaExtensions.FromWord(cb.ToString()),
                    XIgnore(domain, new HashSet<char> { cb }))
                .Identity());

        var leftToRight =
            allAlphabet.Identity()
                .Concat(
                    FstExtensions.FromWordPair(cb.ToString(), lb.ToString()),
                    IgnoreX(domain, new HashSet<char> { cb }).Identity(),
                    FstExtensions.FromWordPair(string.Empty, rb.ToString()))
                .Star()
                .Concat(allAlphabet.Identity())
                .Compose(
                    Replace(allSymbols, FstExtensions.FromWordPair(cb.ToString(), string.Empty)));

        var longestMatch =
            NotInLang(
                ContainLang(
                    FsaExtensions.FromWord(lb.ToString())
                        .Concat(IgnoreX(domain, new HashSet<char> { lb, rb })
                            .Intersect(ContainLang(FsaExtensions.FromWord(rb.ToString()))))))
                .Identity();

        var replacement = Replace(
            allSymbols,
            FstExtensions.FromWordPair(lb.ToString(), string.Empty)
                .Concat(rule, FstExtensions.FromWordPair(rb.ToString(), string.Empty)));

        var (transducer, epsilonOutputs) = initialMatch
            .Compose(leftToRight, longestMatch, replacement)
            .ToRealTime();

        Assert.Equal("dc", transducer.Process("abc").Single());
        Assert.Equal("dcddc", transducer.Process("abcbcabc").Single());
    }
}