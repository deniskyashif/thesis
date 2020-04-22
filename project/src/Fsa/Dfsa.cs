using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Serializable]
public class Dfsa
{
    public Dfsa(
        IEnumerable<int> states,
        int initialState,
        IEnumerable<int> finalStates,
        IReadOnlyDictionary<(int, char), int> transitions)
    {
        this.States = states.ToList();
        this.Initial = initialState;
        this.Final = finalStates.ToHashSet();
        this.Transitions = transitions;
    }

    public IReadOnlyCollection<int> States { get; private set; }
    public int Initial { get; private set; }
    public IReadOnlyCollection<int> Final { get; private set; }
    public IReadOnlyDictionary<(int From, char Label), int> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        var curr = this.Initial;

        foreach (var symbol in word)
        {
            if (!this.Transitions.ContainsKey((curr, symbol)))
                return false;

            curr = this.Transitions[(curr, symbol)];
        }

        return this.Final.Contains(curr);
    }

    public string ToGraphViz(string rankDir = "LR")
    {
        var sb = new StringBuilder($"digraph {{ rankdir={rankDir}; size=\"8,5\" ");
        sb.Append("node [shape=circle] ");

        foreach (var st in this.States)
        {
            var trans = this.Transitions.Where(kvp => kvp.Key.From == st);
            if (trans.Any())
            {
                foreach (var tr in trans)
                    sb.Append($"{tr.Key.From} -> {tr.Value} [label=\"{tr.Key.Label}\"]; ");
            }
            else sb.Append($"{st};");
            
        }

        sb.Append($"{string.Join(",", this.Final)} [shape = doublecircle]");
        sb.Append($"{string.Join(",", this.Initial)} [style = filled, fillcolor = lightgrey]");

        return sb.Append("}").ToString();
    }
}
