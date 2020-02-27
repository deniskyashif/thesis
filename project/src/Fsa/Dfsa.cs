using System.Collections.Generic;
using System.Collections.Immutable;

public class Dfsa
{
    public Dfsa(
        IEnumerable<int> states, 
        int initialState, 
        IEnumerable<int> finalStates,
        IReadOnlyDictionary<(int, char), int> transitions)
    {
        this.States = states.ToImmutableHashSet();
        this.Initial = initialState;
        this.Final = finalStates.ToImmutableHashSet();
        this.Transitions = transitions.ToImmutableDictionary();
    }

    public IImmutableSet<int> States { get; private set; }
    public int Initial { get; private set; }
    public IImmutableSet<int> Final { get; private set; }
    public IReadOnlyDictionary<(int From, char Via), int> Transitions { get; private set; }

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
}

