using System;
using System.Collections.Generic;
using System.Linq;

public static class Rewriter
{
    const char lb = '≪';
    const char rb = '≫';
    const char cb = '†';

    // Optional rewrite transducer
    public static Fst ToOptionalRewriter(this Fst fst, ISet<char> alphabet)
    {
        var idAll = FsaBuilder.All(alphabet).Identity();

        return idAll.Concat(fst.Concat(idAll).Star()).Expand();
    }

    // Obligatory rewrite transducer
    public static Fst ToRewriter(this Fst fst, ISet<char> alphabet)
    {
        var all = FsaBuilder.All(alphabet);

        var notInDomain = all
            .Difference(all.Concat(fst.Domain()).Concat(all))
            .Identity()
            .Union(FstBuilder.FromWordPair(string.Empty, string.Empty));

        return notInDomain.Concat(fst.Concat(notInDomain).Star()).Expand().Trim();
    }
    
    // Obligatory leftmost-longest match rewrite transducer
    public static Fst ToLmlRewriter(this Fst rule, ISet<char> alphabet)
    {
        if (alphabet.Intersect(new[] { lb, rb, cb }).Any())
            throw new ArgumentException("The alphabet contains invalid symbols.");

        var alphabetStarFsa = FsaBuilder.All(alphabet);
        var allSymbols = alphabet.Concat(new[] { lb, rb, cb }).ToHashSet();
        var allSymbolsStarFsa = FsaBuilder.All(allSymbols);

        Fsa NotInLang(Fsa lang) => allSymbolsStarFsa.Difference(lang);
        Fsa ContainLang(Fsa lang) => allSymbolsStarFsa.Concat(lang, allSymbolsStarFsa);
        Fsa IfPThenS(Fsa p, Fsa s) => NotInLang(p.Concat(NotInLang(s)));
        Fsa IfSThenP(Fsa p, Fsa s) => NotInLang(NotInLang(p).Concat(s));
        Fsa PiffS(Fsa p, Fsa s) => IfPThenS(p, s).Intersect(IfSThenP(p, s));
        Fsa LiffR(Fsa lang, Fsa l, Fsa r) => PiffS(lang.Concat(l), r.Concat(lang));

        var ruleDomain = rule.Domain();

        var initialMatch =
            Intro(allSymbols, new HashSet<char> { cb })
                .Compose(
                    LiffR(
                        allSymbolsStarFsa,
                        FsaBuilder.FromWord(cb.ToString()),
                        XIgnore(ruleDomain, allSymbols, new HashSet<char> { cb }))
                    .Identity());

        var leftToRight =
            alphabetStarFsa.Identity()
                .Concat(
                    FstBuilder.FromWordPair(cb.ToString(), lb.ToString()),
                    IgnoreX(ruleDomain, allSymbols, new HashSet<char> { cb }).Identity(),
                    FstBuilder.FromWordPair(string.Empty, rb.ToString()))
                .Star()
                .Concat(alphabetStarFsa.Identity())
                .Compose(
                    FstBuilder.FromWordPair(cb.ToString(), string.Empty).ToRewriter(allSymbols));

        var longestMatch =
            NotInLang(
                ContainLang(
                    FsaBuilder.FromWord(lb.ToString())
                        .Concat(IgnoreX(ruleDomain, allSymbols, new HashSet<char> { lb, rb })
                            .Intersect(ContainLang(FsaBuilder.FromWord(rb.ToString()))))))
            .Identity();

        var replacement = 
                FstBuilder.FromWordPair(lb.ToString(), string.Empty)
                    .Concat(rule, FstBuilder.FromWordPair(rb.ToString(), string.Empty))
                    .ToRewriter(allSymbols);

        return initialMatch.Compose(leftToRight, longestMatch, replacement);
    }

    static Fst Intro(ISet<char> alphabet, ISet<char> symbols)
    {
        return FsaBuilder
            .FromSymbolSet(alphabet.Except(symbols))
            .Identity()
            .Union(
                FsaBuilder.FromWord(string.Empty).Product(FsaBuilder.FromSymbolSet(symbols)))
            .Star();
    }

    static Fst IntroX(ISet<char> alphabet, ISet<char> symbols)
    {
        return Intro(alphabet, symbols)
            .Concat(FsaBuilder
                .FromSymbolSet(alphabet.Except(symbols))
                .Identity())
            .Option();
    }

    static Fst Xintro(ISet<char> alphabet, ISet<char> symbols)
    {
        return FsaBuilder
            .FromSymbolSet(alphabet.Except(symbols))
            .Identity()
            .Concat(Intro(alphabet, symbols))
            .Option();
    }

    static Fsa Ignore(Fsa lang, ISet<char> alphabet, ISet<char> symbols) =>
        lang.Identity().Compose(Intro(alphabet, symbols)).Range();

    static Fsa IgnoreX(Fsa lang, ISet<char> alphabet, ISet<char> symbols) =>
        lang.Identity().Compose(IntroX(alphabet, symbols)).Range();

    static Fsa XIgnore(Fsa lang, ISet<char> alphabet, ISet<char> symbols) =>
        lang.Identity().Compose(Xintro(alphabet, symbols)).Range();
}