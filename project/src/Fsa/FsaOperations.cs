using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public static class FsaOperations
{
    static int NewState(IReadOnlyCollection<int> states) => states.Count;

    static IEnumerable<int> KNewStates(int k, IReadOnlyCollection<int> states) =>
        Enumerable.Range(states.Count, k);

    static Fsa Remap(this Fsa automaton, IReadOnlyCollection<int> states)
    {
        var k = states.Count;

        return new Fsa(
            automaton.States.Select(s => s + k),
            automaton.Initial.Select(s => s + k),
            automaton.Final.Select(s => s + k),
            automaton.Transitions.Select(t => (t.From + k, t.Via, t.To + k)));
    }

    public static Fsa Concat(this Fsa first, Fsa second)
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
                transitions.Add((tr.From, tr.Via, state));

        return new Fsa(
            states: first.States.Union(second.States),
            initialStates,
            second.Final,
            transitions);
    }

    public static Fsa Concat(this Fsa fsa, params Fsa[] automata) =>
        automata.Aggregate(fsa, Concat);

    public static Fsa Union(this Fsa first, Fsa second)
    {
        second = Remap(second, first.States);

        return new Fsa(
            states: first.States.Union(second.States),
            initial: first.Initial.Union(second.Initial),
            final: first.Final.Union(second.Final),
            transitions: first.Transitions.Union(second.Transitions));
    }

    public static Fsa Union(this Fsa fsa, params Fsa[] automata) =>
        automata.Aggregate(fsa, Union);

    public static Fsa Star(this Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, string, int)>();

        foreach (var state in automaton.Initial)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.Final)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates),
            initialStates,
            automaton.Final.Union(initialStates),
            automaton.Transitions.Union(newTransitions));
    }

    public static Fsa Plus(this Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, string, int)>();

        foreach (var state in automaton.Initial)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.Final)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            automaton.States.Union(initialStates),
            initialStates,
            automaton.Final,
            automaton.Transitions.Union(newTransitions));
    }

    public static Fsa Option(this Fsa automaton)
    {
        var state = new[] { NewState(automaton.States) };

        return new Fsa(
            automaton.States.Union(state),
            automaton.Initial.Union(state),
            automaton.Final.Union(state),
            automaton.Transitions);
    }

    /* Preserves the automaton's language but 
       does not preserve the language of the individual states */
    public static Fsa EpsilonFree(this Fsa automaton)
    {
        var initial = automaton.Initial.SelectMany(automaton.EpsilonClosure);

        var transitions = automaton.Transitions
            .Where(t => !string.IsNullOrEmpty(t.Via))
            .SelectMany(t =>
                automaton
                    .EpsilonClosure(t.To)
                    .Select(es => (t.From, t.Via, es)));

        return new Fsa(automaton.States, initial, automaton.Final, transitions);
    }

    public static Fsa Trim(this Fsa automaton)
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
        var states = reachableFromInitial.Intersect(leadingToFinal).ToArray();

        var transitions = automaton.Transitions
            .Where(t => states.Contains(t.From) && states.Contains(t.To))
            .Select(t => (
                Array.IndexOf(states, t.From),
                t.Via,
                Array.IndexOf(states, t.To)));

        var newInitial = states.Intersect(automaton.Initial);
        var newFinal = states.Intersect(automaton.Final);

        return new Fsa(
            states.Select(s => Array.IndexOf(states, s)),
            newInitial.Select(s => Array.IndexOf(states, s)),
            newFinal.Select(s => Array.IndexOf(states, s)),
            transitions);
    }

    public static Dfsa Trim(this Dfsa automaton)
    {
        var reachableStates = automaton.Transitions
            .Select(t => (t.Key.From, t.Value))
            .ToHashSet()
            .TransitiveClosure();

        var newStates = new[] { automaton.Initial }
            .Union(reachableStates
                .Where(pair => pair.Item1 == automaton.Initial)
                .Select(pair => pair.Item2))
            .Intersect(automaton.Final
                .Union(reachableStates
                    .Where(pair => automaton.Final.Contains(pair.Item2))
                    .Select(pair => pair.Item1)))
            .ToArray();

        if (newStates.Length == 0)
            return new Dfsa(
                new[] { 1 },
                1,
                Array.Empty<int>(),
                new Dictionary<(int, char), int>());

        // States are renamed to their indices in the newStates array
        var newTransitions = automaton.Transitions
            .Where(t => newStates.Contains(t.Key.From) && newStates.Contains(t.Value))
            .ToDictionary(
                t => (Array.IndexOf(newStates, t.Key.From), t.Key.Via), // key
                t => Array.IndexOf(newStates, t.Value)); // value

        return new Dfsa(
            newStates.Select(s => Array.IndexOf(newStates, s)),
            Array.IndexOf(newStates, automaton.Initial),
            automaton.Final.Intersect(newStates).Select(s => Array.IndexOf(newStates, s)),
            newTransitions);
    }

    public static Fsa Expand(this Fsa automaton)
    {
        var multiSymbolTransitions = automaton.Transitions.Where(t => t.Via.Length > 1);

        var newStates = automaton.States.ToList();
        var newTransitions = automaton.Transitions.ToHashSet();

        foreach (var tr in multiSymbolTransitions)
        {
            var wordLen = tr.Via.Length;
            var intermediateStates = KNewStates(wordLen - 1, newStates);
            var stateSeq = new[] { tr.From }
                .Concat(intermediateStates)
                .Concat(new[] { tr.To })
                .ToArray();

            newStates.AddRange(intermediateStates);
            var path = Enumerable.Range(0, stateSeq.Length - 1)
                    .Select(i => (stateSeq[i], tr.Via[i].ToString(), stateSeq[i + 1]));

            newTransitions.Remove(tr);
            newTransitions.UnionWith(path);
        }

        return new Fsa(newStates, automaton.Initial, automaton.Final, newTransitions);
    }

    public static Dfsa Determinize(this Fsa automaton)
    {
        var fsa = Expand(EpsilonFree(automaton));

        var stateTransitionMap = fsa.Transitions
            .GroupBy(t => t.From, t => (t.Via, t.To))
            .ToDictionary(g => g.Key, g => g.ToArray());

        var subsetStates = new List<int[]> { fsa.Initial.ToArray() };
        var dfsaTransitions = new Dictionary<(int, char), int>();

        for (var n = 0; n < subsetStates.Count; n++) // we break from the loop when there is no unexamined state
        {
            var symbolToStates = subsetStates[n] // take the last unexamined subset state
                .Where(s => stateTransitionMap.ContainsKey(s)) // keep only the items with outgoing transitions
                .SelectMany(s => stateTransitionMap[s]) // flatten into a set of (symbol, target) pairs
                .Distinct()
                .GroupBy(p => p.Via.Single(), p => p.To) // group them by symbol (fsa has only symbol transitions becase of "Expand")
                .ToDictionary(g => g.Key, g => g.ToArray()); // convert to dictionary of type <symbol, set of states>

            foreach (var state in symbolToStates.Select(p => p.Value)) // the newly formed state sets are in the Dfsa
                if (!subsetStates.Any(ss => ss.SequenceEqual(state))) // check if it has been added
                    subsetStates.Add(state);

            foreach (var pair in symbolToStates)
                dfsaTransitions.Add(
                    (n, pair.Key), // n is the index of the currently examined subset state, pair.Key (symbol) is the trans. label
                    subsetStates.FindIndex(ss => ss.SequenceEqual(pair.Value))); // goes to the index of the subset
        }

        // DFA state names are the indices of the state subsets
        var renamedStates = Enumerable.Range(0, subsetStates.Count);

        // if a state subset contains a final state from the original automaton
        // then it is marked as final in the deterministic automaton
        var finalStates = renamedStates
            .Where(index => subsetStates[index].Intersect(fsa.Final).Any())
            .ToArray();

        return new Dfsa(renamedStates, 0, finalStates, dfsaTransitions);
    }

    public static Fst Product(this Fsa first, Fsa second)
    {
        var firstTransWithEpsilon = first.Transitions.Union(
            first.States.Select(s => (From: s, Via: string.Empty, To: s)));
        var secondTransWithEpsilon = second.Transitions.Union(
            second.States.Select(s => (From: s, Via: string.Empty, To: s)));

        var firstTransitionsPerState = firstTransWithEpsilon
            .GroupBy(t => t.From)
            .ToDictionary(g => g.Key, g => g);
        var secondTransitionsPerState = secondTransWithEpsilon
            .GroupBy(t => t.From)
            .ToDictionary(g => g.Key, g => g);

        var productStates = new List<(int, int)>();

        foreach (var i1 in first.Initial)
            foreach (var i2 in second.Initial)
                productStates.Add((i1, i2));

        var transitions = new HashSet<(int, string, string, int)>();

        for (int n = 0; n < productStates.Count; n++)
        {
            var (p1, p2) = productStates[n];
            var p1Trans = firstTransitionsPerState[p1];
            var p2Trans = secondTransitionsPerState[p2];
            var productTrans = new List<(string, string, int, int)>();

            foreach (var tr1 in p1Trans)
                foreach (var tr2 in p2Trans)
                    productTrans.Add((tr1.Via, tr2.Via, tr1.To, tr2.To));

            foreach (var state in productTrans.Select(t => (t.Item3, t.Item4)))
                if (!productStates.Contains(state))
                    productStates.Add(state);

            foreach (var tr in productTrans)
                transitions.Add((n, tr.Item1, tr.Item2, productStates.IndexOf((tr.Item3, tr.Item4))));
        }

        var states = Enumerable.Range(0, productStates.Count);

        var initial = states.Where(s =>
            first.Initial.Contains(productStates[s].Item1) &&
            second.Initial.Contains(productStates[s].Item2));

        var final = states.Where(s =>
            first.Final.Contains(productStates[s].Item1) &&
            second.Final.Contains(productStates[s].Item2));

        return new Fst(states, initial, final, transitions).EpsilonFree().Trim();
    }

    public static (IReadOnlyList<(int, int)> States, IReadOnlyDictionary<(int From, char Via), int> Transitions)
        Product(
            (int Initial, IReadOnlyDictionary<(int From, char Via), int> Transitions) first,
            (int Initial, IReadOnlyDictionary<(int From, char Via), int> Transitions) second)
    {
        var stateTransitionMapOfFirst = first.Transitions
            .GroupBy(kvp => kvp.Key.From, kvp => (kvp.Key.Via, kvp.Value))
            .ToDictionary(g => g.Key, g => g);

        var productStates = new List<(int, int)> { (first.Initial, second.Initial) };
        var transitions = new Dictionary<(int, char), int>();

        for (var n = 0; n < productStates.Count; n++)
        {
            var (p1, p2) = productStates[n];

            var departingTransitions = stateTransitionMapOfFirst.ContainsKey(p1)
                ? stateTransitionMapOfFirst[p1]
                    .Where(pair => second.Transitions.ContainsKey((p2, pair.Via)))
                    .Select(pair => (pair.Via, (pair.Value, second.Transitions[(p2, pair.Via)])))
                : Array.Empty<(char, (int, int))>();

            foreach (var prodState in departingTransitions.Select(pair => pair.Item2))
                if (!productStates.Contains(prodState))
                    productStates.Add(prodState);

            // Transitions refer to states by their index in the states list
            foreach (var pair in departingTransitions)
                transitions.Add((n, pair.Via), productStates.IndexOf(pair.Item2));
        }

        return (productStates, transitions);
    }

    public static Dfsa Intersect(this Dfsa first, Dfsa second)
    {
        var product = Product(
            (first.Initial, first.Transitions),
            (second.Initial, second.Transitions));

        var states = Enumerable.Range(0, product.States.Count);
        var final = states
            .Where(s =>
                first.Final.Contains(product.States[s].Item1) &&
                second.Final.Contains(product.States[s].Item2));

        return new Dfsa(states, 0, final, product.Transitions).Trim();
    }

    public static Fsa Intersect(this Fsa first, Fsa second) =>
        first.Determinize().Intersect(second.Determinize()).ToFsa();

    public static Dfsa Difference(this Dfsa first, Dfsa second)
    {
        // The second automaton's transition function needs to be total
        var combinedAlphabet = first.Transitions.Select(t => t.Key.Via)
            .Concat(second.Transitions.Select(t => t.Key.Via))
            .Distinct();

        var secondTransitionsAsTotalFn = new Dictionary<(int, char), int>();

        // Make the function total by adding the missing transitions to the invalid state of "-1"
        foreach (var state in second.States.Concat(new[] { -1 }))
            foreach (var symbol in combinedAlphabet)
                secondTransitionsAsTotalFn[(state, symbol)] =
                    second.Transitions.ContainsKey((state, symbol))
                        ? second.Transitions[(state, symbol)]
                        : -1; // leads to an invalid state

        var product = Product(
            (first.Initial, first.Transitions),
            (second.Initial, secondTransitionsAsTotalFn));

        var states = Enumerable.Range(0, product.States.Count);
        var final = states
            .Where(s =>
                first.Final.Contains(product.States[s].Item1) &&
                !second.Final.Contains(product.States[s].Item2));

        return new Dfsa(states, 0, final, product.Transitions).Trim();
    }

    public static Fsa Difference(this Fsa first, Fsa second) =>
        first.Determinize().Difference(second.Determinize()).ToFsa();

    public static Fsa ToFsa(this Dfsa automaton) =>
        new Fsa(
            automaton.States,
            new[] { automaton.Initial },
            automaton.Final,
            automaton.Transitions
                .Select(p => (p.Key.From, p.Key.Via.ToString(), To: p.Value)));

    public static Fst Identity(this Fsa fst) =>
        new Fst(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.Via, t.Via, t.To)));
}