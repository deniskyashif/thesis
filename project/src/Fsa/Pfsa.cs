using System;
using System.Collections.Generic;
using System.Linq;

public class Pfsa
{
    private readonly Lazy<IDictionary<int, IEnumerable<int>>> epsilonClosureOf;

    public Pfsa(IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, Func<char, bool>, int)> transitions)
    {
        States = states.ToList();
        Initial = initial.ToList();
        Final = final.ToList();
        Transitions = transitions.ToList();

        this.epsilonClosureOf = new Lazy<IDictionary<int, IEnumerable<int>>>(
            () => this.PrecomputeEpsilonClosure());
    }

    public IReadOnlyCollection<int> States { get; private set; }
    public IReadOnlyCollection<int> Initial { get; private set; }
    public IReadOnlyCollection<int> Final { get; private set; }
    public IReadOnlyCollection<(int From, Func<char, bool> Pred, int To)> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        IEnumerable<int> currentStates = this.Initial;

        foreach (var symbol in word)
        {
            var nextStates = currentStates
                .SelectMany(EpsilonClosure)
                .SelectMany(s => this.GetTransitions(s, symbol));

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

    IEnumerable<int> GetTransitions(int state, char symbol) => 
        this.Transitions
            .Where(t => t.From == state && t.Pred != default && t.Pred(symbol))
            .Select(t => t.To);

    IDictionary<int, IEnumerable<int>> PrecomputeEpsilonClosure() => 
        this.Transitions
            .Where(t => t.Pred == default)
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure()
            .Union(this.States.Select(s => (From: s, To: s)))
            .GroupBy(p => p.Item1, p => p.Item2)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    
}