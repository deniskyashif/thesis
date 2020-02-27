using System.Collections.Generic;
using System.Linq;
using Xunit;

public class RelationExtensionsTests
{
    [Fact]
    public void ComposeBinaryRelTest()
    {
        var rel1 = new HashSet<(int, int)> { (1, 2), (2, 3), (4, 5) };
        var rel2 = new HashSet<(int, int)> { (2, 9), (5, 5) };
        var expected = new HashSet<(int, int)> { (1, 9), (4, 5) };

        Assert.Equal(expected, rel1.Compose(rel2));
    }

    [Fact]
    public void ComposeBinaryRelTest1()
    {
        var rel1 = new HashSet<(int, int)> { (2, 1), (3, 3) };
        var rel2 = new HashSet<(int, int)> { (2, 9), (5, 5) };

        Assert.Empty(rel1.Compose(rel2));
    }

    [Fact]
    public void ComposeBinaryRelTest2()
    {
        var rel1 = new HashSet<(int, int)> { (2, 1), (3, 3) };
        var rel2 = new HashSet<(int, int)> { (1, 9), (5, 5) };

        Assert.Equal(new[] { (2, 9) }, rel1.Compose(rel2));
    }

    [Fact]
    public void TransitiveClosureTest()
    {
        var r = new HashSet<(int, int)> { (1, 2), (2, 3), (4, 5) };
        var actual = r.TransitiveClosure().OrderBy(x => x.Item1).ThenBy(x => x.Item2);
        var expected = new[] { (1, 2), (2, 3), (4, 5), (1, 3) }
            .OrderBy(x => x.Item1)
            .ThenBy(x => x.Item2);

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