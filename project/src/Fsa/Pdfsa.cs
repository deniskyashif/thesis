using System;
using System.Collections.Generic;
using System.Linq;

public class Pdfsa
{
    public Pdfsa(IEnumerable<int> states,
        int initial,
        IEnumerable<int> final,
        IDictionary<int, IList<(Range, int)>> transitions)
    {
        States = states.ToList();
        Initial = initial;
        Final = final.ToList();
        Transitions = transitions;
    }

    public ICollection<int> States { get; private set; }
    public int Initial { get; private set; }
    public ICollection<int> Final { get; private set; }
    public IDictionary<int, IList<(Range Label, int To)>> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        var curr = this.Initial;

        foreach (var symbol in word)
        {
            if (!this.Transitions.ContainsKey(curr))
                return false;

            var next = this.Transitions[curr].SingleOrDefault(t => t.Label.Includes(symbol));

            if (next == default)
                return false;
            
            curr = next.To;
        }

        return this.Final.Contains(curr);
    }
}