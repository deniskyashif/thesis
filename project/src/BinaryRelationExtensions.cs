using System.Collections.Generic;
using System.Linq;

public static class BinaryRelationExtensions
{
    public static ICollection<(int, int)> TransitiveClosure(this ISet<(int, int)> rel)
    {
        var domainGroup = rel
            .GroupBy(x => x.Item1, x => x.Item2)
            .ToDictionary(g => g.Key, g => g);
        var transitiveClosure = new List<(int, int)>(rel);

        for (int n = 0; n < transitiveClosure.Count; n++)
        {
            var current = transitiveClosure[n];

            if (domainGroup.ContainsKey(current.Item2))
            {
                foreach (var to in domainGroup[current.Item2])
                {
                    if (!transitiveClosure.Contains((current.Item1, to)))
                        transitiveClosure.Add((current.Item1, to));
                }
            }
        }

        return transitiveClosure;
    }
}