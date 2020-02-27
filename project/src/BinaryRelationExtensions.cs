using System.Collections.Generic;
using System.Linq;

public static class BinaryRelationExtensions
{
    public static ISet<(int, int)> TransitiveClosure(this ISet<(int, int)> rel)
    {
        var currentRel = rel;
        var nextRel = currentRel.Union(rel.Compose(currentRel)).ToHashSet();

        while (currentRel.Count < nextRel.Count)
        {
            currentRel = nextRel;
            nextRel = rel.Union(rel.Compose(currentRel)).ToHashSet();
        }

        return currentRel;
    }

    public static IEnumerable<(int, int)> Compose(
        this ISet<(int, int)> rel1, ISet<(int, int)> rel2) 
        => rel1.SelectMany(
            pair1 => rel2
                .Where(pair2 => pair2.Item1 == pair1.Item2)
                .Select(pair2 => (pair1.Item1, pair2.Item2)));
}