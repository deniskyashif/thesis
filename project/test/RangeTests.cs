using Xunit;

public class RangeTests
{
    [Theory]
    [InlineData('a', 'd', 'c', 'f', 'c', 'd')]
    [InlineData('a', 'z', 'c', 'f', 'c', 'f')]
    [InlineData('a', 'd', 'd', 'z', 'd', 'd')]
    [InlineData('c', 'f', 'a', 'd', 'c', 'd')]
    [InlineData('a', 'f', 'f', 'f', 'f', 'f')]
    [InlineData('b', 'f', 'a', 'z', 'b', 'f')]
    [InlineData('a', 'z', 'a', 'z', 'a', 'z')]
    public void IntersectRangeTest(char r1x, char r1y, char r2x, char r2y, char x, char y)
    {
        var r1 = new Range(r1x, r1y);
        var r2 = new Range(r2x, r2y);

        Assert.Equal(new Range(x, y), r1.Intersect(r2));
    }

    [Theory]
    [InlineData('a', 'a', 'b', 'b')]
    [InlineData('a', 'd', 'e', 'f')]
    [InlineData('e', 'k', 'a', 'b')]
    [InlineData('x', 'x', 'y', 'z')]
    public void IntersectDisjointRangeTest(char r1x, char r1y, char r2x, char r2y)
    {
        var r1 = new Range(r1x, r1y);
        var r2 = new Range(r2x, r2y);

        Assert.Null(r1.Intersect(r2));
    }
}