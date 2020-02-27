using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public static class FsaBuilder
{
    static int NewState(IImmutableSet<int> states) => states.Count;

    static IEnumerable<int> KNewStates(int k, IImmutableSet<int> states)
        => Enumerable.Range(states.Count, k);

    // Creates a new Fsa by renaming the states 
    private static Fsa Remap(Fsa automaton, int k)
    {
        var states = automaton.States.Select(s => s + k);
        var initial = automaton.InitialStates.Select(s => s + k);
        var final = automaton.FinalStates.Select(s => s + k);
        var transitions = automaton.Transitions.Select(t => (t.From + k, t.Via, t.To + k));

        return new Fsa(states, initial, final, transitions);
    }

    public static Fsa FromEpsilon() => FromWord(string.Empty);

    public static Fsa FromWord(string word)
    {
        var state = 0;
        var states = new List<int> { state };
        var initialStates = new int[] { state };
        var transitions = new List<(int, string, int)>();

        foreach (var symbol in word)
        {
            var next = state + 1;
            transitions.Add((state, symbol.ToString(), next));
            states.Add(next);
            state = next;
        }

        return new Fsa(
            states,
            initialStates,
            finalStates: new int[] { state },
            transitions);
    }

    public static Fsa FromSymbolSet(ISet<string> alphabet)
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, string, int)>();

        foreach (var token in alphabet)
            transitions.Add((initial, token, final));

        return new Fsa(
            states: new int[] { initial, final },
            initialStates: new int[] { initial },
            finalStates: new int[] { final },
            transitions);
    }

    public static Fsa Concat(Fsa first, Fsa second)
    {
        var firstFinalStates = first.FinalStates;
        second = Remap(second, first.States.Count);
        var secondInitialStates = second.InitialStates;

        var initialStates = first.InitialStates.Intersect(first.FinalStates).Any()
            ? first.InitialStates.Union(second.InitialStates)
            : first.InitialStates;

        var transitions = first.Transitions.Union(second.Transitions).ToList();

        foreach (var tr in first.Transitions.Where(t => first.FinalStates.Contains(t.To)))
            foreach (var state in second.InitialStates)
                transitions.Add((tr.From, tr.Via, state));

        return new Fsa(
            states: first.States.Union(second.States),
            initialStates,
            second.FinalStates,
            transitions);
    }

    public static Fsa Union(Fsa first, Fsa second)
    {
        second = Remap(second, first.States.Count);

        return new Fsa(
            states: first.States.Union(second.States),
            initialStates: first.InitialStates.Union(second.InitialStates),
            finalStates: first.FinalStates.Union(second.FinalStates),
            transitions: first.Transitions.Union(second.Transitions));
    }

    public static Fsa Star(Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, string, int)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates),
            initialStates,
            finalStates: automaton.FinalStates.Union(initialStates),
            automaton.Transitions.Union(newTransitions));
    }

    public static Fsa Plus(Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, string, int)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates),
            initialStates,
            finalStates: automaton.FinalStates,
            automaton.Transitions.Union(newTransitions));
    }

    public static Fsa Option(Fsa automaton)
    {
        var state = new[] { NewState(automaton.States) };

        return new Fsa(
            automaton.States.Union(state),
            automaton.InitialStates.Union(state),
            automaton.FinalStates.Union(state),
            automaton.Transitions);
    }

    public static Fsa All(ISet<string> alphabet)
        => FsaBuilder.Star(FsaBuilder.FromSymbolSet(alphabet));

    /* Preserves the automaton's language but 
       does not preserve the language of individual states */
    public static Fsa EpsilonFree(Fsa automaton)
    {
        var initial = automaton.InitialStates
            .SelectMany(automaton.EpsilonClosure);

        var transitions = automaton.Transitions
            .Where(t => !string.IsNullOrEmpty(t.Via))
            .SelectMany(t =>
                automaton
                    .EpsilonClosure(t.To)
                    .Select(es => (t.From, t.Via, es)));

        return new Fsa(automaton.States, initial, automaton.FinalStates, transitions);
    }

    public static Fsa Trim(Fsa automaton)
    {
        var reachableStates = automaton.Transitions
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure();

        var newStates = automaton.InitialStates
            .Union(
                reachableStates.Where(x => automaton.InitialStates.Contains(x.Item1))
                    .Select(x => x.Item2))
            .Intersect(
                automaton.FinalStates
                    .Union(
                        reachableStates.Where(x => automaton.FinalStates.Contains(x.Item2))
                            .Select(x => x.Item1)))
            .ToArray();

        var newTransitions = automaton.Transitions
            .Where(t => newStates.Contains(t.From) && newStates.Contains(t.To))
            .Select(t => (
                Array.IndexOf(newStates, t.From),
                t.Via,
                Array.IndexOf(newStates, t.To)));

        var newInitial = newStates
            .Intersect(automaton.InitialStates)
            .Select(s => Array.IndexOf(newStates, s));

        var newFinal = newStates
            .Intersect(automaton.FinalStates)
            .Select(s => Array.IndexOf(newStates, s));

        return new Fsa(
            newStates.Select(s => Array.IndexOf(newStates, s)),
            newInitial,
            newFinal,
            newTransitions);
    }

    public static Dfsa Trim(Dfsa automaton)
    {
        var reachableStates = automaton.Transitions
            .Select(t => (t.Key.From, t.Value))
            .ToHashSet()
            .TransitiveClosure();

        var newStates = new[] { automaton.InitialState }
            .Union(
                reachableStates
                    .Where(pair => pair.Item1 == automaton.InitialState)
                    .Select(pair => pair.Item2))
            .Intersect(
                automaton.FinalStates.Union(
                    reachableStates
                        .Where(pair => automaton.FinalStates.Contains(pair.Item2))
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
            Array.IndexOf(newStates, automaton.InitialState),
            automaton.FinalStates.Intersect(newStates).Select(s => Array.IndexOf(newStates, s)),
            newTransitions);
    }

    public static Fsa Expand(Fsa automaton)
    {
        var multiSymbolTransitions = automaton.Transitions.Where(t => t.Via.Length > 1);

        var newStates = automaton.States;
        var newTransitions = automaton.Transitions;

        foreach (var tr in multiSymbolTransitions)
        {
            var wordLen = tr.Via.Length;
            var stateSeq = new[] { tr.From }
                .Concat(KNewStates(wordLen - 1, newStates))
                .Concat(new[] { tr.To })
                .ToArray();

            newStates = newStates.Union(stateSeq);
            var path = Enumerable.Range(0, stateSeq.Length - 1)
                    .Select(i => (stateSeq[i], tr.Via[i].ToString(), stateSeq[i + 1]));

            newTransitions = newTransitions
                .Except(new[] { tr })
                .Union(path);
        }

        return new Fsa(newStates, automaton.InitialStates, automaton.FinalStates, newTransitions);
    }

    public static Dfsa Determinize(Fsa automaton)
    {
        var fsa = Expand(EpsilonFree(automaton));

        var stateTransitionMap = fsa.Transitions
            .GroupBy(t => t.From, t => (t.Via, t.To))
            .ToDictionary(g => g.Key, g => g.ToArray());

        var subsetStates = new List<int[]> { fsa.InitialStates.ToArray() };
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
            .Where(index => subsetStates[index].Intersect(fsa.FinalStates).Any())
            .ToArray();

        return new Dfsa(renamedStates, 0, finalStates, dfsaTransitions);
    }

    public static (IReadOnlyList<(int, int)> States, IReadOnlyDictionary<(int From, char Via), int> Transitions)
        Product(
            (int InitialState, IReadOnlyDictionary<(int From, char Via), int> Transitions) first,
            (int InitialState, IReadOnlyDictionary<(int From, char Via), int> Transitions) second)
    {
        var stateTransitionMapOfFirst = first.Transitions
            .GroupBy(kvp => kvp.Key.From, kvp => (kvp.Key.Via, kvp.Value))
            .ToDictionary(g => g.Key, g => g);

        var productStates = new List<(int, int)> { (first.InitialState, second.InitialState) };
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

    public static Dfsa Intersect(Dfsa first, Dfsa second)
    {
        var product = Product(
            (first.InitialState, first.Transitions), 
            (second.InitialState, second.Transitions));

        var states = Enumerable.Range(0, product.States.Count);
        var final = states
            .Where(s =>
                first.FinalStates.Contains(product.States[s].Item1) &&
                second.FinalStates.Contains(product.States[s].Item2));

        return Trim(new Dfsa(states, 0, final, product.Transitions));
    }

    public static Fsa Intersect(Fsa first, Fsa second) =>
        ToFsa(
            Intersect(
                Determinize(first),
                Determinize(second)));

    public static Dfsa Difference(Dfsa first, Dfsa second)
    {
        // The second automaton's transition function needs to be total
        var combinedAlphabet = first.Transitions.Select(t => t.Key.Via)
            .Concat(second.Transitions.Select(t => t.Key.Via))
            .Distinct();
        var secondTransitionsAsTotalFn = new Dictionary<(int, char), int>();

        // Make the function total by adding the missing transitions to the invalid state of "-1"
        foreach (var state in second.States.Concat(new [] { -1 }))
            foreach (var symbol in combinedAlphabet)
                secondTransitionsAsTotalFn[(state, symbol)] = 
                    second.Transitions.ContainsKey((state, symbol))
                        ? second.Transitions[(state, symbol)]
                        : -1; // leads to an invalid state

        var product = Product(
            (first.InitialState, first.Transitions), 
            (second.InitialState, secondTransitionsAsTotalFn));
        var states = Enumerable.Range(0, product.States.Count);
        var final = states
            .Where(s =>
                first.FinalStates.Contains(product.States[s].Item1) &&
                !second.FinalStates.Contains(product.States[s].Item2));

        return Trim(new Dfsa(states, 0, final, product.Transitions));
    }

    public static Fsa Difference(Fsa first, Fsa second) =>
        ToFsa(
            Difference(
                Determinize(first),
                Determinize(second)));

    public static Fsa Complement(Fsa automaton)
    {
        throw new NotImplementedException();
    }

    public static Fsa Reverse(Fsa automaton)
    {
        throw new NotImplementedException();
    }

    public static Fsa ToFsa(Dfsa automaton) =>
        new Fsa(
            automaton.States,
            new[] { automaton.InitialState },
            automaton.FinalStates,
            automaton.Transitions
                .Select(kvp => (kvp.Key.From, kvp.Key.Via.ToString(), To: kvp.Value)));
}