using System;
using System.Collections.Generic;

struct St { }
class Program
{
    static void Main()
    {
        var s1 = new St();
        var s2 = new St();
        var t1 = (s1, 'a', s2);
        var t2 = (s1, 'a', s2);
        var t3 = (s2, 'a', s1);
        var t4 = (s1, 'b', s2);

        Console.WriteLine(t1.Equals(t3));
        var hs = new HashSet<(St, char, St)>() { t1, t2, t3, t4 };
        Console.WriteLine(hs.Count);
    }

    static void FsaTest()
    {
        var fsa = FsaBuilder.FromWord("abc");
    }

    static void MinAcDfaTest()
    {
        var dfa = DfaBuilder.ConstructMinAcyclicDFA(new[] {
            "appl", "bapp", "cppe", "cppee", "x"
        });

        Console.WriteLine(dfa.Search("appl"));
        Console.WriteLine(dfa.Search("bapp"));
        Console.WriteLine(dfa.Search("cppe"));
        Console.WriteLine(dfa.Search("e"));
        Console.WriteLine(dfa.Search("ex"));
        Console.WriteLine(dfa.Search("cpp"));
    }

    void TrieTest()
    {
        var trie = new Trie();
        trie.Insert("apple");

        Console.WriteLine(trie.Search("apple")); // returns true
        Console.WriteLine(trie.Search("app"));     // returns false
        Console.WriteLine(trie.StartsWith("app")); // returns true

        trie.Insert("app");

        Console.WriteLine(trie.Search("app"));     // returns true
    }
}
