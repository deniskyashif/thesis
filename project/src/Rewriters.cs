using System;
using System.Collections.Generic;
using System.Linq;

/*
    "Optional", "obligatory" and "obligatory leftmost-longsest match 
    rewrite transducers based on "Directed Replacement" by Karttunen (1996)
*/
public static class Rewriters
{
    const char cb = '\0'; // marks the begining of a rewrite occurrence
    const char lb = '\u0002'; // marks the left boundary (start) of a rewrite occurrence
    const char rb = '\u0003'; // marks the right boundary (end) of a rewrite occurrence
    static readonly char[] markers = new char[] { cb, lb, rb };

    // Convert an FST to an optional rewrite transducer
    public static Fst ToOptionalRewriter(this Fst fst, ISet<char> alphabet)
    {
        var idAll = FsaBuilder.All(alphabet).Identity();
        return idAll.Concat(fst.Concat(idAll).Star()).Expand();
    }

    // Convert to an obligatory rewrite transducer
    public static Fst ToRewriter(this Fst fst, ISet<char> alphabet)
    {
        var all = FsaBuilder.All(alphabet);
        var notInDomain = all
            .Difference(all.Concat(fst.Domain()).Concat(all))
            .Identity()
            .Optional();

        return notInDomain.Concat(fst.Concat(notInDomain).Star());
    }

    // Convert to an obligatory leftmost-longest match rewrite transducer (Karttunen 1996)
    public static Fst ToLmlRewriter(this Fst fst, ISet<char> alphabet)
    {
        if (alphabet.Intersect(markers).Any())
            throw new ArgumentException("The alphabet contains invalid symbols.");

        var alphabetStarFsa = FsaBuilder.All(alphabet).Minimal();
        var allSymbols = alphabet.Concat(markers).ToHashSet();
        var allSymbolsStarFsa = FsaBuilder.All(allSymbols).Minimal();

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
        Fsa LiffR(Fsa l, Fsa r) => PiffS(allSymbolsStarFsa.Concat(l), r.Concat(allSymbolsStarFsa));

        var fstDomain = fst.Domain();

        var initialMatch = // mark the beginnings of all rewrite occurrences by inserting "cb"
            Intro(allSymbols, new HashSet<char> { cb })
                .Compose(
                    LiffR(
                        FsaBuilder.FromSymbol(cb),
                        XIgnore(fstDomain, allSymbols, new HashSet<char> { cb }))
                    .Identity());

        var leftToRight = // insert boundary markers ("lb", "rb") around the leftmost rewrite occurrences
            alphabetStarFsa.Identity() // preceeded by arbitrary text that is not matched by the rule
                .Concat(
                    FstBuilder.FromWordPair(cb.ToString(), lb.ToString()), // replace intial match marker with the left boundary marker
                    IgnoreX(fstDomain, allSymbols, new HashSet<char> { cb }).Identity(), // recognize matches with the leftover "cb" symbol inbetween the markers
                    FstBuilder.FromWordPair(string.Empty, rb.ToString())) // insert right boundary marker at the end of the matched substring
                .Star() // handle multiple rewrite occurrences
                .Concat(alphabetStarFsa.Identity()) // succeeded by arbitrary text that is not matched by the rule
                .Compose(
                    FstBuilder.FromWordPair(cb.ToString(), string.Empty).ToRewriter(allSymbols)); // delete the remaining initial match markers

        var includesNotLongestMatches =
            ContainsLang(
                FsaBuilder.FromSymbol(lb)
                    .Concat(
                        IgnoreX(fstDomain, allSymbols, new HashSet<char> { lb, rb })
                            .Intersect(ContainsLang(FsaBuilder.FromSymbol(rb)))));
        // amongst occurrences with the same starting point, preserve only the longest ones
        var longestMatch = NotInLang(includesNotLongestMatches).Identity();

        var replacement = // replace the rewrite occurrence and delete the left and right markers
            FstBuilder.FromWordPair(lb.ToString(), string.Empty) // delete the left boundary marker
                .Concat(
                    fst, // perform the replacement
                    FstBuilder.FromWordPair(rb.ToString(), string.Empty)) // delete the right boundary marker
                .ToRewriter(allSymbols);

        return initialMatch.Compose(leftToRight, longestMatch, replacement);
    }

    // Convert to an obligatory leftmost-longest match rewrite transducer (van Noord, Gerdemann 1999)
    public static Fst ToLmlRewriter2(this Fst fst, ISet<char> alphabet)
    {
        const char notMarkerSymbol = '0';
        const char isMarkerSymbol = '1';
        var markers = new[] { notMarkerSymbol, isMarkerSymbol };
        
        var sigFsa = FsaBuilder.FromSymbolSet(alphabet)
            .Concat(FsaBuilder.FromSymbolSet(new[] { notMarkerSymbol }));
        var sigStarFsa = sigFsa.Star().Minimal();
        var xSig = alphabet.Concat(markers).ToHashSet();
        var xSigFsa = sigFsa.Concat(FsaBuilder.FromSymbolSet(markers));
        var xSigStarFsa = xSigFsa.Star().Minimal();

        const char lb1Marker = '<'; // <1
        const char lb2Marker = '≪'; // <2
        const char rb1Marker = '>'; // 1>
        const char rb2Marker = '≫'; // 2>

        var lb1 = FsaBuilder.FromSymbol(lb1Marker).Concat(FsaBuilder.FromSymbol(isMarkerSymbol));
        var lb2 = FsaBuilder.FromSymbol(lb2Marker).Concat(FsaBuilder.FromSymbol(isMarkerSymbol));
        var rb2 = FsaBuilder.FromSymbol(rb2Marker).Concat(FsaBuilder.FromSymbol(isMarkerSymbol));
        var rb1 = FsaBuilder.FromSymbol(rb1Marker).Concat(FsaBuilder.FromSymbol(isMarkerSymbol));
        var lb = lb1.Union(lb2);
        var rb = rb1.Union(rb2);
        var b1 = lb1.Union(rb1);
        var b2 = lb2.Union(rb2);
        var brack = lb.Union(rb);

        Fsa Not(Fsa lang) => xSigStarFsa.Difference(lang);
        Fsa Contain(Fsa lang) => xSigStarFsa.Concat(lang, xSigStarFsa);

        Fsa IfPThenS(Fsa l1, Fsa l2) => Not(l1.Concat(Not(l2)));
        Fsa IfSThenP(Fsa l1, Fsa l2) => Not(Not(l1).Concat(l2));
        Fsa PiffS(Fsa l1, Fsa l2) => IfPThenS(l1, l2).Intersect(IfSThenP(l1, l2));
        Fsa LiffR(Fsa l1, Fsa l2) => PiffS(xSigStarFsa.Concat(l1), l2.Concat(xSigStarFsa));

        var trueFsa = xSigStarFsa;
        var falseFsa = FsaBuilder.FromEpsilon();

        // Fsa CoerceToBoolean(Fsa l) => l.Identity()
        //     .Compose(trueFsa.Product(trueFsa)).Range();

        // Fst If(Fsa cond, Fst then, Fst @else) =>
        //     CoerceToBoolean(cond).Identity().Compose(then)
        //         .Union(Not(CoerceToBoolean(cond)).Identity().Compose(@else));

        var leftCtx = FsaBuilder.FromEpsilon();
        var rightCtx = FsaBuilder.FromEpsilon();
        var domainT = fst.Domain();

        var nonMarkersFst = FsaBuilder.FromSymbolSet(alphabet)
            .Identity()
            .Concat(FstBuilder.FromWordPair(string.Empty, notMarkerSymbol.ToString()));

        Fsa NonMarkers(Fsa l) => l.Identity().Compose(nonMarkersFst).Range();

        // begin R
        var cond = FsaBuilder.FromEpsilon().Intersect(rightCtx);
        var then = FsaBuilder.FromEpsilon().Product(rb2).Concat(sigFsa.Identity()).Star()
            .Concat(FsaBuilder.FromEpsilon().Product(rb2));
        var @else = Intro(xSig, new HashSet<char> { rb2Marker }).Compose(
            LiffR(rb2, XIgnore(NonMarkers(rightCtx), xSig, new HashSet<char> { rb2Marker })).Identity());

        // var r = If(cond, then, @else);
        var r = FsaBuilder.FromEpsilon().Product(rb2).Concat(sigFsa.Identity()).Star()
            .Concat(FsaBuilder.FromEpsilon().Product(rb2));
        // end R

        var f = Intro(xSig, new HashSet<char> { lb2Marker })
            .Compose(
                LiffR(lb2, XIgnoreX(NonMarkers(domainT), xSig, new HashSet<char> { lb2Marker, rb2Marker })
                    .Concat(lb2.Optional(), rb2)).Identity());

        // begin lr
        var leftToRightBody = lb2.Product(lb1)
            .Concat(
                Ignore(NonMarkers(domainT), xSig, new HashSet<char> { lb2Marker, rb2Marker }).Identity()
                    .Compose(Intro(xSig, new HashSet<char> { lb2Marker }).Inverse()))
            .Concat(rb2.Product(rb1));

        var leftToRight = xSigStarFsa.Identity()
            .Concat(leftToRightBody)
            .Star()
            .Concat(xSigStarFsa.Identity());
        // end lr

        // begin longest match
        var longestBody = lb1
            .Concat(
                IgnoreX(NonMarkers(domainT), xSig, new HashSet<char> { lb1Marker, lb2Marker, rb1Marker, rb2Marker })
                    .Intersect(Contain(rb1)))
            .Concat(rb);

        var longestMatch = Not(Contain(longestBody)).Identity()
            .Compose(Intro(xSig, new HashSet<char> { rb2Marker }).Inverse());
        // end longest match

        var auxReplace = sigFsa.Union(lb2).Identity()
            .Union(lb1.Identity()
                .Concat(nonMarkersFst.Inverse().Compose(fst, nonMarkersFst))
                .Concat(rb1.Product(FsaBuilder.FromEpsilon())))
            .Star();

        var l1 = Ignore(
            IfSThenP(
                IgnoreX(xSigStarFsa.Concat(NonMarkers(leftCtx)), xSig, new HashSet<char> { lb1Marker }),
                lb1.Concat(xSigStarFsa)),
            xSig,
            new HashSet<char> { lb2Marker })
        .Identity()
        .Compose(Intro(xSig, new HashSet<char> { lb1Marker }).Inverse());

        var l2 = IfSThenP(
            IgnoreX(Not(xSigStarFsa.Concat(NonMarkers(leftCtx))), xSig, new HashSet<char> { lb2Marker }),
            lb2.Concat(xSigStarFsa))
        .Identity()
        .Compose(Intro(xSig, new HashSet<char> { lb2Marker }).Inverse());

        var replace = nonMarkersFst.Compose(
            r, f,
            leftToRight, longestMatch, auxReplace,
            l1, l2,
            nonMarkersFst.Inverse());

        return replace;
    }

    // Introduce symbols from a set S into an input string not containing symbols in S
    static Fst Intro(ISet<char> alphabet, ISet<char> symbols) =>
        FsaBuilder.FromSymbolSet(alphabet.Except(symbols))
            .Identity()
            .Union(
                FsaBuilder.FromEpsilon()
                    .Product(FsaBuilder.FromSymbolSet(symbols)))
            .Star();

    // Same as "Intro" except symbols from S cannot occur at the end of the string
    static Fst IntroX(ISet<char> alphabet, ISet<char> symbols) =>
        Intro(alphabet, symbols)
            .Concat(FsaBuilder.FromSymbolSet(alphabet.Except(symbols)).Identity())
            .Optional();

    // Same as "Intro" except symbols from S cannot occur at the beginning of the string
    static Fst Xintro(ISet<char> alphabet, ISet<char> symbols) =>
        FsaBuilder.FromSymbolSet(alphabet.Except(symbols))
            .Identity()
            .Concat(Intro(alphabet, symbols))
            .Optional();

    static Fst XintroX(ISet<char> alphabet, ISet<char> symbols)
    {
        var f = FsaBuilder.FromSymbolSet(alphabet.Except(symbols)).Identity();
        var s = Intro(alphabet, symbols);
        var res = f.Concat(s, f).Union(f);

        return res.Optional();
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

    static Fsa XIgnoreX(Fsa fsa, ISet<char> fsaAlphabet, ISet<char> symbols) =>
        fsa.Identity()
            .Compose(XintroX(fsaAlphabet, symbols))
            .Range();
}
