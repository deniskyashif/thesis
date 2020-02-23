/*
    Finite-State Automaton -
    Construction and closure operations
*/

using System;
using System.Collections.Generic;
using System.Linq;

public class Fsa
{
    public class State { }

    public Fsa(
        ISet<State> states, 
        ISet<State> initialStates, 
        ISet<State> finalStates,
        ISet<(State, string, State)> transitions)
    {
        this.States = states;
        this.InitialStates = initialStates;
        this.FinalStates = finalStates;
        this.Transitions = transitions;
    }
    public ISet<State> States { get; private set; }
    public ISet<State> InitialStates { get; private set; }
    public ISet<State> FinalStates { get; private set; }
    public ISet<(State From, string Via, State To)> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        var currentStates = this.InitialStates;

        foreach (var symbol in word)
        {
            var nextStatesViaEpsilon = currentStates.SelectMany(EpsilonClosure);
            var nextStates = currentStates
                .Union(nextStatesViaEpsilon)
                .SelectMany(s => this.GetTransitions(s, symbol.ToString()))
                .ToHashSet();

            currentStates = nextStates;

            if (!currentStates.Any())
                break;
        }

        return this.FinalStates.Intersect(currentStates).Any() ||
            this.FinalStates.Intersect(currentStates.SelectMany(EpsilonClosure)).Any();
    }

    ISet<State> GetTransitions(State state, string word)
    {
        return this.Transitions
            .Where(t => (state, word) == (t.From, t.Via))
            .Select(t => t.To)
            .ToHashSet();
    }

    ISet<State> EpsilonClosure(State state)
    {
        void TraverseEpsilonTransitions(State current, HashSet<State> visited)
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

        var result = new HashSet<State>();
        TraverseEpsilonTransitions(state, result);

        return result;
    }
}

public static class FsaBuilder
{
    public static Fsa FromEpsilon() => FromWord(string.Empty);

    public static Fsa FromWord(string word)
    {
        var state = new Fsa.State();
        var states = new HashSet<Fsa.State>{ state };
        var initialStates = new HashSet<Fsa.State>{ state };
        var transitions = new HashSet<(Fsa.State, string, Fsa.State)>();

        foreach (var symbol in word)
        {
            var next = new Fsa.State();
            transitions.Add((state, symbol.ToString(), next));
            states.Add(next);
            state = next;
        }

        return new Fsa(
            states,
            initialStates,
            finalStates: new HashSet<Fsa.State> { state },
            transitions);
    }

    public static Fsa UniversalLanguage(IEnumerable<string> alphabet)
    {
        var state = new Fsa.State();
        var transitions = new HashSet<(Fsa.State, string, Fsa.State)>();

        foreach (var token in alphabet)
            transitions.Add((state, token, state));

        return new Fsa(
            states: new HashSet<Fsa.State> { state },
            initialStates: new HashSet<Fsa.State> { state },
            finalStates: new HashSet<Fsa.State> { state },
            transitions);
    }

    public static Fsa Concat(Fsa first, Fsa second)
    {
        var firstFinalStates = first.FinalStates;
        var secondInitialStates = second.InitialStates;

        var initialStates = first.InitialStates.Intersect(first.FinalStates).Any() 
            ? first.InitialStates.Union(second.InitialStates)
            : first.InitialStates;

        var transitions = first.Transitions.Union(second.Transitions).ToHashSet();

        foreach (var tr in first.Transitions.Where(t => first.FinalStates.Contains(t.To)))
        {
            foreach (var state in second.InitialStates)
                transitions.Add((tr.From, tr.Via, state));
        }

        return new Fsa(
            states: first.States.Union(second.States).ToHashSet(),
            initialStates.ToHashSet(),
            second.FinalStates.ToHashSet(),
            transitions
        );
    }

    public static Fsa Union(Fsa first, Fsa second)
    {
        return new Fsa(
            states: first.States.Union(second.States).ToHashSet(),
            initialStates: first.InitialStates.Union(second.InitialStates).ToHashSet(),
            finalStates: first.FinalStates.Union(second.FinalStates).ToHashSet(),
            transitions: first.Transitions.Union(second.Transitions).ToHashSet());
    }

    public static Fsa Star(Fsa automaton)
    {
        var initial = new Fsa.State();
        var newTransitions = new HashSet<(Fsa.State, string, Fsa.State)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));
        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        var initialStates = new HashSet<Fsa.State> { initial };

        return new Fsa(
            states: automaton.States.Union(initialStates).ToHashSet(),
            initialStates,
            finalStates: automaton.FinalStates.Union(initialStates).ToHashSet(),
            automaton.Transitions.Union(newTransitions).ToHashSet()
        );
    }

    public static Fsa Difference(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Intersect(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa RemoveEpsilonTransitions(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }
}