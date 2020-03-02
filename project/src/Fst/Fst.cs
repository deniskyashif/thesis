/*  
    Two-tape, classical finite-state transducer
*/
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System;

public class Fst
{
    private readonly IReadOnlyDictionary<int, HashSet<int>> epsilonClosureOf;

    public Fst(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string, string, int)> transitions)
    {
        this.States = states.ToHashSet();
        this.Initial = initial.ToHashSet();
        this.Final = final.ToHashSet();
        this.Transitions = transitions.ToList();

        this.epsilonClosureOf = this.PrecomputeEpsilonClosure();
    }

    public IReadOnlyCollection<int> States { get; private set; }
    public IReadOnlyCollection<int> Initial { get; private set; }
    public IReadOnlyCollection<int> Final { get; private set; }
    public IReadOnlyCollection<(int From, string In, string Out, int To)> Transitions { get; private set; }

    public ICollection<string> Process(string word) => 
        this.Process(word.ToCharArray().Select(x => x.ToString()).ToArray());

    public ISet<string> Process(IList<string> tokens)
    {
        var successfulPaths = new HashSet<string>();
        var path = new Stack<string>();

        void TraverseDepthFirst(int state, int index)
        {
            if (index == tokens.Count)
            {
                if (this.Final.Intersect(this.EpsilonClosure(state)).Any())
                    successfulPaths.Add(string.Join(string.Empty, path.Reverse()));
            }
            else
            {
                foreach (var pair in this.GetTransitions(state, tokens[index]))
                {
                    path.Push(pair.Out);
                    TraverseDepthFirst(pair.To, index + 1);
                    path.Pop();
                }
                foreach (var pair in this.GetTransitions(state, string.Empty))
                {
                    path.Push(pair.Out);
                    TraverseDepthFirst(pair.To, index);
                    path.Pop();
                }
            }
        }

        foreach (var state in this.Initial)
            TraverseDepthFirst(state, index: 0);

        return successfulPaths;
    }

    public IEnumerable<int> EpsilonClosure(int state)
    {
        if (this.epsilonClosureOf.ContainsKey(state))
            return this.epsilonClosureOf[state];

        return Array.Empty<int>();        
    }

    IEnumerable<(string Out, int To)> GetTransitions(int state, string input) => 
        this.Transitions
            .Where(t => (state, input) == (t.From, t.In))
            .Select(t => (t.Out, t.To));

    IReadOnlyDictionary<int, HashSet<int>> PrecomputeEpsilonClosure() => 
        this.Transitions
            .Where(t => (string.IsNullOrEmpty($"{t.In}{t.Out}")))
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure()
            .Union(this.States.Select(s => (From: s, To: s)))
            .GroupBy(p => p.Item1, p => p.Item2)
            .ToDictionary(g => g.Key, g => g.ToHashSet());
}