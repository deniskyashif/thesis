using System.Collections.Generic;
using System.Linq;

class Tokenizer
{
    static readonly ISet<char> alphabet =
        Enumerable.Range(32, 95).Select(x => (char)x)
        .Concat(new[] { '\t', '\n', '\v', '\f', '\r' })
        .ToHashSet();

    public static Fst CreateForEnglish()
    {
        var whitespaces = new[] { ' ', '\t', '\n' };
        var upperCaseLetters = Enumerable.Range(65, 27).Select(x => (char)x);
        var lowerCaseLetters = Enumerable.Range(97, 27).Select(x => (char)x);
        var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        var letters = upperCaseLetters.Concat(lowerCaseLetters);

        var riseCase = alphabet
            .Select(symbol =>
                FstExtensions.FromWordPair(
                    symbol.ToString(),
                    char.IsLower(symbol)
                        ? symbol.ToString().ToUpper()
                        : symbol.ToString()))
            .Aggregate((aggr, fst) => aggr.Union(fst));

        var multiWordExpressionList = new[] { "AT LEAST", "IN SPITE OF" };
        var multiWordExpression = multiWordExpressionList
            .Select(exp => FsaExtensions.FromWord(exp))
            .Aggregate((aggr, fsa) => aggr.Union(fsa));

        var token = FsaExtensions.FromSymbolSet(letters).Plus()
            .Union(FsaExtensions.FromSymbolSet(digits).Plus())
            .Union(riseCase
                .Compose(multiWordExpression.Identity())
                .Domain())
            .Union(FsaExtensions.FromSymbolSet(alphabet.Except(whitespaces)));

        var insertLeadingNewLine = FstExtensions.FromWordPair(string.Empty, "\n")
            .Concat(FsaExtensions.FromSymbolSet(alphabet).Star().Identity());

        var clearSpaces = FstExtensions.Product(
            FsaExtensions.FromSymbolSet(whitespaces).Plus(),
            FsaExtensions.FromWord(" "))
            .ToLmlRewriter(alphabet);

        var markTokens = token.Identity()
            .Concat(FstExtensions.FromWordPair(string.Empty, "\n"))
            .ToLmlRewriter(alphabet);

        var clearLeadingSpace = 
            insertLeadingNewLine.Compose(
                FstExtensions.FromWordPair("\n", "\n").ToRewriter(alphabet),
                insertLeadingNewLine.Inverse());

        var tokenizer = clearSpaces.Compose(markTokens, clearLeadingSpace).ToRealTime();

        return tokenizer.Transducer;
    }
}