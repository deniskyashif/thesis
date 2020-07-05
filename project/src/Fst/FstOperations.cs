/*  
    Operations on finite-state transducers i.e. regular relation algebra.
*/
using System;
using System.Collections.Generic;
using System.Linq;

public static class FstOperations
{
    static int NewState(IReadOnlyCollection<int> states) => states.Count;

    static IEnumerable<int> KNewStates(int k, IReadOnlyCollection<int> states) =>
        Enumerable.Range(states.Count, k);

    // Clones the finite automaton by renaming the states
    static Fst Remap(this Fst fst, IReadOnlyCollection<int> states)
    {
        var k = states.Count;

        return new Fst(
            fst.States.Select(s => s + k),
            fst.Initial.Select(s => s + k),
            fst.Final.Select(s => s + k),
            fst.Transitions.Select(t => (t.From + k, t.In, t.Out, t.To + k)));
    }

    public static Fst Union(this Fst first, Fst second)
    {
        second = second.Remap(first.States);

        return new Fst(
            first.States.Concat(second.States),
            first.Initial.Concat(second.Initial),
            first.Final.Concat(second.Final),
            first.Transitions.Concat(second.Transitions));
    }

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

    public static Fst Optional(this Fst fst)
    {
        var newState = new[] { NewState(fst.States) };

        return new Fst(
            fst.States.Concat(newState),
            fst.Initial.Concat(newState),
            fst.Final.Concat(newState),
            fst.Transitions);
    }

    // Removes the epsilon transitions by preserving the transducer's language
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

    // Removes the states that are not on a successful path in the automaton.
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

        var states = reachableFromInitial.Intersect(leadingToFinal).ToList();
        var initial = states.Intersect(fst.Initial);
        var final = states.Intersect(fst.Final);
        var transitions = fst.Transitions
            .Where(t => states.Contains(t.From) && states.Contains(t.To))
            .Select(t => (states.IndexOf(t.From), t.In, t.Out, states.IndexOf(t.To)));

        return new Fst(
            states.Select(s => states.IndexOf(s)),
            initial.Select(s => states.IndexOf(s)),
            final.Select(s => states.IndexOf(s)),
            transitions);
    }

    // Infers the underlying finite-state automaton from the upper tape
    public static Fsa Domain(this Fst fst) =>
        new Fsa(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.In, t.To)));

    // Infers the underlying finite-state automaton from the lower tape
    public static Fsa Range(this Fst fst) =>
        new Fsa(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.Out, t.To)));

    // Constructs a Fst by switching the tapes
    public static Fst Inverse(this Fst fst) =>
        new Fst(
            fst.States,
            fst.Initial,
            fst.Final,
            fst.Transitions.Select(t => (t.From, t.Out, t.In, t.To)));

    /* For a finite-state transducer, constructs an equivalent classical 2-tape letter automaton,
       where each transitions is of length <= 1 on both tapes. */
    public static Fst Expand(this Fst fst)
    {
        string SymbolAt(string word, int index) =>
            index < word.Length
                ? word[index].ToString()
                : string.Empty;

        var multiWordTransitions = fst.Transitions
            .Where(t => t.In.Length > 1 || t.Out.Length > 1)
            .ToList();
        var states = fst.States.ToList();
        var transitions = fst.Transitions
            .Where(t => !(t.In.Length > 1 || t.Out.Length > 1))
            .ToList();

        for (int n = 0; n < multiWordTransitions.Count; n++)
        {
            var curr = multiWordTransitions[n];
            var longerWordLen = Math.Max(curr.In.Length, curr.Out.Length);

            var intermediateStates = KNewStates(longerWordLen - 1, states);
            states.AddRange(intermediateStates);

            var stateSeq = (new[] { curr.From })
                .Concat(intermediateStates)
                .Concat(new[] { curr.To })
                .ToList();

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
        first = first.PseudoMinimal();
        second = second.PseudoMinimal();

        var firstTransPerState = first.Transitions
            .Concat(first.States.Select(s => (From: s, In: string.Empty, Out: string.Empty, To: s)))
            .GroupBy(t => t.From, t => (t.In, t.Out, t.To))
            .ToDictionary(g => g.Key, g => g);

        var secondTransPerState = second.Transitions
            .Concat(second.States.Select(s => (From: s, In: string.Empty, Out: string.Empty, To: s)))
            .GroupBy(t => t.From, t => (t.In, t.Out, t.To))
            .ToDictionary(g => g.Key, g => g);

        var productStates = new List<(int, int)>();
        var transitions = new HashSet<(int, string, string, int)>();

        foreach (var i1 in first.Initial)
            foreach (var i2 in second.Initial)
                productStates.Add((i1, i2));

        var addedStates = new HashSet<(int, int)>(productStates);

        for (int n = 0; n < productStates.Count; n++)
        {
            var curr = productStates[n];
            var compositeTransitions = new HashSet<((string In, string Out) Label, (int, int) To)>();

            foreach (var tr1 in firstTransPerState[curr.Item1])
                foreach (var tr2 in secondTransPerState[curr.Item2])
                    if (tr1.Out == tr2.In)
                        compositeTransitions.Add(
                            ((tr1.In, tr2.Out), (tr1.To, tr2.To)));

            foreach (var tr in compositeTransitions)
            {
                if (!addedStates.Contains(tr.To))
                {
                    productStates.Add(tr.To);
                    addedStates.Add(tr.To);
                }
            }

            transitions.UnionWith(
                compositeTransitions.Select(tr =>
                    (n, tr.Label.In, tr.Label.Out, productStates.IndexOf(tr.To))));
        }

        var prodStateIndices = Enumerable.Range(0, productStates.Count);

        var initial = prodStateIndices.Where(s =>
            first.Initial.Contains(productStates[s].Item1) &&
            second.Initial.Contains(productStates[s].Item2));

        var final = prodStateIndices.Where(s =>
            first.Final.Contains(productStates[s].Item1) &&
            second.Final.Contains(productStates[s].Item2));

        return new Fst(prodStateIndices, initial, final, transitions).EpsilonFree().Trim();
    }

    public static Fst Compose(this Fst fst, params Fst[] automata) =>
        automata.Aggregate(fst, Compose);

    // Produce a transducer by removing the epsilon transitions on the upper tape.
    public static (Fst Transducer, ISet<string> EpsilonOutputs) ToRealTime(this Fst fst) =>
        fst.Trim()
            .EpsilonFree()
            .RemoveUpperEpsilon();

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

    static IEnumerable<(int From, int To, string Out)> EpsilonClosure(
        IEnumerable<(int From, int To, string Out)> transitions)
    {
        var transitiveClosure = transitions.ToList();
        var transitionsForState = transitions
            .GroupBy(t => t.From, t => (t.To, t.Out))
            .ToDictionary(g => g.Key, g => g);

        for (int n = 0; n < transitiveClosure.Count; n++)
        {
            var current = transitiveClosure[n];

            // Detected a loop.
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

    // Used when converting an Fst to a Bimachine
    static bool AreSelectorsEqual(IDictionary<int, int> sel1, IDictionary<int, int> sel2)
    {
        if (sel1.Count != sel2.Count)
            return false;

        foreach (var p1 in sel1)
            if (sel2.TryGetValue(p1.Key, out int val) && val != p1.Value)
                return false;

        return true;
    }

    // Used when converting an Fst to a Bimachine
    static bool AreBmLeftStatesEqual(
        (ISet<int> SState, IDictionary<int, int> Selector) ls1,
        (ISet<int> SState, IDictionary<int, int> Selector) ls2) =>
        ls1.SState.SetEquals(ls2.SState) && AreSelectorsEqual(ls1.Selector, ls2.Selector);

    public static Bimachine ToBimachine(this Fst fst, ISet<char> alphabet)
    {
        var (rtFst, _) = fst.ToRealTime();

        // Construct the right Dfa by reversing the transitions
        // Group the transitions by destination state (4 to {12})
        var fstTransGroupedByTarget = rtFst.Transitions
            .GroupBy(tr => tr.To)
            .ToDictionary(g => g.Key, g => g.Select(tr => (In: tr.In, To: tr.From)));

        var rightSStates = new List<ISet<int>> { rtFst.Final.ToHashSet() };
        var rightTrans = new Dictionary<(int From, char Label), int>();

        for (int n = 0; n < rightSStates.Count; n++)
        {
            var symbolToSStates = rightSStates[n]
                .Where(st => fstTransGroupedByTarget.ContainsKey(st))
                .SelectMany(st => fstTransGroupedByTarget[st])
                .GroupBy(tr => tr.In, tr => tr.To)
                .ToDictionary(g => g.Key, g => g.ToHashSet());

            foreach (var (symbol, subsetState) in symbolToSStates)
                if (!rightSStates.Any(rs => rs.SetEquals(subsetState)))
                    rightSStates.Add(subsetState);

            foreach (var (label, targetSState) in symbolToSStates)
                rightTrans.Add(
                    (n, label.Single()),
                    rightSStates.FindIndex(ss => ss.SetEquals(targetSState)));
        }

        // Group right Dfa's transitions by a destination state
        var rDfaTransGroupedByTarget = rightTrans // 3 to 12
            .GroupBy(kvp => kvp.Value, kvp => (To: kvp.Key.From, Symbol: kvp.Key.Label))
            .ToDictionary(g => g.Key, g => g);
        var fstTransGroupedBySourceAndSymbol = new Dictionary<(char In, int From), ISet<(int To, string Out)>>();

        foreach (var tr in rtFst.Transitions)
        {
            if (!fstTransGroupedBySourceAndSymbol.ContainsKey((tr.In.Single(), tr.From)))
                fstTransGroupedBySourceAndSymbol[(tr.In.Single(), tr.From)] = new HashSet<(int, string)>();

            fstTransGroupedBySourceAndSymbol[(tr.In.Single(), tr.From)].Add((tr.To, tr.Out));
        }

        // Construct the left Dfa and the bimachine's output function
        var leftDfaStates = new List<(ISet<int> SState, IDictionary<int, int> Selector)>();
        var leftDfaTrans = new Dictionary<(int, char), int>();
        var bmOutput = new Dictionary<(int, char, int), string>();

        // Construct left Dfa's initial selector function.
        // The selector function maps a right dfa state (subset state index)
        // to a state in the input transducer
        var initStateSelector = new Dictionary<int, int>();

        for (int rIndex = 0; rIndex < rightSStates.Count; rIndex++)
        {
            var initStates = rightSStates[rIndex].Intersect(rtFst.Initial);
            if (initStates.Any())
                initStateSelector.Add(rIndex, initStates.First());
        }

        leftDfaStates.Add((rtFst.Initial.ToHashSet(), initStateSelector));

        for (int k = 0; k < leftDfaStates.Count; k++)
        {
            var currLState = leftDfaStates[k];
            // Find target states & their compute selectors on each alphabet symbol
            var targetLStatesPerSymbol =
                new Dictionary<char, (ISet<int> LSState, IDictionary<int, int> Selector)>();

            foreach (var symbol in alphabet)
            {
                var targetLSState = new HashSet<int>();
                // Successor (set of states) of L on symbol
                foreach (var st in currLState.SState)
                    if (fstTransGroupedBySourceAndSymbol.ContainsKey((symbol, st)))
                        targetLSState.UnionWith(
                            fstTransGroupedBySourceAndSymbol[(symbol, st)]
                                .Select(x => x.To));

                if (!targetLSState.Any()) continue;

                var targetSelector = new Dictionary<int, int>();

                foreach (var (toRIndex, fstState) in currLState.Selector)
                {
                    if (!rDfaTransGroupedByTarget.ContainsKey(toRIndex))
                        continue;
                    // toRIndex <--symbol-- fromRIndex
                    foreach (var (fromRIndex, _) in
                        rDfaTransGroupedByTarget[toRIndex].Where(p => p.Symbol == symbol))
                    {
                        if (!fstTransGroupedBySourceAndSymbol.ContainsKey((symbol, fstState)))
                            continue;
                        /* Pick any state from the intersection of the target left & source right states.
                            If there is a transition in the source transducer from this state on this symbol
                            but with different outputs - the transducer is not functional & we shoud throw. */
                        var reachableFstState = fstTransGroupedBySourceAndSymbol[(symbol, fstState)]
                            .FirstOrDefault(p => rightSStates[fromRIndex].Contains(p.To));

                        if (reachableFstState != default)
                            targetSelector.Add(fromRIndex, reachableFstState.To);
                    }
                }

                if (targetSelector.Any())
                    targetLStatesPerSymbol.Add(symbol, (targetLSState, targetSelector));
            }

            foreach (var (symbol, (targetLSState, targetSelector)) in targetLStatesPerSymbol)
            {
                // Add to the bimachine's output function
                foreach (var (fromRIndex, fstState) in targetSelector)
                {
                    var predecessorOfR = rightTrans[(fromRIndex, symbol)];
                    var state = currLState.Selector[predecessorOfR];
                    var destinations = fstTransGroupedBySourceAndSymbol[(symbol, state)]
                        .Where(p => p.To == fstState);

                    foreach (var (toState, word) in destinations)
                    {
                        var outFnPair = (Key: (k, symbol, fromRIndex), Val: word);

                        if (bmOutput.ContainsKey(outFnPair.Key))
                        {
                            if (bmOutput[outFnPair.Key] != outFnPair.Val)
                                throw new InvalidOperationException(
                                    $"Cannot have different values for the same key: '{bmOutput[outFnPair.Key]}', '{outFnPair.Val}'");
                        }
                        else bmOutput.Add(outFnPair.Key, outFnPair.Val);
                    }
                }

                var nextLState = (targetLSState, targetSelector);

                // Left Dfa's states
                if (!leftDfaStates.Any(ls => AreBmLeftStatesEqual(ls, nextLState)))
                    leftDfaStates.Add(nextLState);

                // Left Dfa's transitions
                leftDfaTrans.Add(
                    (k, symbol),
                    leftDfaStates.FindIndex(ls => AreBmLeftStatesEqual(ls, nextLState)));
            }
        }

        var leftStateIndices = Enumerable.Range(0, leftDfaStates.Count);
        var leftDfa = new Dfsa(leftStateIndices, 0, Array.Empty<int>(), leftDfaTrans);
        var rightStateIndices = Enumerable.Range(0, rightSStates.Count);
        var rightDfa = new Dfsa(rightStateIndices, 0, Array.Empty<int>(), rightTrans);

        return new Bimachine(leftDfa, rightDfa, bmOutput);
    }

    public static Fst PseudoDeterminize(this Fst fst)
    {
        var stateTransitionMap = fst.Transitions
            .GroupBy(t => t.From)
            .ToDictionary(g => g.Key, g => g.Select(t => (t.In, t.Out, t.To)));

        var subsetStates = new List<ISet<int>> { fst.Initial.ToHashSet() };
        var dfstTransitions = new HashSet<(int, string, string, int)>();

        for (int n = 0; n < subsetStates.Count; n++)
        {
            var labelToStates = subsetStates[n]
                .Where(state => stateTransitionMap.ContainsKey(state))
                .SelectMany(state => stateTransitionMap[state])
                .Distinct()
                .GroupBy(t => (t.In, t.Out), t => t.To)
                .ToDictionary(g => g.Key, g => g.ToHashSet());

            foreach (var kvp in labelToStates)
            {
                var label = kvp.Key;
                var target = kvp.Value;

                if (!subsetStates.Any(ss => ss.SetEquals(target)))
                    subsetStates.Add(target);

                var targetIndex = subsetStates.FindIndex(ss => ss.SetEquals(target));
                dfstTransitions.Add((n, label.In, label.Out, targetIndex));
            }
        }

        var dfstStates = Enumerable.Range(0, subsetStates.Count);
        var dfstFinal = dfstStates
            .Where(index => subsetStates[index].Intersect(fst.Final).Any());

        return new Fst(dfstStates, new[] { 0 }, dfstFinal, dfstTransitions).Trim();
    }

    public static Fst PseudoMinimal(this Fst fst)
    {
        int EquivClassCount(Dictionary<int, int> eqRel) =>
            eqRel.Values.Distinct().Count();

        fst = fst.PseudoDeterminize();
        var alphabet = fst.Transitions.Select(t => (t.In, t.Out)).Distinct();
        var transitionMap = fst.Transitions
            .GroupBy(t => (t.From, (t.In, t.Out)), t => t.To)
            .ToDictionary(g => g.Key, g => g.Single());

        // The initial two equivalence classes are the final and non-final states
        var eqRel = RelationOperations.Kernel(
            fst.States,
            st => fst.Final.Contains(st) ? 0 : -1);
        var prevEqClassCount = 0;

        while (alphabet.Any() && prevEqClassCount < EquivClassCount(eqRel))
        {
            var kernelsPerLabel = new List<IDictionary<int, int>>();

            foreach (var pair in alphabet)
            {
                Func<int, int> eqClassSelector = state =>
                    transitionMap.ContainsKey((state, pair))
                        ? eqRel[transitionMap[(state, pair)]]
                        : -1;
                kernelsPerLabel.Add(RelationOperations.Kernel(fst.States, eqClassSelector));
            }

            var nextEqRel = kernelsPerLabel.Count > 1
                ? RelationOperations.IntersectEqRel(fst.States, kernelsPerLabel[0], kernelsPerLabel[1])
                : kernelsPerLabel[0];

            for (int i = 2; i < kernelsPerLabel.Count; i++)
                nextEqRel = RelationOperations.IntersectEqRel(fst.States, nextEqRel, kernelsPerLabel[i]);

            prevEqClassCount = EquivClassCount(eqRel);
            eqRel = RelationOperations.IntersectEqRel(fst.States, eqRel, nextEqRel);
        }

        var minTransitions = new HashSet<(int, string, string, int)>();

        foreach (var tr in fst.Transitions)
            minTransitions.Add((eqRel[tr.From], tr.In, tr.Out, eqRel[tr.To]));

        return new Fst(
            fst.States.Select(s => eqRel[s]),
            fst.Initial.Select(s => eqRel[s]),
            fst.Final.Select(s => eqRel[s]),
            minTransitions).Trim();
    }
}
