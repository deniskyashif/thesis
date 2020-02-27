using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

public class Dfsa
{
    public Dfsa(
        IEnumerable<int> states, 
        int initialState, 
        IEnumerable<int> finalStates,
        IReadOnlyDictionary<(int, string), int> transitions)
    {
        this.States = states.ToImmutableHashSet();
        this.InitialState = initialState;
        this.FinalStates = finalStates.ToImmutableHashSet();
        this.Transitions = transitions.ToImmutableDictionary();
    }

    public IImmutableSet<int> States { get; private set; }
    public int InitialState { get; private set; }
    public IImmutableSet<int> FinalStates { get; private set; }
    public IReadOnlyDictionary<(int From, string Via), int> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        var curr = this.InitialState;

        foreach (var symbol in word)
        {
            if (!this.Transitions.ContainsKey((curr, symbol.ToString())))
                return false;
            curr = this.Transitions[(curr, symbol.ToString())];
        }

        return this.FinalStates.Contains(curr);
    }
}

