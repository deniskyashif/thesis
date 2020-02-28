using System.Linq;
using Xunit;

public class FstTests
{
    [Fact]
    public void FstTransduceTest()
    {
        var states = new[] { 0, 1, 2, 3 };
        var initial = new[] { 0 };
        var final = new[] { 2, 3 };
        var transitions = new[]
        {
            (0, "a", "x", 1),
            (0, "a", "y", 2),
            (1, "b", "y", 3),
            (2, "c", "z", 2),
        };
        var fst = new Fst(states, initial, final, transitions);

        Assert.Equal("xy", fst.Transduce(new[] { "a", "b" }).Single());
        Assert.Equal("y", fst.Transduce(new[] { "a" }).Single());
        Assert.Equal("yzzzz", fst.Transduce(new[] { "a", "c", "c", "c", "c" }).Single());
    }

    [Fact]
    public void FstWithEpsilonTransduceTest()
    {
        var states = new[] { 0, 1, 2, 3 };
        var initial = new[] { 0 };
        var final = new[] { 2, 3 };
        var transitions = new[]
        {
            (0, "a", "x", 1),
            (0, "a", "y", 2),
            (1, "b", "y", 3),
            (2, "c", "z", 2),
            (2, "", "E", 1),
        };
        var fst = new Fst(states, initial, final, transitions);

        var res1 = fst.Transduce(new[] { "a", "b" });
        Assert.Equal(2, res1.Count);
        Assert.Contains("xy", res1);
        Assert.Contains("yEy", res1);

        Assert.Equal("y", fst.Transduce(new[] { "a" }).Single());
        Assert.Equal("yzzzz", fst.Transduce(new[] { "a", "c", "c", "c", "c" }).Single());
        Assert.Equal("yzzzEy", fst.Transduce(new[] { "a", "c", "c", "c", "b" }).Single());
    }
}