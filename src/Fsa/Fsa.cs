/*
    Finite-State Automaton -
    Construction and closure operations
*/

using System.Collections.Generic;
using System.Linq;

public class Fsa
{
    public Fsa(
        IReadOnlyList<int> states,
        IReadOnlyList<int> initialStates,
        IReadOnlyList<int> finalStates,
        IReadOnlyList<(int, string, int)> transitions)
    {
        this.States = states;
        this.InitialStates = initialStates;
        this.FinalStates = finalStates;
        this.Transitions = transitions;
    }

    public IReadOnlyList<int> States { get; private set; }
    public IReadOnlyList<int> InitialStates { get; private set; }
    public IReadOnlyList<int> FinalStates { get; private set; }
    public IReadOnlyList<(int From, string Via, int To)> Transitions { get; private set; }

    public bool Recognize(string word)
    {
        IEnumerable<int> currentStates = this.InitialStates;

        foreach (var symbol in word)
        {
            var nextStates =
                currentStates.SelectMany(EpsilonClosure)
                .SelectMany(s => this.GetTransitions(s, symbol.ToString()));

            currentStates = nextStates;

            if (!currentStates.Any())
                break;
        }

        return this.FinalStates
            .Intersect(currentStates.SelectMany(EpsilonClosure))
            .Any();
    }

    IEnumerable<int> GetTransitions(int state, string word)
    {
        return this.Transitions
            .Where(t => (state, word) == (t.From, t.Via))
            .Select(t => t.To);
    }

    public IEnumerable<int> EpsilonClosure(int state)
    {
        void TraverseEpsilonTransitions(int current, HashSet<int> visited)
        {
            var epsilonTransitions = this.GetTransitions(current, string.Empty);
            foreach (var epsilonState in epsilonTransitions)
            {
                if (!visited.Contains(epsilonState))
                {
                    visited.Add(epsilonState);
                    TraverseEpsilonTransitions(epsilonState, visited);
                }
            }
        }

        var result = new HashSet<int>() { state };
        TraverseEpsilonTransitions(state, result);

        return result;
    }
}