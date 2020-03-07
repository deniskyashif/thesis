using System;
using System.Collections.Generic;
using System.Linq;

static class Tokenizers
{
    static readonly ISet<char> alphabet =
        Enumerable.Range(32, 95).Select(x => (char)x)
        .Concat(new[] { '\t', '\n', '\v', '\f', '\r' })
        .ToHashSet();

    public static Bimachine CreateForEnglish()
    {
        var whitespaces = new[] { ' ', '\t', '\n' };
        var upperCaseLetters = Enumerable.Range(65, 27).Select(x => (char)x);
        var lowerCaseLetters = Enumerable.Range(97, 27).Select(x => (char)x);
        var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        var letters = upperCaseLetters.Concat(lowerCaseLetters);

        Console.WriteLine("Constructing the \"rise case\" transducer.");
        
        var riseCase = alphabet
            .Select(symbol =>
                FstBuilder.FromWordPair(
                    symbol.ToString(),
                    char.IsLower(symbol)
                        ? symbol.ToString().ToUpper()
                        : symbol.ToString()))
            .Aggregate((aggr, fst) => aggr.UnionWith(fst))
            .Star();

        Console.WriteLine("Constructing the \"multi word expression list\" transducer.");
        
        var multiWordExprList = new[] { "AT LEAST", "IN SPITE OF" };
        var multiWordExpr = 
            multiWordExprList
                .Select(exp => FsaBuilder.FromWord(exp))
                .Aggregate((aggr, fsa) => aggr.Union(fsa));

        Console.WriteLine("Constructing the \"token\" transducer.");
        
        var token = 
            FsaBuilder.FromSymbolSet(letters)
            .Plus()
            .Union(
                FsaBuilder.FromSymbolSet(digits).Plus(),
                riseCase.Compose(multiWordExpr.Identity()).Domain(),
                FsaBuilder.FromSymbolSet(alphabet.Except(whitespaces)));

        Console.WriteLine("Constructing the \"insert leading newline\" transducer.");

        var insertLeadingNewLine = 
            FstBuilder.FromWordPair(string.Empty, "\n")
                .Concat(FsaBuilder.FromSymbolSet(alphabet).Star().Identity());

        Console.WriteLine("Constructing the \"clear spaces\" transducer.");
        
        var clearSpaces = 
            FstOperations.Product(
                FsaBuilder.FromSymbolSet(whitespaces).Plus(),
                FsaBuilder.FromWord(" "))
            .ToLmlRewriter(alphabet);

        Console.WriteLine("Constructing the \"mark tokens\" transducer.");
        
        var markTokens = 
            token.Identity()
                .Concat(FstBuilder.FromWordPair(string.Empty, "\n"))
                .ToLmlRewriter(alphabet);

        Console.WriteLine("Constructing the \"clear leading whitespace\" transducer.");
        
        var clearLeadingSpace = 
            insertLeadingNewLine.Compose(
                FstBuilder.FromWordPair("\n ", "\n").ToRewriter(alphabet),
                insertLeadingNewLine.Inverse());

        Console.WriteLine("Creating the composed transducer.");
        
        var fst = clearSpaces.Compose(markTokens, clearLeadingSpace);
        
        Console.WriteLine("Converting to a bimachine.");
        
        var bm = fst.ToBimachine(alphabet);

        return bm;
    }
}