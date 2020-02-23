/*
    Finite-State Automaton -
    Construction and closure operations
*/

using System;
using System.Collections.Generic;
using System.Linq;

public class Fsa
{
    public Fsa() : this(new HashSet<State>()) { }
    public Fsa(ICollection<State> states)
    {
        this.States = states;
    }
    public ICollection<State> States { get; private set; }

    public IEnumerable<State> InitialStates => this.States.Where(s => s.IsInitial);

    public IEnumerable<State> FinalStates => this.States.Where(s => s.IsFinal);

    public bool Recognize(string word)
    {
        var currentStates = this.InitialStates;

        foreach (var symbol in word)
        {
            var nextStatesViaEpsilon = currentStates
                .SelectMany(s => s.EpsilonClosure());
            var nextStates = currentStates
                .Concat(nextStatesViaEpsilon)
                .SelectMany(s => s.GetTransitnion(symbol));

            currentStates = nextStates;

            if (!currentStates.Any())
                break;
        }

        return currentStates.Any(s => s.IsFinal);
    }

    public class State
    {
        private readonly IDictionary<char, ICollection<State>> transitions = new Dictionary<char, ICollection<State>>();
        private readonly ISet<State> epsilonTransitions = new HashSet<State>();

        public State(bool isInitial = false, bool isFinal = false)
        {
            this.IsInitial = isInitial;
            this.IsFinal = isFinal;
        }

        public bool IsInitial { get; set; }

        public bool IsFinal { get; set; }

        public ICollection<State> GetTransitnion(char symbol)
        {
            this.transitions.TryGetValue(symbol, out var states);
            return states ?? Array.Empty<State>();
        }

        public State AddTransition(char symbol, State state)
        {
            if (!this.transitions.ContainsKey(symbol))
                this.transitions.Add(symbol, new HashSet<State>());

            this.transitions[symbol].Add(state);
            return this;
        }

        public State AddEpsilonTransition(State state)
        {
            this.epsilonTransitions.Add(state);
            return this;
        }

        public IEnumerable<State> EpsilonClosure()
        {
            void TraverseEpsilonTransitions(State current, HashSet<State> visited)
            {
                foreach (var epsilonState in current.epsilonTransitions)
                {
                    if (!visited.Contains(epsilonState))
                    {
                        visited.Add(epsilonState);
                        TraverseEpsilonTransitions(epsilonState, visited);
                    }
                }
            }

            var states = new HashSet<State>();
            TraverseEpsilonTransitions(this, states);

            return states;
        }
    }
}


public static class FsaBuilder
{
    public static Fsa FromEpsilon() => FromWord(string.Empty);
    
    public static Fsa FromWord(string word)
    {
        var fsa = new Fsa();
        var state = new Fsa.State(true, false);
        fsa.States.Add(state);
        
        foreach (var symbol in word)
        {
            var next = new Fsa.State(false, false);
            state.AddTransition(symbol, next);
            fsa.States.Add(next);
            state = next;
        }

        state.IsFinal = true;
        return fsa;
    }

    public static Fsa UniversalLanguage(IEnumerable<char> alphabet)
    {
        var fsa = new Fsa();
        var state = new Fsa.State(true, true);

        foreach (var symbol in alphabet)
            state.AddTransition(symbol, state);

        fsa.States.Add(state);
        return fsa;
    }

    public static Fsa Concat(Fsa first, Fsa second)
    {
        var fsa = new Fsa();
        var firstFinalStates = first.FinalStates.ToArray();
        var secondInitialStates = second.InitialStates.ToArray();

        foreach (var finalStateOfFirst in firstFinalStates)
        {
            foreach (var initialStateOfSecond in secondInitialStates)
            {
                finalStateOfFirst.AddEpsilonTransition(initialStateOfSecond);
                initialStateOfSecond.IsInitial = false;
            }
            finalStateOfFirst.IsFinal = false;
        }

        var allStates = first.States.Union(second.States);

        foreach (var state in allStates)
            fsa.States.Add(state);

        return fsa;
    }

    public static Fsa Union(Fsa first, Fsa second)
    {
        var initial = new Fsa.State(true, false);
        var final = new Fsa.State(false, true);

        foreach (var state in first.InitialStates.Union(second.InitialStates))
        {
            initial.AddEpsilonTransition(state);
            state.IsInitial = false;
        }

        foreach (var state in first.FinalStates.Union(second.FinalStates))
        {
            initial.AddEpsilonTransition(state);
            state.IsFinal = false;
        }

        var allStates = first.States
            .Union(second.States)
            .Append(initial)
            .Append(final);

        return new Fsa(allStates.ToHashSet());
    }

    public static Fsa Difference(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }
}