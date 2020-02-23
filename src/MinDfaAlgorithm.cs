/*
    Direct construction of a minimal, deterministic, acyclic 
    finite state automaton from a set of strings.
*/
using System.Collections.Generic;
using System.Linq;

class DfaNode
{
    private SortedDictionary<char, DfaNode> transitions = new SortedDictionary<char, DfaNode>();

    public bool IsFinal { get; set; }
    public bool HasTransitions => this.transitions.Any();
    public int TransitionCount => this.transitions.Count;
    public KeyValuePair<char, DfaNode> LastChild => this.transitions.LastOrDefault();

    public DfaNode GetTransitnion(char symbol)
    {
        this.transitions.TryGetValue(symbol, out var node);
        return node;
    }

    public DfaNode AddTransition(char symbol)
    {
        var node = new DfaNode();
        this.transitions.Add(symbol, node);
        return node;
    }

    public DfaNode UpdateTransitnion(char symbol, DfaNode node)
    {
        this.transitions[symbol] = node;
        return node;
    }

    public bool IsEquivalentTo(DfaNode q)
    {
        if ((this.IsFinal != q.IsFinal) || (this.TransitionCount != q.TransitionCount))
            return false;

        foreach (var pair in this.transitions)
        {
            var qNext = q.GetTransitnion(pair.Key);
            if (qNext == null || !pair.Value.IsEquivalentTo(qNext))
                return false;
        }

        return true;
    }
}

static class DfaExtensions
{
    public static DfaNode Insert(this DfaNode node, string word)
    {
        var curr = node;

        foreach (var symbol in word)
        {
            var next = curr.GetTransitnion(symbol);

            if (next == null)
                next = curr.AddTransition(symbol);

            curr = next;
        }

        curr.IsFinal = true;
        return node;
    }

    public static (DfaNode EndNode, int PathLength) Walk(this DfaNode node, string word)
    {
        var curr = node;
        int i;

        for (i = 0; i < word.Length; i++)
        {
            var next = curr.GetTransitnion(word[i]);
            if (next == null)
                break;
            curr = next;
        }

        return (curr, i);
    }

    public static bool Search(this DfaNode node, string word)
    {
        var (endNode, pathLength) = node.Walk(word);
        return pathLength == word.Length && endNode.IsFinal;
    }
}

static class DfaBuilder
{
    public static DfaNode ConstructMinAcyclicDFA(IEnumerable<string> words)
    {
        var register = new HashSet<DfaNode>();
        var start = new DfaNode();

        foreach (var word in words)
        {
            var (lastState, pathLength) = start.Walk(word);
            var commonPrefix = word.Substring(0, pathLength);
            var currentSuffix = word.Substring(pathLength);

            if (lastState.HasTransitions)
                ReplaceOrRegister(lastState, register);
            lastState.Insert(currentSuffix);
        }

        ReplaceOrRegister(start, register);

        return start;
    }

    static void ReplaceOrRegister(DfaNode state, ICollection<DfaNode> register)
    {
        var (symbol, child) = state.LastChild;

        if (child.HasTransitions)
            ReplaceOrRegister(child, register);

        var equiv = FindEquivalent(child, register);

        if (equiv != null)
            state.UpdateTransitnion(symbol, equiv);
        else
            register.Add(child);
    }

    static DfaNode FindEquivalent(DfaNode state, ICollection<DfaNode> register)
        => register.FirstOrDefault(s => s.IsEquivalentTo(state));
}