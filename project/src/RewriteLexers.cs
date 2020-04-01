using System.Linq;

public static class RewriteLexers
{
    public static Bimachine CreateForEnglish()
    {
        var alphabet = Enumerable.Range(32, 95).Select(x => (char)x)
            .Concat(new[] { '\t', '\n', '\v', '\f', '\r' })
            .ToHashSet();
        var whitespaces = new[] { ' ', '\t', '\n' };
        var upperCaseLetters = Enumerable.Range(65, 27).Select(x => (char)x);
        var lowerCaseLetters = Enumerable.Range(97, 27).Select(x => (char)x);
        var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        var letters = upperCaseLetters.Concat(lowerCaseLetters);

        var riseCase = alphabet
            .Select(symbol =>
                FstBuilder.FromWordPair(
                    symbol.ToString(),
                    char.IsLower(symbol)
                        ? symbol.ToString().ToUpper()
                        : symbol.ToString()))
            .Aggregate((aggr, fst) => aggr.Union(fst))
            .Star();

        var multiWordExprList = new[] { "AT LEAST", "IN SPITE OF", "HEAD OVER HEELS" };
        var multiWordExpr =
            multiWordExprList
                .Select(exp => FsaBuilder.FromWord(exp))
                .Aggregate((aggr, fsa) => aggr.Union(fsa));

        var token =
            FsaBuilder.FromSymbolSet(letters)
            .Plus()
            .Union(
                FsaBuilder.FromSymbolSet(digits).Plus(),
                riseCase.Compose(multiWordExpr.Identity()).Domain(),
                FsaBuilder.FromSymbolSet(alphabet.Except(whitespaces)));

        var insertLeadingNewLine =
            FstBuilder.FromWordPair(string.Empty, "\n")
                .Concat(FsaBuilder.FromSymbolSet(alphabet).Star().Identity());

        var clearSpaces =
                FsaBuilder.FromSymbolSet(whitespaces)
                .Plus()
                .Product(FsaBuilder.FromWord(" "))
                .ToLmlRewriter(alphabet);

        var markTokens =
            token.Identity()
                .Concat(FstBuilder.FromWordPair(string.Empty, "\n"))
                .ToLmlRewriter(alphabet);

        var clearLeadingSpace =
            insertLeadingNewLine.Compose(
                FstBuilder.FromWordPair("\n ", "\n").ToRewriter(alphabet),
                insertLeadingNewLine.Inverse());

        return clearSpaces.Compose(markTokens, clearLeadingSpace).ToBimachine(alphabet);
    }
}