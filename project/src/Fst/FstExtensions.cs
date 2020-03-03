using System;
using System.Collections.Generic;
using System.Linq;

public static class FstExtensions
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

    public static Fst FromWordPair(string input, string output)
        => new Fst(
            new[] { 0, 1 },
            new[] { 0 },
            new[] { 1 },
            new[] { (0, input, output, 1) });

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

    public static Fst Product(Fsa first, Fsa second)
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
}