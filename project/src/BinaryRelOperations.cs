using System;
using System.Collections.Generic;
using System.Linq;

public static class RelationOperations
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

    /* Splits a set of states to equivalence classes based on a custom 
        equivalence class selector function. Used for Dfsa & Fst minimization. */
    internal static Dictionary<int, int> Kernel(IEnumerable<int> states, Func<int, int> eqClassSelector)
    {
        var eqClasses = states.Select(s => eqClassSelector(s)).Distinct().ToList();

        return states
            .Select(s => (State: s, Class: eqClasses.IndexOf(eqClassSelector(s))))
            .ToDictionary(p => p.State, p => p.Class);
    }

    // Intersects two equivalence relations. Used for Dfsa & Fst minimization.
    internal static Dictionary<int, int> IntersectEqRel(
        IEnumerable<int> states, 
        IDictionary<int, int> eqRel1, 
        IDictionary<int, int> eqRel2)
    {
        var eqClassPairs = states.Select(s => (eqRel1[s], eqRel2[s])).Distinct().ToList();

        return states
            .Select(s => (State: s, Class: eqClassPairs.IndexOf((eqRel1[s], eqRel2[s]))))
            .ToDictionary(p => p.State, p => p.Class);
    }
}