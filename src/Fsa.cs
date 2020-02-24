/*
    Finite-State Automaton -
    Construction and closure operations
*/

using System;
using System.Collections.Generic;
using System.Linq;

public class Fsa
{
    public Fsa(
        ISet<int> states,
        ISet<int> initialStates,
        ISet<int> finalStates,
        ISet<(int, string, int)> transitions)
    {
        this.States = states;
        this.InitialStates = initialStates;
        this.FinalStates = finalStates;
        this.Transitions = transitions;
    }

    public ISet<int> States { get; private set; }
    public ISet<int> InitialStates { get; private set; }
    public ISet<int> FinalStates { get; private set; }
    public ISet<(int From, string Via, int To)> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        var currentStates = this.InitialStates;

        foreach (var symbol in word)
        {
            var nextStates =
                currentStates.SelectMany(EpsilonClosure)
                .SelectMany(s => this.GetTransitions(s, symbol.ToString()))
                .ToHashSet();

            currentStates = nextStates;

            if (!currentStates.Any())
                break;
        }

        return this.FinalStates
            .Intersect(currentStates.SelectMany(EpsilonClosure))
            .Any();
    }

    ISet<int> GetTransitions(int state, string word)
    {
        return this.Transitions
            .Where(t => (state, word) == (t.From, t.Via))
            .Select(t => t.To)
            .ToHashSet();
    }

    public ISet<int> EpsilonClosure(int state)
    {
        void TraverseEpsilonTransitions(int current, HashSet<int> visited)
        {
            var epsilonTransitions = this.GetTransitions(current, string.Empty);
            foreach (var epsilonState in epsilonTransitions)
            {
                if (!visited.Contains(epsilonState))
                {
                    visited.Add(epsilonState);
                    TraverseEpsilonTransitions(epsilonState, visited);
                }
            }
        }

        var result = new HashSet<int>() { state };
        TraverseEpsilonTransitions(state, result);

        return result;
    }
}

public static class FsaBuilder
{
    private static int NewState(ISet<int> states) => states.Count;

    private static ISet<int> KNewStates(int k, ISet<int> states)
    {
        var newStates = new HashSet<int>();
        
        for (int i = 0; i < k; i++)
            newStates.Add(states.Count + i);
        
        return newStates;
    }

    private static Fsa RemapStates(Fsa automaton, int k)
    {
        var states = automaton.States.Select(s => s + k).ToHashSet();
        var initial = automaton.InitialStates.Select(s => s + k).ToHashSet();
        var final = automaton.FinalStates.Select(s => s + k).ToHashSet();
        var transitions = automaton.Transitions.Select(t => (t.From + k, t.Via, t.To + k)).ToHashSet();

        return new Fsa(states, initial, final, transitions);
    }

    public static Fsa FromEpsilon() => FromWord(string.Empty);

    public static Fsa FromWord(string word)
    {
        var state = 0;
        var states = new HashSet<int> { state };
        var initialStates = new HashSet<int> { state };
        var transitions = new HashSet<(int, string, int)>();

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
            finalStates: new HashSet<int> { state },
            transitions);
    }

    public static Fsa SymbolSet(ISet<string> alphabet)
    {
        var initial = 0;
        var final = 1;
        var transitions = new HashSet<(int, string, int)>();

        foreach (var token in alphabet)
            transitions.Add((initial, token, final));

        return new Fsa(
            states: new HashSet<int> { initial, final },
            initialStates: new HashSet<int> { initial },
            finalStates: new HashSet<int> { final },
            transitions);
    }

    public static Fsa Concat(Fsa first, Fsa second)
    {
        var firstFinalStates = first.FinalStates;
        second = RemapStates(second, first.States.Count);
        var secondInitialStates = second.InitialStates;

        var initialStates = first.InitialStates.Intersect(first.FinalStates).Any()
            ? first.InitialStates.Union(second.InitialStates)
            : first.InitialStates;

        var transitions = first.Transitions.Union(second.Transitions).ToHashSet();

        foreach (var tr in first.Transitions.Where(t => first.FinalStates.Contains(t.To)))
            foreach (var state in second.InitialStates)
                transitions.Add((tr.From, tr.Via, state));

        return new Fsa(
            states: first.States.Union(second.States).ToHashSet(),
            initialStates.ToHashSet(),
            second.FinalStates.ToHashSet(),
            transitions);
    }

    public static Fsa Union(Fsa first, Fsa second)
    {
        second = RemapStates(second, first.States.Count);
        return new Fsa(
            states: first.States.Union(second.States).ToHashSet(),
            initialStates: first.InitialStates.Union(second.InitialStates).ToHashSet(),
            finalStates: first.FinalStates.Union(second.FinalStates).ToHashSet(),
            transitions: first.Transitions.Union(second.Transitions).ToHashSet());
    }

    public static Fsa Star(Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new HashSet<int> { initial };
        var newTransitions = new HashSet<(int, string, int)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates).ToHashSet(),
            initialStates,
            finalStates: automaton.FinalStates.Union(initialStates).ToHashSet(),
            automaton.Transitions.Union(newTransitions).ToHashSet());
    }

    public static Fsa Plus(Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new HashSet<int> { initial };
        var newTransitions = new HashSet<(int, string, int)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates).ToHashSet(),
            initialStates,
            finalStates: automaton.FinalStates.ToHashSet(),
            automaton.Transitions.Union(newTransitions).ToHashSet());
    }

    public static Fsa Option(Fsa automaton)
    {
        var state = new[] { NewState(automaton.States) };

        return new Fsa(
            automaton.States.Union(state).ToHashSet(),
            automaton.InitialStates.Union(state).ToHashSet(),
            automaton.FinalStates.Union(state).ToHashSet(),
            automaton.Transitions.ToHashSet());
    }

    public static Fsa All(ISet<string> alphabet) 
        => FsaBuilder.Star(FsaBuilder.SymbolSet(alphabet));

    /* Preserves the automaton's language but 
       does not preserve the language of individual states */
    public static Fsa EpsilonFree(Fsa automaton)
    {
        var initialStates = automaton.InitialStates
            .SelectMany(automaton.EpsilonClosure)
            .ToHashSet();

        var transitions = automaton.Transitions
            .Where(t => !string.IsNullOrEmpty(t.Via))
            .SelectMany(t =>
                automaton
                    .EpsilonClosure(t.To)
                    .Select(es => (t.From, t.Via, es)))
            .ToHashSet();

        return new Fsa(automaton.States, initialStates, automaton.FinalStates, transitions);
    }

    public static Fsa Intersect(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Difference(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Determinize(Fsa automaton)
    {
        throw new NotImplementedException();
    }

    public static Fsa Complement(Fsa automaton)
    {
        throw new NotImplementedException();
    }
}