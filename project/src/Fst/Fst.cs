/*  
    Two-tape, classical finite-state transducer
*/
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

public class Fst
{
    public Fst(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string, string, int)> transitions)
    {
        this.States = states.ToList();
        this.Initial = initial.ToList();
        this.Final = final.ToList();
        this.Transitions = transitions.ToList();
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
                if (this.Final.Contains(state) || this.Final.Intersect(this.EpsilonClosure(state)).Any())
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

    IEnumerable<(string Out, int To)> GetTransitions(int state, string input) => 
        this.Transitions
            .Where(t => (state, input) == (t.From, t.In))
            .Select(t => (t.Out, t.To));

    IEnumerable<(string Out, int To)> GetTransitions(int state, string input, string output) => 
        this.Transitions
            .Where(t => (state, input, output) == (t.From, t.In, t.Out))
            .Select(t => (t.Out, t.To));

    public IEnumerable<int> EpsilonClosure(int state)
    {
        void TraverseEpsilonTransitionsDepthFirst(int current, HashSet<int> visited)
        {
            var epsilonTransitions = this.GetTransitions(current, string.Empty, string.Empty);

            foreach (var pair in epsilonTransitions)
            {
                if (!visited.Contains(pair.To))
                {
                    visited.Add(pair.To);
                    TraverseEpsilonTransitionsDepthFirst(pair.To, visited);
                }
            }
        }

        var result = new HashSet<int>() { state };
        TraverseEpsilonTransitionsDepthFirst(state, result);

        return result;
    }
}