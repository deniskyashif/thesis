using System;
using System.Collections.Generic;
using System.Linq;

public static class BimachineOperations
{
    public static Bimachine PseudoMinimal(this Bimachine bm)
    {
        var leftProfiles = bm.Output
            .GroupBy(
                o => o.Key.Lstate,
                o => (Symbol: o.Key.Symbol, State: o.Key.Rstate, Word: o.Value))
            .ToDictionary(g => g.Key, g => g.ToList());

        var rightProfiles = bm.Output
            .GroupBy(
                o => o.Key.Rstate,
                o => (Symbol: o.Key.Symbol, State: o.Key.Lstate, Word: o.Value))
            .ToDictionary(g => g.Key, g => g.ToList());
        
        var alphabet = bm.Left.Transitions.Select(t => t.Key.Label).Distinct();
        var leftEqRel = FindBmDfaEqRel(bm.Left, leftProfiles, alphabet);
        var rightEqRel = FindBmDfaEqRel(bm.Right, rightProfiles, alphabet);

        var leftMinDfaTrans = bm.Left.Transitions
            .Select(t => (Key: (leftEqRel[t.Key.From], t.Key.Label), Value: leftEqRel[t.Value]))
            .Distinct()
            .ToDictionary(p => p.Key, p => p.Value);

        var leftMinDfa = new Dfsa(
            bm.Left.States.Select(s => leftEqRel[s]),
            leftEqRel[bm.Left.Initial],
            Array.Empty<int>(),
            leftMinDfaTrans);

        var rightMinDfaTrans = bm.Right.Transitions
            .Select(t => (Key: (rightEqRel[t.Key.From], t.Key.Label), Value: rightEqRel[t.Value]))
            .Distinct()
            .ToDictionary(p => p.Key, p => p.Value);

        var rightMinDfa = new Dfsa(
            bm.Right.States.Select(s => rightEqRel[s]),
            rightEqRel[bm.Right.Initial],
            Array.Empty<int>(),
            rightMinDfaTrans);

        var minBmOutput = bm.Output
            .Select(x => (Key: (leftEqRel[x.Key.Lstate], x.Key.Symbol, rightEqRel[x.Key.Rstate]), Value: x.Value))
            .Distinct()
            .ToDictionary(p => p.Key, p => p.Value);

        return new Bimachine(leftMinDfa, rightMinDfa, minBmOutput);
    }

    static Dictionary<int, int> FindBmDfaEqRel(
        Dfsa automaton, Dictionary<int, List<(char, int, string)>> profiles, IEnumerable<char> alphabet)
    {
        int EquivClassCount(Dictionary<int, int> eqRel) => eqRel.Values.Distinct().Count();

        var profilesRange = profiles.Values.ToList();
        Func<int, int> initLeftEqClassSelector = state =>
            profiles.ContainsKey(state)
                ? profilesRange.FindIndex(triples =>
                    profiles[state].Count == triples.Count && profiles[state].All(triples.Contains))
                : -1;

        var prevEqClasCount = 0;
        var eqRel = RelationOperations.Kernel(
            automaton.States, initLeftEqClassSelector);

        while (prevEqClasCount < EquivClassCount(eqRel))
        {
            var kernelsPerSymbol = new List<IDictionary<int, int>>();

            foreach (var symbol in alphabet)
            {
                Func<int, int> eqClassSelectorOnSymbol = state =>
                    automaton.Transitions.ContainsKey((state, symbol))
                        ? eqRel[automaton.Transitions[(state, symbol)]]
                        : -1;
                kernelsPerSymbol.Add(
                    RelationOperations.Kernel(automaton.States, eqClassSelectorOnSymbol));
            }

            var nextLeftEqRel = kernelsPerSymbol.Count > 1
                ? RelationOperations.IntersectEqRel(
                    automaton.States, kernelsPerSymbol[0], kernelsPerSymbol[1])
                : kernelsPerSymbol[0];

            for (int i = 2; i < kernelsPerSymbol.Count; i++)
                nextLeftEqRel = RelationOperations.IntersectEqRel(
                    automaton.States, nextLeftEqRel, kernelsPerSymbol[i]);

            prevEqClasCount = EquivClassCount(eqRel);
            eqRel = RelationOperations.IntersectEqRel(automaton.States, eqRel, nextLeftEqRel);
        }

        return eqRel;
    }
}