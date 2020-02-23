using System;

class Program
{
    static void Main()
    {
        FsaTest();
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
