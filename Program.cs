using System;

class Program
{
    static void Main()
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
