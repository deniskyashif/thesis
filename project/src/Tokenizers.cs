using System;
using System.Collections.Generic;
using System.Linq;

public static class Tokenizers
{
    public static Bimachine CreateForArithmeticExpr()
    {
        /*
            INT: [0-9]+
            FLOAT: INT.INT
            OP: '+' | '-' | '*' | '/'
            epsilon -> WS+ 
        */
        const string tokenBoundary = "\n";
        var whitespaces = new[] { ' ', '\t' };
        var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        var operators = new[] { '+', '-', '/', '*' };
        var floatingPoint = new[] { '.' };
        var alphabet = operators
            .Concat(digits)
            .Concat(whitespaces)
            .Concat(floatingPoint)
            .ToHashSet();

        var clearWS = FsaBuilder.FromSymbolSet(whitespaces)
            .Plus()
            .Product(FsaBuilder.FromEpsilon())
            .ToLmlRewriter(alphabet);

        var integerFsa = FsaBuilder.FromSymbolSet(digits).Plus();
        var floatFsa = integerFsa.Concat(FsaBuilder.FromSymbolSet(floatingPoint)).Concat(integerFsa);
        var operatorFsa = FsaBuilder.FromSymbolSet(operators);
        var insertIntBoundary = FstBuilder.FromWordPair(string.Empty, $"<INT>{tokenBoundary}");
        var insertFloatBoundary = FstBuilder.FromWordPair(string.Empty, $"<FLOAT>{tokenBoundary}");
        var insertOperatorBoundary = FstBuilder.FromWordPair(string.Empty, $"<OP>{tokenBoundary}");
        
        var markTokens = integerFsa
            .Identity()
            .Concat(insertIntBoundary)
            .Union(
                floatFsa.Identity().Concat(insertFloatBoundary),
                operatorFsa.Identity().Concat(insertOperatorBoundary))
            .ToLmlRewriter(alphabet);

        return clearWS.Compose(markTokens).ToBimachine(alphabet);
    }

    public static Bimachine CreateForRegularExpr()
    {
        /*
            SYMBOL: [a-zA-Z0-9]
            LPAREN: '('
            RPAREN: ')'
            OP: '|' | '.' | '*'
        */
        const string tokenBoundary = "\n";
        var upperCaseLetters = Enumerable.Range(65, 27).Select(x => (char)x);
        var lowerCaseLetters = Enumerable.Range(97, 27).Select(x => (char)x);
        var letters = upperCaseLetters.Concat(lowerCaseLetters);
        var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        var operators = new[] { '|', '.', '*' };
        var alphabet = letters.Concat(digits).Concat(operators).Concat(new[] { '(', ')' }).ToHashSet();

        var symbolFsa = FsaBuilder.FromSymbolSet(letters.Concat(digits));
        var operatorFsa = FsaBuilder.FromSymbolSet(operators);
        var lParenFsa = FsaBuilder.FromSymbolSet(new[] { '(' });
        var rParenFsa = FsaBuilder.FromSymbolSet(new[] { ')' });

        var insertSymbolBoundary = FstBuilder.FromWordPair(string.Empty, $"<SYMBOL>{tokenBoundary}");
        var insertOperatorBoundary = FstBuilder.FromWordPair(string.Empty, $"<OP>{tokenBoundary}");
        var insertLParenBoundary = FstBuilder.FromWordPair(string.Empty, $"<LPAREN>{tokenBoundary}");
        var insertRParenBoundary = FstBuilder.FromWordPair(string.Empty, $"<RPAREN>{tokenBoundary}");

        return symbolFsa
            .Identity()
            .Concat(insertSymbolBoundary)
            .Union(
                operatorFsa.Identity().Concat(insertOperatorBoundary),
                lParenFsa.Identity().Concat(insertLParenBoundary),
                rParenFsa.Identity().Concat(insertRParenBoundary))
            .ToLmlRewriter(alphabet)
            .ToBimachine(alphabet);
    }

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

        Console.WriteLine("Constructing the \"rise case\" transducer.");
        
        var riseCase = alphabet
            .Select(symbol =>
                FstBuilder.FromWordPair(
                    symbol.ToString(),
                    char.IsLower(symbol)
                        ? symbol.ToString().ToUpper()
                        : symbol.ToString()))
            .Aggregate((aggr, fst) => aggr.Union(fst))
            .Star();

        Console.WriteLine("Constructing the \"multi word expression list\" transducer.");
        
        var multiWordExprList = new[] { "AT LEAST", "IN SPITE OF", "HEAD OVER HEELS" };
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
                FsaBuilder.FromSymbolSet(whitespaces)
                .Plus()
                .Product(FsaBuilder.FromWord(" "))
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
        
        return fst.ToBimachine(alphabet);
    }
}