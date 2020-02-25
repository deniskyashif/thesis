using System.Collections.Generic;
using System.Linq;

public static class StateExtensions
{
    public static ISet<(int, int)> TransitiveClosure(this IEnumerable<(int, int)> rel)
    {
        var currentRel = rel;
        var nextRel = currentRel.Union(rel.Compose(currentRel)).ToList();

        while (currentRel.Count() < nextRel.Count)
        {
            currentRel = nextRel;
            nextRel = rel.Union(rel.Compose(currentRel)).ToList();
        }

        return currentRel.ToHashSet();
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