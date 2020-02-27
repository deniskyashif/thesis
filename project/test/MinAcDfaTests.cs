using Xunit;

public class MinAcDfaTests
{
    [Fact]
    void MinAcDfaConstructionTest()
    {
        var dfa = DfaBuilder.ConstructMinAcyclicDFA(new[] 
        {
            "appl", "bapp", "cppe", "cppee", "x"
        });

        Assert.True(dfa.Recognize("appl"));
        Assert.True(dfa.Recognize("bapp"));
        Assert.True(dfa.Recognize("cppe"));
        Assert.True(dfa.Recognize("cppee"));
        Assert.True(dfa.Recognize("x"));
        Assert.False(dfa.Recognize("e"));
        Assert.False(dfa.Recognize("ex"));
        Assert.False(dfa.Recognize("cpp"));
    }
}