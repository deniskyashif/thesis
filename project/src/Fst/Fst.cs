/*  
    Finite-state transducer
*/
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System;

public class Fst
{
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
    }

    public IReadOnlyCollection<int> States { get; private set; }

    public IReadOnlyCollection<int> Initial { get; private set; }

    public IReadOnlyCollection<int> Final { get; private set; }

    public IReadOnlyCollection<(int From, string In, string Out, int To)> Transitions { get; private set; }

    public ICollection<string> Process(string word) =>
        this.Process(word.ToCharArray().Select(x => x.ToString()).ToList());

    public ISet<string> Process(IList<string> tokens)
    {
        var successfulPaths = new HashSet<string>();
        var path = new Stack<string>();

        void TraverseDepthFirst(int state, int index)
        {
            if (index == tokens.Count)
            {
                if (this.Final.Contains(state))
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
            }

            foreach (var pair in this.GetTransitions(state, string.Empty))
            {
                path.Push(pair.Out);
                TraverseDepthFirst(pair.To, index);
                path.Pop();
            }
        }

        foreach (var state in this.Initial)
            TraverseDepthFirst(state, index: 0);

        return successfulPaths;
    }

    IEnumerable<(string Out, int To)> GetTransitions(int state, string input) =>
        this.Transitions
            .Where(tr => (state, input) == (tr.From, tr.In))
            .Select(tr => (tr.Out, tr.To));
}