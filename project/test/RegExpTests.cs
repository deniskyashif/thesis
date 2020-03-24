using Xunit;

public class RegExpTests
{
    [Fact]
    public void FromPatternShouldMatchCorrectly()
    {
        var re = new RegExp("a*b");

        Assert.True(re.Match("aaaaaab"));
        Assert.True(re.Match("b"));
        Assert.False(re.Match("aa"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly1()
    {
        var re = new RegExp(string.Empty);

        Assert.True(re.Match(string.Empty));
        Assert.False(re.Match("a"));
        Assert.False(re.Match(" ab"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly2()
    {
        var re = new RegExp("a");

        Assert.False(re.Match(string.Empty));
        Assert.True(re.Match("a"));
        Assert.False(re.Match("aaa"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly3()
    {
        var re = new RegExp("a*");

        Assert.True(re.Match(string.Empty));
        Assert.True(re.Match("a"));
        Assert.True(re.Match("aaaaa"));
        Assert.False(re.Match("aba"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly4()
    {
        var re = new RegExp("(0|(1(01*(00)*0)*1)*)*");

        Assert.True(re.Match(string.Empty));
        Assert.True(re.Match("0"));
        Assert.True(re.Match("00"));
        Assert.True(re.Match("11"));
        Assert.True(re.Match("000"));
        Assert.True(re.Match("011"));
        Assert.True(re.Match("110"));
        Assert.True(re.Match("0000"));
        Assert.True(re.Match("0011"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly5()
    {
        var re = new RegExp("(a|b)*c");

        Assert.True(re.Match("c"));
        Assert.True(re.Match("ac"));
        Assert.True(re.Match("ababc"));
        Assert.True(re.Match("bbbc"));
        Assert.True(re.Match("aaaaaaac"));
        Assert.True(re.Match("ac"));
        Assert.True(re.Match("bac"));
        Assert.True(re.Match("abbbbc"));

        Assert.False(re.Match("cc"));
        Assert.False(re.Match("a"));
        Assert.False(re.Match("b"));
        Assert.False(re.Match("ababab"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly6()
    {
        var re = new RegExp("abc|def");

        Assert.True(re.Match("abc"));
        Assert.True(re.Match("def"));
        Assert.False(re.Match("ab"));
        Assert.False(re.Match("ef"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly7()
    {
        var re = new RegExp("a(b*|c)");

        Assert.True(re.Match("ac"));
        Assert.True(re.Match("abbbb"));
        Assert.True(re.Match("ab"));
        Assert.True(re.Match("a"));

        Assert.False(re.Match("abc"));
        Assert.False(re.Match("acc"));
        Assert.False(re.Match(string.Empty));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly8()
    {
        var re = new RegExp("(ab|e)+c?dd");

        Assert.True(re.Match("abeeabdd"));
        Assert.True(re.Match("eecdd"));
        Assert.True(re.Match("eabdd"));
        Assert.True(re.Match("ecdd"));

        Assert.False(re.Match("eabddd"));
        Assert.False(re.Match("eccdd"));
        Assert.False(re.Match(string.Empty));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly9()
    {
        var re = new RegExp("(π|©)ю+_¡˚\\*");

        Assert.True(re.Match("©ю_¡˚*"));
        Assert.True(re.Match("πюююююю_¡˚*"));
        Assert.False(re.Match("π_¡˚*"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly10()
    {
        var re = new RegExp("a.*\\.");

        Assert.True(re.Match("aю_¡˚*."));
        Assert.True(re.Match("abab_dsdqdwqd."));
        Assert.True(re.Match("a."));
        Assert.False(re.Match("baa."));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly11()
    {
        var re = new RegExp(".+@.+\\.(com|net|org)");

        Assert.True(re.Match("john@yahoo.com"));
        Assert.True(re.Match("abab_dsdq&&dd@gmail.org"));
        Assert.True(re.Match("++@()().net"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly12()
    {
        var re = new RegExp("[1-9][0-9]*\\.[0-9]+");

        Assert.True(re.Match("1.0"));
        Assert.True(re.Match("12300.0"));
        Assert.True(re.Match("99.999"));
        Assert.False(re.Match("01.1"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly13()
    {
        var re = new RegExp("[a-z<;A-Z0-9,#\\-]+|_");

        Assert.True(re.Match("1a<<<AccAER,ER-34#"));
        Assert.True(re.Match("a"));
        Assert.True(re.Match("B;;--;;;BBBBB99"));
        Assert.True(re.Match("_"));
        Assert.False(re.Match("_a"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly14()
    {
        var re = new RegExp("a{0,2}b");

        Assert.True(re.Match("b"));
        Assert.True(re.Match("ab"));
        Assert.True(re.Match("aab"));
        Assert.False(re.Match("aaab"));
        Assert.False(re.Match("abb"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly15()
    {
        var re = new RegExp("a{0,2},[a-z]{2,11},_{2,}");

        Assert.True(re.Match("a,bz,__"));
        Assert.True(re.Match(",bzadwqve,____"));
        Assert.False(re.Match("aaa,ppp,__"));
        Assert.False(re.Match("a,www,_"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly16()
    {
        var re = new RegExp("[----]");

        Assert.True(re.Match("-"));
        Assert.False(re.Match(string.Empty));
    }

    [Fact(Skip="Fix character range parsing.")]
    public void FromPatternShouldMatchCorrectly17()
    {
        var re = new RegExp("[a-zA-]+");

        Assert.True(re.Match("-"));
        Assert.True(re.Match("-aAa-"));
        Assert.True(re.Match("--"));
        Assert.False(re.Match(string.Empty));
    }
}