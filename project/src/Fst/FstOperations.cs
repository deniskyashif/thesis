using System;
using System.Collections.Generic;
using System.Linq;

public static class FstOperations
{
    static int NewState(IReadOnlyCollection<int> states) => states.Count;

    static IEnumerable<int> KNewStates(int k, IReadOnlyCollection<int> states) =>
        Enumerable.Range(states.Count, k);

    static Fst Remap(this Fst fst, IReadOnlyCollection<int> states)
    {
        var k = states.Count;

        return new Fst(
            fst.States.Select(s => s + k),
            fst.Initial.Select(s => s + k),
            fst.Final.Select(s => s + k),
            fst.Transitions.Select(t => (t.From + k, t.In, t.Out, t.To + k)));
    }

    public static Fst UnionWith(this Fst first, Fst second)
    {
        second = second.Remap(first.States);

        return new Fst(
            first.States.Concat(second.States),
            first.Initial.Concat(second.Initial),
            first.Final.Concat(second.Final),
            first.Transitions.Concat(second.Transitions));
    }

    public static Fst Union(this Fst fst, params Fst[] automata) =>
        automata.Aggregate(fst, UnionWith);

    public static Fst Concat(this Fst first, Fst second)
    {
        second = second.Remap(first.States);

        var transitions = first.Transitions
            .Concat(second.Transitions)
            .Concat(first.Final
                .SelectMany(f1 =>
                    second.Initial.Select(i2 => (f1, string.Empty, string.Empty, i2))));

        return new Fst(
            first.States.Concat(second.States),
            first.Initial,
            second.Final,
            transitions);
    }

    public static Fst Concat(this Fst fst, params Fst[] automata) =>
        automata.Aggregate(fst, Concat);

    public static Fst Star(this Fst fst)
    {
        var initial = new[] { NewState(fst.States) };
        var transitions = fst.Transitions
            .Concat(fst.Initial.Select(i => (initial[0], string.Empty, string.Empty, i)))
            .Concat(fst.Final.Select(f => (f, string.Empty, string.Empty, initial[0])));

        return new Fst(
            fst.States.Concat(initial),
            initial,
            fst.Final.Concat(initial),
            transitions);
    }

    public static Fst Plus(this Fst fst)
    {
        var initial = new[] { NewState(fst.States) };
        var transitions = fst.Transitions
            .Concat(fst.Initial.Select(i => (initial[0], string.Empty, string.Empty, i)))
            .Concat(fst.Final.Select(f => (f, string.Empty, string.Empty, initial[0])));

        return new Fst(
            fst.States.Concat(initial),
            initial,
            fst.Final,
            transitions);
    }

    public static Fst Option(this Fst fst)
    {
        var newState = new[] { NewState(fst.States) };

        return new Fst(
            fst.States.Concat(newState),
            fst.Initial.Concat(newState),
            fst.Final.Concat(newState),
            fst.Transitions);
    }

    public static Fst EpsilonFree(this Fst fst)
    {
        var epsilonClosureOf = fst.Transitions
            .Where(t => (string.IsNullOrEmpty($"{t.In}{t.Out}")))
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure()
            .Union(fst.States.Select(s => (From: s, To: s)))
            .GroupBy(p => p.Item1, p => p.Item2)
            .ToDictionary(g => g.Key, g => g.ToHashSet());

        var transitions = fst.Transitions
            .Where(tr => !(string.IsNullOrEmpty(tr.In) && string.IsNullOrEmpty(tr.Out)))
            .SelectMany(tr => epsilonClosureOf[tr.To].Select(to => (tr.From, tr.In, tr.Out, to)));

        return new Fst(
            fst.States,
            fst.Initial.SelectMany(s => epsilonClosureOf[s]),
            fst.Final,
            transitions);
    }

    public static Fst Trim(this Fst fst)
    {
        ICollection<(int From, int To)> transitiveClosure = fst.Transitions
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure();

        var reachableFromInitial = fst.Initial.Union(
            transitiveClosure.Where(p => fst.Initial.Contains(p.From)).Select(p => p.To));
        var leadingToFinal = fst.Final.Union(
            transitiveClosure.Where(p => fst.Final.Contains(p.To)).Select(p => p.From));

        var states = reachableFromInitial.Intersect(leadingToFinal).ToArray();
        var initial = states.Intersect(fst.Initial);
        var final = states.Intersect(fst.Final);
        var transitions = fst.Transitions
            .Where(t => states.Contains(t.From) && states.Contains(t.To))
            .Select(t => (Array.IndexOf(states, t.From), t.In, t.Out, Array.IndexOf(states, t.To)));

        return new Fst(
            states.Select(s => Array.IndexOf(states, s)),
            initial.Select(s => Array.IndexOf(states, s)),
            final.Select(s => Array.IndexOf(states, s)),
            transitions);
    }

    public static Fsa Domain(this Fst fst) =>
        new Fsa(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.In, t.To)));

    public static Fsa Range(this Fst fst) =>
        new Fsa(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.Out, t.To)));

    public static Fst Inverse(this Fst fst) =>
        new Fst(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.Out, t.In, t.To)));

    public static Fst Expand(this Fst fst)
    {
        string SymbolAt(string word, int index) =>
            index < word.Length
                ? word[index].ToString()
                : string.Empty;

        var multiWordTransitions = fst.Transitions
            .Where(t => t.In.Length > 1 || t.Out.Length > 1)
            .ToArray();
        var states = fst.States.ToList();
        var transitions = fst.Transitions
            .Where(t => !(t.In.Length > 1 || t.Out.Length > 1))
            .ToList();

        for (int n = 0; n < multiWordTransitions.Length; n++)
        {
            var curr = multiWordTransitions[n];
            var longerWordLen = Math.Max(curr.In.Length, curr.Out.Length);

            var intermediateStates = KNewStates(longerWordLen - 1, states);
            states.AddRange(intermediateStates);

            var stateSeq = (new[] { curr.From })
                .Concat(intermediateStates)
                .Concat(new[] { curr.To })
                .ToArray();

            for (var i = 0; i < longerWordLen; i++)
            {
                transitions.Add((
                    stateSeq[i],
                    SymbolAt(curr.In, i),
                    SymbolAt(curr.Out, i),
                    stateSeq[i + 1]));
            }
        }

        return new Fst(states, fst.Initial, fst.Final, transitions);
    }

    public static Fst Compose(this Fst first, Fst second)
    {
        first = first.Expand();
        second = second.Expand();

        var firstOutgoingTransitions = first.Transitions
            .Concat(first.States.Select(s => (From: s, In: string.Empty, Out: string.Empty, To: s)))
            .GroupBy(t => t.From, t => (t.In, t.Out, t.To))
            .ToDictionary(g => g.Key, g => g);

        var secondOutgoingTransitions = second.Transitions
            .Concat(second.States.Select(s => (From: s, In: string.Empty, Out: string.Empty, To: s)))
            .GroupBy(t => t.From, t => (t.In, t.Out, t.To))
            .ToDictionary(g => g.Key, g => g);

        var states = new List<(int, int)>();
        var transitions = new HashSet<(int, string, string, int)>();

        foreach (var i1 in first.Initial)
            foreach (var i2 in second.Initial)
                states.Add((i1, i2));

        var addedStates = new HashSet<(int, int)>(states);

        for (int n = 0; n < states.Count; n++)
        {
            var curr = states[n];
            var composedTransitions = firstOutgoingTransitions[curr.Item1]
                .SelectMany(t1 => secondOutgoingTransitions[curr.Item2]
                    .Where(t2 => t2.In == t1.Out) // (a, b) * (b, c) -> (a, c)
                    .Select(t2 => (Via: (t1.In, t2.Out), To: (t1.To, t2.To))));

            foreach (var tr in composedTransitions)
            {
                if (!addedStates.Contains(tr.To))
                {
                    states.Add(tr.To);
                    addedStates.Add(tr.To);
                }
            }

            transitions.UnionWith(
                composedTransitions.Select(t => (n, t.Via.In, t.Via.Out, states.IndexOf(t.To))));
        }

        var stateIndices = Enumerable.Range(0, states.Count);
        var initial = stateIndices.Where(s =>
            first.Initial.Contains(states[s].Item1) && second.Initial.Contains(states[s].Item2));
        var final = stateIndices.Where(s =>
            first.Final.Contains(states[s].Item1) && second.Final.Contains(states[s].Item2));

        return new Fst(stateIndices, initial, final, transitions).EpsilonFree().Trim();
    }

    public static Fst Compose(this Fst fst, params Fst[] automata) =>
        automata.Aggregate(fst, Compose);

    public static (Fst Transducer, ISet<string> EpsilonOutputs) ToRealTime(this Fst fst) =>
        fst.Trim().EpsilonFree().Expand().RemoveUpperEpsilon();

    private static (Fst, ISet<string>) RemoveUpperEpsilon(this Fst fst)
    {
        var upperEpsilonTransitions = fst.Transitions
            .Where(tr => string.IsNullOrEmpty(tr.In))
            .Select(tr => (tr.From, tr.To, tr.Out));

        var epsilonClosure = EpsilonClosure(upperEpsilonTransitions)
            .Union(fst.States.Select(s => (From: s, To: s, Out: string.Empty)));

        var initialToFinalViaEpsilon = epsilonClosure
            .Where(t => fst.Initial.Contains(t.From) && fst.Final.Contains(t.To));

        var possibleEpsilonOutputs = initialToFinalViaEpsilon.Select(t => t.Out).ToHashSet();
        var final = fst.Final.Union(initialToFinalViaEpsilon.Select(t => t.From));

        var reachableWithEpsilonFrom = epsilonClosure
            .GroupBy(t => t.From, t => (t.To, t.Out))
            .ToDictionary(g => g.Key, g => g);

        var reachableWithEpsilonTo = epsilonClosure
            .GroupBy(t => t.To, t => (t.From, t.Out))
            .ToDictionary(g => g.Key, g => g);

        var transitions = fst.Transitions
            .Where(tr => !string.IsNullOrEmpty(tr.In))
            .SelectMany(tr =>
            {
                reachableWithEpsilonTo.TryGetValue(tr.From, out var incoming);
                reachableWithEpsilonFrom.TryGetValue(tr.To, out var outgoing);

                var newTransitions = new List<(int, string, string, int)>();

                foreach (var inc in incoming)
                    foreach (var outg in outgoing)
                        newTransitions.Add((inc.From, tr.In, $"{inc.Out}{tr.Out}{outg.Out}", outg.To));

                return newTransitions;
            });

        var transducer = new Fst(fst.States, fst.Initial, final, transitions);

        return (transducer, possibleEpsilonOutputs);
    }

    private static IEnumerable<(int From, int To, string Out)> EpsilonClosure(
        IEnumerable<(int From, int To, string Out)> transitions)
    {
        var transitiveClosure = transitions.ToList();
        var transitionsForState = transitions
            .GroupBy(t => t.From, t => (t.To, t.Out))
            .ToDictionary(g => g.Key, g => g);

        for (int n = 0; n < transitiveClosure.Count; n++)
        {
            var current = transitiveClosure[n];

            if (current.From == current.To && !string.IsNullOrEmpty(current.Out))
                throw new ArgumentException("The transducer cannot be infinitely ambiguous.");

            if (transitionsForState.ContainsKey(current.To))
            {
                foreach (var tr in transitionsForState[current.To])
                {
                    var next = (current.From, tr.To, $"{current.Out}{tr.Out}");
                    if (!transitiveClosure.Contains(next))
                        transitiveClosure.Add(next);
                }
            }
        }

        return transitiveClosure;
    }

    public static Fst PseudoDeterminize(this Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst SquaredOutput(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static bool IsFunctional(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Bimachine ToBimachine(this Fst fst, ISet<char> alphabet)
    {
        // TODO: Handle epsilon inputs
        var (rtFst, _) = fst.ToRealTime();

        // Construct the right Dfa by reversing the transitions
        // Group the transitions by destination state
        var fstTransGroupedBy_4to12 = rtFst.Transitions
            .GroupBy(tr => tr.To)
            .ToDictionary(g => g.Key, g => g.Select(tr => (In: tr.In, To: tr.From)));

        var rightSStates = new List<ISet<int>> { rtFst.Final.ToHashSet() };
        var rightTransitions = new Dictionary<(int, char), int>();

        for (int n = 0; n < rightSStates.Count; n++)
        {
            var sState = rightSStates[n];
            var symbolToSStates = sState
                .Where(st => fstTransGroupedBy_4to12.ContainsKey(st))
                .SelectMany(st => fstTransGroupedBy_4to12[st])
                .GroupBy(tr => tr.In)
                .ToDictionary(g => g.Key, g => g.Select(tr => tr.To).ToHashSet());

            foreach (var subsetState in symbolToSStates.Select(p => p.Value))
                if (!rightSStates.Any(rs => rs.SetEquals(subsetState)))
                    rightSStates.Add(subsetState);

            foreach (var (label, targetSState) in symbolToSStates)
                rightTransitions.Add(
                    (n, label[0]),
                    rightSStates.FindIndex(ss => ss.SetEquals(targetSState)));
        }

        // Group right Dfa's transitions by a destination state
        var rightDfaTransGroupedBy_3to12 = rightTransitions
            .GroupBy(kvp => kvp.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(kvp => (To: kvp.Key.Item1, Symbol: kvp.Key.Item2)).ToHashSet());

        var fstTransGroupedBy_21to43 = new Dictionary<(char In, int From), ISet<(int To, string Out)>>();

        foreach (var tr in rtFst.Transitions)
        {
            if (!fstTransGroupedBy_21to43.ContainsKey((tr.In[0], tr.From)))
                fstTransGroupedBy_21to43[(tr.In[0], tr.From)] = new HashSet<(int, string)>();

            fstTransGroupedBy_21to43[(tr.In[0], tr.From)].Add((tr.To, tr.Out));
        }

        // Construct the left Dfa and the bimachine's output function
        var leftStates = new List<(ISet<int> SState, IDictionary<int, int> Selector)>();
        var leftTransitions = new Dictionary<(int, char), int>();
        var bmOutput = new Dictionary<(int, char, int), string>();

        // Construct left Dfa's initial state selector
        var initStateSelector = new Dictionary<int, int>();

        for (int rIndex = 0; rIndex < rightSStates.Count; rIndex++)
        {
            var initStates = rightSStates[rIndex].Intersect(rtFst.Initial);

            if (initStates.Any())
                initStateSelector.Add(rIndex, initStates.First());
        }

        leftStates.Add((rtFst.Initial.ToHashSet(), initStateSelector));

        for (int k = 0; k < leftStates.Count; k++)
        {
            var current = leftStates[k];

            // Find target states & their selectors on each alphabet symbol
            var targetTransForCurrentOnSymbol =
                new Dictionary<char, (ISet<int> LeftSState, IDictionary<int, int> SelectorForSymbol)>();

            foreach (var symbol in alphabet)
            {
                var targetLeftSState = new HashSet<int>();

                foreach (var st in current.SState)
                    if (fstTransGroupedBy_21to43.ContainsKey((symbol, st)))
                        targetLeftSState.UnionWith(
                            fstTransGroupedBy_21to43[(symbol, st)].Select(x => x.To));

                if (!targetLeftSState.Any())    
                    continue;

                var targetSelector = new Dictionary<int, int>();

                foreach (var (toRightIndex, fstState) in current.Selector)
                {
                    if (!rightDfaTransGroupedBy_3to12.ContainsKey(toRightIndex))
                        continue;

                    foreach (var (fromRIndex, _) in rightDfaTransGroupedBy_3to12[toRightIndex].Where(p => p.Symbol == symbol))
                    {
                        if (!fstTransGroupedBy_21to43.ContainsKey((symbol, fstState)))
                            continue;

                        var reachableTargets = fstTransGroupedBy_21to43[(symbol, fstState)]
                            .Where(p => rightSStates[fromRIndex].Contains(p.To));

                        if (reachableTargets.Any() && !targetSelector.ContainsKey(fromRIndex))
                            targetSelector.Add(fromRIndex, reachableTargets.First().To);
                    }
                }

                if (targetLeftSState.Any() && targetSelector.Any())
                    targetTransForCurrentOnSymbol.Add(symbol, (targetLeftSState, targetSelector));
            }

            foreach (var tr in targetTransForCurrentOnSymbol)
            {
                var symbol = tr.Key;
                var (targetSState, targetSelector) = tr.Value;

                // Add to the bimachine's output function
                foreach (var (fromRIndex, fstState) in targetSelector)
                {
                    if (!rightTransitions.ContainsKey((fromRIndex, symbol)))
                        continue;
                    if (!current.Selector.ContainsKey(rightTransitions[(fromRIndex, symbol)]))
                        continue;

                    var state = current.Selector[rightTransitions[(fromRIndex, symbol)]];
                    var destinationStates = fstTransGroupedBy_21to43[(symbol, state)].Where(p => p.To == fstState);

                    foreach (var (toState, word) in destinationStates)
                        if (!bmOutput.ContainsKey((k, symbol, fromRIndex)))
                            bmOutput.Add((k, symbol, fromRIndex), word);
                }

                // Left Dfa's states
                if (!leftStates.Any(ls => ls.SState.SetEquals(targetSState)))
                    leftStates.Add((targetSState, targetSelector));

                // Left Dfa's transitions
                leftTransitions.Add(
                    (k, symbol),
                    leftStates.FindIndex(p => p.SState.SetEquals(targetSState)));
            }
        }

        var leftStateIndices = Enumerable.Range(0, leftStates.Count);
        var leftDfsa = new Dfsa(leftStateIndices, 0, leftStateIndices, leftTransitions);
        var rightStateIndices = Enumerable.Range(0, rightSStates.Count);
        var rightDfsa = new Dfsa(rightStateIndices, 0, rightStateIndices, rightTransitions);

        return new Bimachine(leftDfsa, rightDfsa, bmOutput);
    }
}