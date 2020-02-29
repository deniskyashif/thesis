using System.Collections.Generic;
using System.Linq;
using Xunit;

public class RelationExtensionsTests
{
    [Fact]
    public void TransitiveClosureTest()
    {
        var rel = new HashSet<(int, int)> { (1, 2), (2, 3), (4, 5) };
        var actual = rel.TransitiveClosure().OrderBy(x => x.Item1).ThenBy(x => x.Item2);
        var expected = new[] { (1, 2), (2, 3), (4, 5), (1, 3) }.OrderBy(x => x.Item1).ThenBy(x => x.Item2);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TransitiveClosureTest1()
    {
        var rel = new HashSet<(int, int)> { (1, 2), (2, 3), (3, 4), (4, 5) };
        var actual = rel.TransitiveClosure().OrderBy(x => x.Item1).ThenBy(x => x.Item2);
        var expected = new[]
        {
            (1, 2), (2, 3), (3, 4), (4, 5), (1, 3), (2, 4), (3, 5), (1, 4), (2, 5), (1, 5)
        }.OrderBy(x => x.Item1).ThenBy(x => x.Item2);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TransitiveClosureTest2()
    {
        var rel = new HashSet<(int, int)> { (0, 0), (0, 1), (1, 2) };
        var actual = rel.TransitiveClosure().OrderBy(x => x.Item1).ThenBy(x => x.Item2);
        var expected = new[]
        {
            (0, 0), (0, 1), (0, 2), (1, 2)
        }.OrderBy(x => x.Item1).ThenBy(x => x.Item2);

        Assert.Equal(expected, actual);
    }
}