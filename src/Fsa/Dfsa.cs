using System.Collections.Generic;
using System.Linq;

public class Dfsa
{
    public Dfsa(
        IReadOnlyList<int> states, 
        int initialState, 
        IReadOnlyList<int> finalStates, 
        IReadOnlyDictionary<(int, string), int> transitions)
    {
        this.States = states;
        this.InitialState = initialState;
        this.FinalStates = finalStates;
        this.Transitions = transitions;
    }

    public IReadOnlyList<int> States { get; private set; }
    public int InitialState { get; set; }
    public IReadOnlyList<int> FinalStates { get; private set; }
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

