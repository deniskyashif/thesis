using System;
using Xunit;

public class TokenizerTests
{
    [Fact]
    public void ArithmeticExprTokenizerTest()
    {
        var t = Tokenizers.CreateForArithmeticExpr();

        Assert.Equal("1<INT>\n+<OP>\n1<INT>\n", t.Process("1 + 1"));
        Assert.Equal("1.2<FLOAT>\n", t.Process("1.2"));
        Assert.Equal(
            "10<INT>\n/<OP>\n1<INT>\n-<OP>\n3<INT>\n*<OP>\n0.1193<FLOAT>\n",
            t.Process("10 / 1 - 3 * 0.1193"));
        Assert.Equal("1<INT>\n+<OP>\n+<OP>\n-<OP>\n", t.Process("1+ +-"));
        Assert.Throws<ArgumentException>(() => t.Process("123 + x"));
    }

    [Fact]
    public void RegExpTokenizerTest()
    {
        var t = Tokenizers.CreateForRegularExpr();

        Assert.Equal("a<SYMBOL>\n", t.Process("a"));
        Assert.Equal("a<SYMBOL>\n.<OP>\nb<SYMBOL>\n", t.Process("a.b"));
        Assert.Equal(
            "(<LPAREN>\na<SYMBOL>\n|<OP>\n1<SYMBOL>\n)<RPAREN>\n*<OP>\n", 
            t.Process("(a|1)*"));
        Assert.Throws<ArgumentException>(() => t.Process("a/b"));
    }
}