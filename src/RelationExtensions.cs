using System.Collections.Generic;
using System.Linq;

public static class StateExtensions
{
    public static ISet<(int, int)> TransitiveClosure(this ISet<(int, int)> rel)
    {
        var currentRel = rel;
        var nextRel = currentRel.Union(rel.Compose(currentRel)).ToHashSet();

        while (currentRel.Count() < nextRel.Count)
        {
            currentRel = nextRel;
            nextRel = rel.Union(rel.Compose(currentRel)).ToHashSet();
        }

        return currentRel;
    }

    public static IEnumerable<(int, int)> Compose(
        this IEnumerable<(int, int)> r1,
        IEnumerable<(int, int)> r2)
    {
        return r1
            .SelectMany(
                t1 => r2
                    .Where(t2 => t2.Item1 == t1.Item2)
                    .Select(t2 => (t1.Item1, t2.Item2)));
    }
}