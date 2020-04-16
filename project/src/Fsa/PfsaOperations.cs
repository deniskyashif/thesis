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
            automaton.Transitions.Select(t => (t.From + k, t.Pred, t.To + k)));
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
                transitions.Add((tr.From, tr.Pred, state));

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
        var newTransitions = new List<(int, Func<char, bool>, int)>();

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
        var newTransitions = new List<(int, Func<char, bool>, int)>();

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
            .Where(t => t.Pred != default)
            .SelectMany(t =>
                automaton
                    .EpsilonClosure(t.To)
                    .Select(es => (t.From, t.Pred, es)));

        return new Pfsa(automaton.States, initial, automaton.Final, transitions);
    }
}