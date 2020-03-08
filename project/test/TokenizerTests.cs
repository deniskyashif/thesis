using Xunit;

public class TokenizerTests
{
    [Fact]
    public void ArithmeticExprTokenizerTest()
    {
        var t = Tokenizers.CreateForArithmeticExpr();

        Assert.Equal("1<INT>\n+<OP>\n1<INT>\n", t.Process("1 + 1"));
        Assert.Equal(
            "10<INT>\n/<OP>\n1<INT>\n-<OP>\n3<INT>\n*<OP>\n1193<INT>\n", 
            t.Process("10 / 1 - 3 * 1193"));
        Assert.Equal("1<INT>\n+<OP>\n+<OP>\n-<OP>\n", t.Process("1+ +-"));
    }
}