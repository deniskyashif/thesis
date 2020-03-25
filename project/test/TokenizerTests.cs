using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class TokenizerTests
{
    [Fact]
    public void ArithmeticExprTokenizerTest()
    {
        var grammar = new[]
        {
            new Rule("[0-9]+", "INT"),
            new Rule("[0-9]+\\.[0-9]+", "FLOAT"),
            new Rule("[+\\-*/]", "OP"),
        };
        var lexer = new Lexer(grammar);

        var tokens1 = lexer.GetNextToken("1+1").ToList();
        var expectedTokens1 = new[]
        {
            new Lexeme { Type = "INT", Text = "1", Index = 0, Position = (0, 0)},
            new Lexeme { Type = "OP", Text = "+", Index = 1, Position = (1, 1)},
            new Lexeme { Type = "INT", Text = "1", Index = 2, Position = (2, 2)},
        };

        Assert.True(TokensEqual(expectedTokens1, tokens1));

        var tokens2 = lexer.GetNextToken("10/1-3*0.1193").ToList();
        var expectedTokens2 = new[]
        {
            new Lexeme { Type = "INT", Text = "10", Index = 0, Position = (0, 1)},
            new Lexeme { Type = "OP", Text = "/", Index = 1, Position = (2, 2)},
            new Lexeme { Type = "INT", Text = "1", Index = 2, Position = (3, 3)},
            new Lexeme { Type = "OP", Text = "-", Index = 3, Position = (4, 4)},
            new Lexeme { Type = "INT", Text = "3", Index = 4, Position = (5, 5)},
            new Lexeme { Type = "OP", Text = "*", Index = 5, Position = (6, 6)},
            new Lexeme { Type = "FLOAT", Text = "0.1193", Index = 6, Position = (7, 12)},
        };

        Assert.True(TokensEqual(expectedTokens2, tokens2));
        Assert.Throws<ArgumentException>(() => lexer.GetNextToken("123 + x").ToList());
    }

    [Fact]
    public void JsonLexerTest()
    {
        var intRe = "0|[1-9][0-9]*";
        var expRe = $"[Ee][+\\-]?({intRe})";
        var hexRe = "[0-9a-fA-F]";
        var unicodeRe = $"u{hexRe}{hexRe}{hexRe}{hexRe}";
        var escRe = "\\\\([\"/bfnrt]|" + unicodeRe + ")";
        var safeCodepointRe = "[^\"\u0000-\u001F\\\\]";

        var grammar = new[]
        {
            new Rule(@"\{", "OBJ_START"),
            new Rule(@"\}", "OBJ_END"),
            new Rule(@"\[", "ARR_START"),
            new Rule(@"\]", "ARR_END"),
            new Rule(":", "PAIR_DELIMITER"),
            new Rule(",", "COMMA"),
            new Rule($"\"({escRe}|{safeCodepointRe})*\"", "STRING"),
            new Rule($"-?({intRe})(\\.[0-9]+)?({expRe})?", "NUMBER"),
            new Rule("true|false", "BOOLEAN"),
            new Rule("[ \t\n\r]+", "WS"),
        };
        var lexer = new Lexer(grammar);
        
        var tokens = lexer.GetNextToken("{\"ab\":false,\"c\":-4.3,\"ww\":[{},\"\"]").ToList();
        var expectedTokens = new[]
        {
            new Lexeme { Type = "OBJ_START", Text = "{", Index = 0, Position = (0, 0)},
            new Lexeme { Type = "STRING", Text = "\"ab\"", Index = 1, Position = (1, 4)},
            new Lexeme { Type = "PAIR_DELIMITER", Text = ":", Index = 2, Position = (5, 5)},
            new Lexeme { Type = "BOOLEAN", Text = "false", Index = 3, Position = (6, 10)},
            new Lexeme { Type = "COMMA", Text = ",", Index = 4, Position = (11, 11)},
            new Lexeme { Type = "STRING", Text = "\"c\"", Index = 5, Position = (12, 14)},
            new Lexeme { Type = "PAIR_DELIMITER", Text = ":", Index = 6, Position = (15, 15)},
            new Lexeme { Type = "NUMBER", Text = "-4.3", Index = 7, Position = (16, 19)},
            new Lexeme { Type = "COMMA", Text = ",", Index = 8, Position = (20, 20)},
            new Lexeme { Type = "STRING", Text = "\"ww\"", Index = 9, Position = (21, 24)},
            new Lexeme { Type = "PAIR_DELIMITER", Text = ":", Index = 10, Position = (25, 25)},
            new Lexeme { Type = "ARR_START", Text = "[", Index = 11, Position = (26, 26)},
            new Lexeme { Type = "OBJ_START", Text = "{", Index = 12, Position = (27, 27)},
            new Lexeme { Type = "OBJ_END", Text = "}", Index = 13, Position = (28, 28)},
            new Lexeme { Type = "COMMA", Text = ",", Index = 14, Position = (29, 29)},
            new Lexeme { Type = "STRING", Text = "\"\"", Index = 15, Position = (30, 31)},
            new Lexeme { Type = "ARR_END", Text = "]", Index = 16, Position = (32, 32)},
        };
        
        Assert.True(TokensEqual(expectedTokens, tokens));
    }

    [Fact]
    public void RegExpTokenizerTest()
    {
        var grammar = new[]
        {
            new Rule("[a-zA-Z0-9]", "SYMBOL"),
            new Rule("\\(", "LPAREN"),
            new Rule("\\)", "RPAREN"),
            new Rule("[|.*]", "OP"),
        };
        var lex = new Lexer(grammar);

        var tokens1 = lex.GetNextToken("a").ToList();
        var expectedTokens1 = new[]
        {
            new Lexeme { Type = "SYMBOL", Text = "a", Index = 0, Position = (0, 0)},
        };

        Assert.True(TokensEqual(expectedTokens1, tokens1));

        var tokens2 = lex.GetNextToken("(a|1)*").ToList();
        var expectedTokens2 = new[]
        {
            new Lexeme { Type = "LPAREN", Text = "(", Index = 0, Position = (0, 0)},
            new Lexeme { Type = "SYMBOL", Text = "a", Index = 1, Position = (1, 1)},
            new Lexeme { Type = "OP", Text = "|", Index = 2, Position = (2, 2)},
            new Lexeme { Type = "SYMBOL", Text = "1", Index = 3, Position = (3, 3)},
            new Lexeme { Type = "RPAREN", Text = ")", Index = 4, Position = (4, 4)},
            new Lexeme { Type = "OP", Text = "*", Index = 5, Position = (5, 5)},
        };

        Assert.True(TokensEqual(expectedTokens2, tokens2));
        Assert.Throws<ArgumentException>(() => lex.GetNextToken("a/b").ToList());
    }

    [Fact(Skip = "Reenable after rewrite")]
    public void EnglishTokenizerTest()
    {
        var t = RewriteTokenizers.CreateForEnglish();

        Assert.Equal("They\nwon\nat least\n'\n10\n'\ntimes\n.\n", t.Process("They won at least '10' times."));
        Assert.Equal("HEAD over hEelS\n", t.Process("HEAD over hEelS"));
        Assert.Equal("Not\nto\n,\n,\nworry\n", t.Process("Not to,, worry"));
    }

    private bool TokensEqual(IList<Lexeme> first, IList<Lexeme> second)
    {
        if (first.Count != second.Count) return false;
        
        for (int i = 0; i < first.Count; i++)
        {
            if (first[i].Index != second[i].Index) return false;
            if (first[i].Position != second[i].Position) return false;
            if (first[i].Text != second[i].Text) return false;
            if (first[i].Type != second[i].Type) return false;
        }

        return true;
    }
}
