using Xunit;

public class RegExp2Tests
{
    [Fact]
    public void FromPatternShouldMatchCorrectly()
    {
        var re = new RegExp2("a*b");

        Assert.True(re.Match("aaaaaab"));
        Assert.True(re.Match("b"));
        Assert.False(re.Match("aa"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly1()
    {
        var re = new RegExp2(string.Empty);

        Assert.True(re.Match(string.Empty));
        Assert.False(re.Match("a"));
        Assert.False(re.Match(" ab"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly2()
    {
        var re = new RegExp2("a");

        Assert.False(re.Match(string.Empty));
        Assert.True(re.Match("a"));
        Assert.False(re.Match("aaa"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly3()
    {
        var re = new RegExp2("a*");

        Assert.True(re.Match(string.Empty));
        Assert.True(re.Match("a"));
        Assert.True(re.Match("aaaaa"));
        Assert.False(re.Match("aba"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly4()
    {
        var re = new RegExp2("(0|(1(01*(00)*0)*1)*)*");

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
        var re = new RegExp2("(a|b)*c");

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
        var re = new RegExp2("abc|def");

        Assert.True(re.Match("abc"));
        Assert.True(re.Match("def"));
        Assert.False(re.Match("ab"));
        Assert.False(re.Match("ef"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly7()
    {
        var re = new RegExp2("a(b*|c)");

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
        var re = new RegExp2("(ab|e)+c?dd");

        Assert.True(re.Match("abeeabdd"));
        Assert.True(re.Match("eecdd"));
        Assert.True(re.Match("eabdd"));
        Assert.True(re.Match("ecdd"));

        Assert.False(re.Match("eabddd"));
        Assert.False(re.Match("eccdd"));
        Assert.False(re.Match(string.Empty));
    }

    public void FromPatternShouldMatchCorrectly9()
    {
        var re = new RegExp2("(π|©)ю+_¡˚\\*");

        Assert.True(re.Match("©ю_¡˚*"));
        Assert.True(re.Match("πюююююю_¡˚*"));
        Assert.False(re.Match("π_¡˚*"));
    }

    public void FromPatternShouldMatchCorrectly10()
    {
        var re = new RegExp2("a.*\\.");

        Assert.True(re.Match("aю_¡˚*."));
        Assert.True(re.Match("abab_dsdqdwqd."));
        Assert.True(re.Match("a."));
        Assert.False(re.Match("baa."));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly11()
    {
        var re = new RegExp2(".+@.+\\.(com|net|org)");

        Assert.True(re.Match("john@yahoo.com"));
        Assert.True(re.Match("abab_dsdq&&dd@gmail.org"));
        Assert.True(re.Match("++@()().net"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly12()
    {
        var re = new RegExp2("[1-9][0-9]*\\.[0-9]+");

        Assert.True(re.Match("1.0"));
        Assert.True(re.Match("12300.0"));
        Assert.True(re.Match("99.999"));
        Assert.False(re.Match("01.1"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly13()
    {
        var re = new RegExp2("[a-z<;A-Z0-9,#\\-]+|_");

        Assert.True(re.Match("1a<<<AccAER,ER-34#"));
        Assert.True(re.Match("a"));
        Assert.True(re.Match("B;;--;;;BBBBB99"));
        Assert.True(re.Match("_"));
        Assert.False(re.Match("_a"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly14()
    {
        var re = new RegExp2("a{0,2}b");

        Assert.True(re.Match("b"));
        Assert.True(re.Match("ab"));
        Assert.True(re.Match("aab"));
        Assert.False(re.Match("aaab"));
        Assert.False(re.Match("abb"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly15()
    {
        var re = new RegExp2("a{0,2},[a-z]{2,11},_{2,}");

        Assert.True(re.Match("a,bz,__"));
        Assert.True(re.Match(",bzadwqve,____"));
        Assert.False(re.Match("aaa,bz,__"));
        Assert.False(re.Match("aaa,ppp,__"));
        Assert.False(re.Match("a,www,_"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly16()
    {
        var re = new RegExp2("a{2,}");
        Assert.True(re.Match("aa"));
        Assert.True(re.Match("aaa"));
        Assert.True(re.Match("aaaaaa"));
        Assert.False(re.Match("a"));

        var re1 = new RegExp2("a{2}");
        Assert.True(re1.Match("aa"));
        Assert.False(re1.Match("a"));
        Assert.False(re1.Match("aaa"));
        Assert.False(re1.Match("aaaa"));

        var re2 = new RegExp2("a{2,4}");
        Assert.True(re2.Match("aa"));
        Assert.True(re2.Match("aaa"));
        Assert.True(re2.Match("aaaa"));
        Assert.False(re2.Match("a"));
        Assert.False(re2.Match("aaaaa"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly17()
    {
        var re = new RegExp2("[----]");

        Assert.True(re.Match("-"));
        Assert.False(re.Match(string.Empty));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly18()
    {
        var re = new RegExp2("[a-zA-]+");

        Assert.True(re.Match("-"));
        Assert.True(re.Match("-aAa-"));
        Assert.True(re.Match("--"));
        Assert.False(re.Match(string.Empty));
    }

    [Fact(Skip="Until handling of negative char sets is implemented.")]
    public void FromPatternShouldMatchCorrectly19()
    {
        var re = new RegExp2("[^abc]");
        Assert.True(re.Match("d"));
        Assert.True(re.Match("e"));
        Assert.False(re.Match("a"));
        Assert.False(re.Match("b"));
        Assert.False(re.Match("c"));

        var re1 = new RegExp2("[^\"\\\\]");
        Assert.True(re1.Match("a"));
        Assert.True(re1.Match("e"));
        Assert.False(re1.Match("\\\\"));
        Assert.False(re1.Match("\""));
    }

    [Fact(Skip="Until handling of negative char sets is implemented.")]
    public void FromPatternShouldMatchCorrectly20()
    {
        var re = new RegExp2("[^0-9?\\-^a-z]+");

        Assert.True(re.Match(" "));
        Assert.True(re.Match("AB()C"));
        Assert.False(re.Match("-"));
        Assert.False(re.Match("923481567"));
        Assert.False(re.Match("vstgko"));
        Assert.False(re.Match("?"));
        Assert.False(re.Match("^"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly21()
    {
        var re = new RegExp2("[\\^0-9]+");

        Assert.True(re.Match("^"));
        Assert.True(re.Match("923481567"));
        Assert.False(re.Match(" "));
        Assert.False(re.Match("AB()C"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly22()
    {
        var re = new RegExp2("[a\\-z]");

        Assert.True(re.Match("a"));
        Assert.True(re.Match("z"));
        Assert.True(re.Match("-"));
        Assert.False(re.Match("v"));
        Assert.False(re.Match("c"));
    }

    [Fact]
    public void FromPatternShouldMatchCorrectly23()
    {
        var re = new RegExp2("[01]*1[01]{5}");
        var fsm = re.Automaton.Determinize(); //.Minimal();

        Assert.True(fsm.Recognize("100000"));
        Assert.True(fsm.Recognize("111001"));
        Assert.True(fsm.Recognize("010101111001"));
        Assert.True(fsm.Recognize("000101110"));
        Assert.False(fsm.Recognize("000000"));
    }
}
