using System;
using System.Collections.Generic;
using System.Linq;

public static class PfsaOperations
{
    static int NewState(IReadOnlyCollection<int> states) => states.Count;

    static IEnumerable<int> KNewStates(int k, IReadOnlyCollection<int> states) =>
        Enumerable.Range(states.Count, k);

    // Clones the finite automaton by renaming the states
    static Pfsa Remap(this Pfsa automaton, IReadOnlyCollection<int> states)
    {
        var k = states.Count;

        return new Pfsa(
            automaton.States.Select(s => s + k),
            automaton.Initial.Select(s => s + k),
            automaton.Final.Select(s => s + k),
            automaton.Transitions.Select(t => (t.From + k, t.Label, t.To + k)));
    }

    public static Pfsa Concat(this Pfsa first, Pfsa second)
    {
        var firstFinalStates = first.Final;
        second = Remap(second, first.States);
        var secondInitialStates = second.Initial;

        var initialStates = first.Initial.Intersect(first.Final).Any()
            ? first.Initial.Union(second.Initial)
            : first.Initial;

        var transitions = first.Transitions.Union(second.Transitions).ToList();

        foreach (var tr in first.Transitions.Where(t => first.Final.Contains(t.To)))
            foreach (var state in second.Initial)
                transitions.Add((tr.From, tr.Label, state));

        return new Pfsa(
            states: first.States.Union(second.States),
            initialStates,
            second.Final,
            transitions);
    }

    public static Pfsa Concat(this Pfsa fsa, params Pfsa[] automata) =>
        automata.Aggregate(fsa, Concat);

    public static Pfsa Union(this Pfsa first, Pfsa second)
    {
        second = Remap(second, first.States);

        return new Pfsa(
            states: first.States.Union(second.States),
            initial: first.Initial.Union(second.Initial),
            final: first.Final.Union(second.Final),
            transitions: first.Transitions.Union(second.Transitions));
    }

    public static Pfsa Union(this Pfsa fsa, params Pfsa[] automata) =>
        automata.Aggregate(fsa, Union);

    // Kleene star operation on a finite automaton
    public static Pfsa Star(this Pfsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, Range, int)>();

        foreach (var state in automaton.Initial)
            newTransitions.Add((initial, default, state));

        foreach (var state in automaton.Final)
            newTransitions.Add((state, default, initial));

        return new Pfsa(
            states: automaton.States.Union(initialStates),
            initialStates,
            automaton.Final.Union(initialStates),
            automaton.Transitions.Union(newTransitions));
    }

    public static Pfsa Plus(this Pfsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, Range, int)>();

        foreach (var state in automaton.Initial)
            newTransitions.Add((initial, default, state));

        foreach (var state in automaton.Final)
            newTransitions.Add((state, default, initial));

        return new Pfsa(
            automaton.States.Union(initialStates),
            initialStates,
            automaton.Final,
            automaton.Transitions.Union(newTransitions));
    }

    public static Pfsa Optional(this Pfsa automaton)
    {
        var state = new[] { NewState(automaton.States) };

        return new Pfsa(
            automaton.States.Union(state),
            automaton.Initial.Union(state),
            automaton.Final.Union(state),
            automaton.Transitions);
    }

    public static Pfsa EpsilonFree(this Pfsa automaton)
    {
        var initial = automaton.Initial.SelectMany(automaton.EpsilonClosure);

        var transitions = automaton.Transitions
            .Where(t => t.Label != null)
            .SelectMany(t =>
                automaton
                    .EpsilonClosure(t.To)
                    .Select(es => (t.From, t.Label, es)));

        return new Pfsa(automaton.States, initial, automaton.Final, transitions);
    }

    public static Pfsa Trim(this Pfsa automaton)
    {
        var transitiveClosure = automaton.Transitions
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure();

        var reachableFromInitial = automaton.Initial
            .Union(transitiveClosure
                .Where(x => automaton.Initial.Contains(x.Item1))
                .Select(x => x.Item2));
        var leadingToFinal = automaton.Final
            .Union(transitiveClosure
                .Where(x => automaton.Final.Contains(x.Item2))
                .Select(x => x.Item1));
        var states = reachableFromInitial.Intersect(leadingToFinal).ToList();

        var transitions = automaton.Transitions
            .Where(t => states.Contains(t.From) && states.Contains(t.To))
            .Select(t => (
                states.IndexOf(t.From),
                t.Label,
                states.IndexOf(t.To)));

        var newInitial = states.Intersect(automaton.Initial);
        var newFinal = states.Intersect(automaton.Final);

        return new Pfsa(
            states.Select(s => states.IndexOf(s)),
            newInitial.Select(s => states.IndexOf(s)),
            newFinal.Select(s => states.IndexOf(s)),
            transitions);
    }

    public static Pfsa Intersect(this Pfsa first, Pfsa second)
    {
        var firstEpsFree = first.EpsilonFree();
        var secondEpsFree = second.EpsilonFree();

        var product = Product(firstEpsFree, secondEpsFree);

        var states = Enumerable.Range(0, product.States.Count);
        var final = states
            .Where(s =>
                first.Final.Contains(product.States[s].Item1) &&
                second.Final.Contains(product.States[s].Item2));

        return new Pfsa(states, new[] { 0 }, final, product.Transitions).Trim();
    }

    public static (IList<(int, int)> States, IList<(int, Range, int)> Transitions)
        Product(Pfsa first, Pfsa second)
    {
        var firstTransPerState = first.Transitions
            .GroupBy(t => t.From, t => (t.Label, t.To))
            .ToDictionary(g => g.Key, g => g.ToList());

        var secondTransPerState = second.Transitions
            .GroupBy(t => t.From, t => (t.Label, t.To))
            .ToDictionary(g => g.Key, g => g.ToList());

        var productStates = new List<(int, int)>();

        foreach (var i1 in first.Initial)
            foreach (var i2 in second.Initial)
                productStates.Add((i1, i2));

        var transitions = new List<(int, Range, int)>();

        for (var n = 0; n < productStates.Count; n++)
        {
            var (p1, p2) = productStates[n];
            var departingTransitions = new List<(Range Label, (int, int) To)>();

            if (firstTransPerState.ContainsKey(p1) && secondTransPerState.ContainsKey(p2))
            {
                foreach (var t1 in firstTransPerState[p1])
                {
                    foreach (var t2 in secondTransPerState[p2])
                    {
                        var @int = t1.Label.Intersect(t2.Label);
                        if (@int != null)
                            departingTransitions.Add((@int, (t1.To, t2.To)));
                    }
                }
                        
            }

            foreach (var t in departingTransitions)
                if (!productStates.Contains(t.To))
                    productStates.Add(t.To);

            // Transitions refer to states by their index in the states list
            foreach (var pair in departingTransitions)
                transitions.Add((n, pair.Label, productStates.IndexOf(pair.To)));
        }

        return (productStates, transitions);
    }

    public static Pdfsa Determinize(this Pfsa automaton)
    {
        var fsa = automaton.EpsilonFree();
        return null;
    }

    public static Pfsa Difference(this Pfsa automaton)
    {
        return null;
    }
}