/*
  Non-deterministic finite-state automaton
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Fsa
{
    public Fsa(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string, int)> transitions)
    {
        this.States = states.ToList();
        this.Initial = initial.ToHashSet();
        this.Final = final.ToHashSet();
        this.Transitions = transitions.ToHashSet();
    }

    public IReadOnlyCollection<int> States { get; private set; }
    public IReadOnlyCollection<int> Initial { get; private set; }
    public IReadOnlyCollection<int> Final { get; private set; }
    public IReadOnlyCollection<(int From, string Label, int To)> Transitions { get; private set; }
    public ISet<string> Alphabet => this.Transitions.Select(t => t.Label).ToHashSet();

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

    public IEnumerable<int> EpsilonClosure(int state) => 
        this.Transitions
            .Where(t => string.IsNullOrEmpty(t.Label))
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure()
            .Where(p => p.Item1 == state)
            .Select(p => p.Item2)
            .Union(new[] { state });

    IEnumerable<int> GetTransitions(int state, string label) => 
        this.Transitions
            .Where(t => t.From == state && t.Label == label)
            .Select(t => t.To);

    public string ToGraphViz()
    {
        var sb = new StringBuilder("digraph { rankdir=LR; size=\"8,5\" ");
        sb.Append("node [shape=circle] ");

        foreach (var st in this.States)
        {
            foreach (var tr in this.Transitions.Where(t => t.From == st))
            {
                var label = string.IsNullOrEmpty(tr.Label) ? "ε" : tr.Label;
                sb.Append($"{tr.From} -> {tr.To} [label=\"{label}\"]; ");
            }            
        }

        sb.Append($"{string.Join(",", this.Final)} [shape = doublecircle]");
        sb.Append($"{string.Join(",", this.Initial)} [style = filled, fillcolor = lightgrey]");

        return sb.Append("}").ToString();
    }
}
