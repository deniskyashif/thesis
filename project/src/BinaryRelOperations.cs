using System.Collections.Generic;
using System.Linq;

public static class BinaryRelationOperations
{
    public static ICollection<(int, int)> TransitiveClosure(this ISet<(int, int)> rel)
    {
        var groupedByA = rel
            .GroupBy(x => x.Item1, x => x.Item2)
            .ToDictionary(g => g.Key, g => g);

        var transitiveClosure = new List<(int, int)>(rel);
        var examined = new HashSet<(int, int)>(rel);

        for (int n = 0; n < transitiveClosure.Count; n++)
        {
            var (a, b) = transitiveClosure[n];

            if (groupedByA.ContainsKey(b))
            {
                foreach (var c in groupedByA[b])
                {
                    if (!examined.Contains((a, c)))
                    {
                        transitiveClosure.Add((a, c));
                        examined.Add((a, c));
                    }
                }
            }
        }

        return transitiveClosure;
    }
}