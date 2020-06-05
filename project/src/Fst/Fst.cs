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
    public const char IdOutsideAlphabet = '\u0001';
    public const char AnyOutsideAlphabet = '\u0004';

    public Fst(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string, string, int)> transitions,
        IEnumerable<string> alphabet)
    {
        this.States = states.ToList();
        this.Initial = initial.ToHashSet();
        this.Final = final.ToHashSet();
        this.Transitions = transitions.ToList();
        this.Alphabet = alphabet.ToHashSet();
    }

    public Fst(
        IEnumerable<int> states,
        IEnumerable<int> initial,
        IEnumerable<int> final,
        IEnumerable<(int, string In, string Out, int)> transitions)
        : this(states, initial, final, transitions,
            transitions
                .Where(t =>
                    !string.IsNullOrEmpty(t.In) &&
                    t.In != IdOutsideAlphabet.ToString() &&
                    t.In != AnyOutsideAlphabet.ToString())
                // .Where(t => 
                //     !string.IsNullOrEmpty(t.Out) &&
                //     t.Out != IdOutsideAlphabet.ToString() && 
                //     t.Out != AnyOutsideAlphabet.ToString())
                .Select(t => t.In))
    { }

    public ICollection<int> States { get; private set; }
    public ICollection<int> Initial { get; private set; }
    public ICollection<int> Final { get; private set; }
    public ICollection<(int From, string In, string Out, int To)> Transitions { get; private set; }
    public ISet<string> Alphabet { get; private set; }

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

    IEnumerable<(string Out, int To)> GetTransitions(int state, string input)
    {
        if (this.Alphabet.Contains(input) || string.IsNullOrEmpty(input))
        {
            return this.Transitions
                .Where(tr => (state, input) == (tr.From, tr.In))
                .Select(tr => (tr.Out, tr.To));
        }

        return this.Transitions
            .Where(tr => tr.From == state && tr.In == Fsa.AnySymbolOutsideAlphabet.ToString())
            .Select(tr =>
            {
                var @out = tr.Out == Fsa.AnySymbolOutsideAlphabet.ToString() ? input : tr.Out;
                return (@out, tr.To);
            });
    }

    public string ToGraphViz()
    {
        var sb = new StringBuilder("digraph G { rankdir=LR; size=\"8,5\" ");

        sb.Append("node [shape=circle] ");

        foreach (var st in this.States)
        {
            foreach (var tr in this.Transitions.Where(t => t.From == st))
            {
                var @in = string.IsNullOrEmpty(tr.In) ? "ε" : tr.In;
                var @out = string.IsNullOrEmpty(tr.Out) ? "ε" : tr.Out;
                sb.Append($"{tr.From} -> {tr.To} [label=\"{@in}/{@out}\"]");
            }
        }

        sb.Append($"{string.Join(",", this.Final)} [shape = doublecircle]");
        sb.Append($"{string.Join(",", this.Initial)} [style = filled, fillcolor = lightgrey]");

        return sb.Append("}").ToString();
    }
}
