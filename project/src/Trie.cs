/*
    Trie - construction & traversal.
*/
using System.Collections.Generic;

public class Trie
{
    private IDictionary<char, Trie> transitions = new Dictionary<char, Trie>();

    private bool IsFinal { get; set; }

    public void Insert(string word)
    {
        var current = this;

        foreach (var s in word)
        {
            var next = current.GetTransitnion(s);

            if (next == null)
                next = current.AddTransition(s);

            current = next;
        }

        current.IsFinal = true;
    }

    public bool Recognize(string word) => Traverse(word)?.IsFinal ?? false;

    public bool StartsWith(string prefix) => Traverse(prefix) != null;

    private Trie Traverse(string word)
    {
        var curr = this;
        foreach (var s in word)
        {
            curr = curr.GetTransitnion(s);
            if (curr == null)
                break;
        }

        return curr;    
    }

    private Trie GetTransitnion(char symbol)
    {
        this.transitions.TryGetValue(symbol, out var node);

        return node;
    }

    private Trie AddTransition(char symbol)
    {
        var node = new Trie();
        this.transitions.Add(symbol, node);

        return node;
    }
}