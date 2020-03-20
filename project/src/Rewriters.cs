using System;
using System.Collections.Generic;
using System.Linq;

public static class Rewriters
{
    const char cb = '†'; // marks the begining of a rewrite occurrence
    const char lb = '≪'; // marks the left boundary (start) of a rewrite occurrence
    const char rb = '≫'; // marks the right boundary (end) of a rewrite occurrence
    static readonly char[] boundaryMarkers = new char[] { cb, lb, rb };

    // Convert to an optional rewrite transducer
    public static Fst ToOptionalRewriter(this Fst fst, ISet<char> alphabet)
    {
        var idAll = FsaBuilder.All(alphabet).Identity();

        return idAll.Concat(fst.Concat(idAll).Star()).Expand();
    }

    // Convert to ab obligatory rewrite transducer
    public static Fst ToRewriter(this Fst fst, ISet<char> alphabet)
    {
        var all = FsaBuilder.All(alphabet);

        var notInDomain = all
            .Difference(all.Concat(fst.Domain()).Concat(all))
            .Identity()
            .Union(FstBuilder.FromWordPair(string.Empty, string.Empty));

        return notInDomain.Concat(fst.Concat(notInDomain).Star()).Expand().Trim();
    }
    
    // Convert to an obligatory leftmost-longest match rewrite transducer
    public static Fst ToLmlRewriter(this Fst rewriteRule, ISet<char> alphabet)
    {
        if (alphabet.Intersect(boundaryMarkers).Any())
            throw new ArgumentException("The alphabet contains invalid symbols.");

        var alphabetStarFsa = FsaBuilder.All(alphabet);
        var allSymbols = alphabet.Concat(boundaryMarkers).ToHashSet();
        var allSymbolsStarFsa = FsaBuilder.All(allSymbols);

        // Automaton recognizing all words that are not in the language of the input automaton (complement)
        Fsa NotInLang(Fsa lang) => allSymbolsStarFsa.Difference(lang);

        // Automaton recognizing all words that contain an occurrence of a word from the input automaton
        Fsa ContainsLang(Fsa lang) => allSymbolsStarFsa.Concat(lang, allSymbolsStarFsa);

        // All words w where each prefix of w representing a string in "P" is followed by a suffix which is in "S"
        Fsa IfPThenS(Fsa p, Fsa s) => NotInLang(p.Concat(NotInLang(s)));
        
        // All words for which each suffix from "S" is preceeded by a prefix from "P"
        Fsa IfSThenP(Fsa p, Fsa s) => NotInLang(NotInLang(p).Concat(s));
        Fsa PiffS(Fsa l, Fsa r) => IfPThenS(l, r).Intersect(IfSThenP(l, r));

        /* Describes the words where every position is preceded by a string with a suffix in "L"
           if and only if it is followed by a string with a prefix in "R" */
        Fsa LiffR(Fsa lang, Fsa l, Fsa r) => PiffS(lang.Concat(l), r.Concat(lang));

        var ruleDomain = rewriteRule.Domain();

        var initialMatch = // mark the beginnings of all rewrite occurrences by inserting "cb"
            Intro(allSymbols, new HashSet<char> { cb })
                .Compose(
                    LiffR(
                        allSymbolsStarFsa,
                        FsaBuilder.FromWord(cb.ToString()),
                        XIgnore(ruleDomain, allSymbols, new HashSet<char> { cb }))
                    .Identity());

        var leftToRight = // insert boundary markers ("lb", "rb") arount the leftmost rewrite occurrences
            alphabetStarFsa.Identity() // preceeded by arbitrary text that is not matched by the rule
                .Concat(
                    FstBuilder.FromWordPair(cb.ToString(), lb.ToString()), // replace intial match marker with the left boundary marker
                    IgnoreX(ruleDomain, allSymbols, new HashSet<char> { cb }).Identity(), // recognize matches with the leftover "cb" symbol inbetween the markers
                    FstBuilder.FromWordPair(string.Empty, rb.ToString())) // insert right boundary marker at the end of the matched substring
                .Star() // handle multiple rewrite occurrences
                .Concat(alphabetStarFsa.Identity()) // succeeded by arbitrary text that is not matched by the rule
                .Compose(
                    FstBuilder.FromWordPair(cb.ToString(), string.Empty).ToRewriter(allSymbols)); // delete the remaining initial match markers

        var longestMatch = // amongst occurrences with the same starting point, preserve only the longest ones
            NotInLang(
                ContainsLang(
                    FsaBuilder.FromWord(lb.ToString())
                        .Concat(IgnoreX(ruleDomain, allSymbols, new HashSet<char> { lb, rb })
                            .Intersect(ContainsLang(FsaBuilder.FromWord(rb.ToString()))))))
            .Identity();

        var replacement = // replace the rewrite occurrence and delete the left and right markers
                FstBuilder.FromWordPair(lb.ToString(), string.Empty) // delete the left boundary marker
                    .Concat(
                        rewriteRule, // perform the replacement
                        FstBuilder.FromWordPair(rb.ToString(), string.Empty)) // delete the right boundary marker
                    .ToRewriter(allSymbols);

        return initialMatch.Compose(leftToRight, longestMatch, replacement);
    }

    // Introduce symbols from a set S into an input string not containing symbols in S
    static Fst Intro(ISet<char> alphabet, ISet<char> symbols)
    {
        return FsaBuilder
            .FromSymbolSet(alphabet.Except(symbols))
            .Identity()
            .Union(
                FsaBuilder.FromEpsilon()
                    .Product(FsaBuilder.FromSymbolSet(symbols)))
            .Star();
    }

    // Same as "Intro" except symbols from S cannot occur at the end of the string
    static Fst IntroX(ISet<char> alphabet, ISet<char> symbols)
    {
        return Intro(alphabet, symbols)
            .Concat(FsaBuilder.FromSymbolSet(alphabet.Except(symbols)).Identity())
            .Optional();
    }

    // Same as "Intro" except symbols from S cannot occur at the beginning of the string
    static Fst Xintro(ISet<char> alphabet, ISet<char> symbols)
    {
        return FsaBuilder.FromSymbolSet(alphabet.Except(symbols))
            .Identity()
            .Concat(Intro(alphabet, symbols))
            .Optional();
    }

    /* For a given automaton L and a set of symbols S, construct the automaton 
        which recognizes all words from L with freely introduced symbols from S */
    static Fsa Ignore(Fsa fsa, ISet<char> fsaAlphabet, ISet<char> symbols) =>
        fsa.Identity()
            .Compose(Intro(fsaAlphabet, symbols))
            .Range();

    // Same as "Ignore" except symbols from S cannot be at the end
    static Fsa IgnoreX(Fsa fsa, ISet<char> fsaAlphabet, ISet<char> symbols) =>
        fsa.Identity()
            .Compose(IntroX(fsaAlphabet, symbols))
            .Range();

    // Same as "Ignore" except symbols from S cannot be at the beginning
    static Fsa XIgnore(Fsa fsa, ISet<char> fsaAlphabet, ISet<char> symbols) =>
        fsa.Identity()
            .Compose(Xintro(fsaAlphabet, symbols))
            .Range();
}