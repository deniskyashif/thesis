/*  
    Finite-state transducer
*/
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

public class Fst
{
    public Fst(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string, string, int)> transitions)
    {
        this.States = states.ToList();
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

    public ISet<string> Process(IList<string> inputs)
    {
        var successfulPaths = new HashSet<string>();
        var path = new Stack<string>();

        void TraverseDepthFirst(int state, int index)
        {
            if (index == inputs.Count)
            {
                if (this.Final.Contains(state))
                    successfulPaths.Add(string.Join(string.Empty, path.Reverse()));
            }
            else
            {
                foreach (var pair in this.GetTransitions(state, inputs[index]))
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
    
    public string ToGraphViz()
    {
        var sb = new StringBuilder("digraph G { rankdir=LR; size=\"8,5\" ");

        sb.Append("node [shape=circle] ");
        sb.Append("-1 [label= \"\", shape=none,height=.0,width=.0];");

        foreach (var i in this.Initial)
            sb.Append($"-1 -> {i};");

        foreach (var tr in this.Transitions)
        {
            var @in = string.IsNullOrEmpty(tr.In) ? "ε" : tr.In;
            var @out = string.IsNullOrEmpty(tr.Out) ? "ε" : tr.Out;
            sb.Append($"{tr.From} -> {tr.To} [label=\"{@in}/{@out}\"]");
        }

        sb.Append($"{string.Join(",", this.Final)} [shape = doublecircle]");

        return sb.Append("}").ToString();
    }
}