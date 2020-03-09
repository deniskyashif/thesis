/*
  Non-deterministic finite-state automaton
*/
using System;
using System.Collections.Generic;
using System.Linq;

public class Fsa
{
    private readonly Lazy<IReadOnlyDictionary<int, HashSet<int>>> epsilonClosureOf;

    public Fsa(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string, int)> transitions)
    {
        this.States = states.ToHashSet();
        this.Initial = initial.ToHashSet();
        this.Final = final.ToHashSet();
        this.Transitions = transitions.ToHashSet();

        this.epsilonClosureOf = new Lazy<IReadOnlyDictionary<int, HashSet<int>>>(
            () => this.PrecomputeEpsilonClosure());
    }

    public IReadOnlyCollection<int> States { get; private set; }

    public IReadOnlyCollection<int> Initial { get; private set; }

    public IReadOnlyCollection<int> Final { get; private set; }

    public IReadOnlyCollection<(int From, string Label, int To)> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        IEnumerable<int> currentStates = this.Initial;

        foreach (var symbol in word)
        {
            var nextStates = currentStates
                .SelectMany(EpsilonClosure)
                .SelectMany(s => this.GetTransitions(s, symbol.ToString()));

            currentStates = nextStates;

            if (!currentStates.Any())
                break;
        }

        return this.Final
            .Intersect(currentStates.SelectMany(EpsilonClosure))
            .Any();
    }

    public IEnumerable<int> EpsilonClosure(int state)
    {
        if (this.epsilonClosureOf.Value.ContainsKey(state))
            return this.epsilonClosureOf.Value[state];

        return Array.Empty<int>();
    }

    IEnumerable<int> GetTransitions(int state, string label) => 
        this.Transitions
            .Where(t => (state, label) == (t.From, t.Label))
            .Select(t => t.To);

    IReadOnlyDictionary<int, HashSet<int>> PrecomputeEpsilonClosure() => 
        this.Transitions
            .Where(t => string.IsNullOrEmpty(t.Label))
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure()
            .Union(this.States.Select(s => (From: s, To: s)))
            .GroupBy(p => p.Item1, p => p.Item2)
            .ToDictionary(g => g.Key, g => g.ToHashSet());
}
