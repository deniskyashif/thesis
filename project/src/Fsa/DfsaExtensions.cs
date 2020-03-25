using System.Collections.Generic;
using System.Linq;

public static class DfsaExtensions
{
    public static (bool Success, IList<int> Path) RecPathRightToLeft(this Dfsa automaton, string word)
    {
        var current = automaton.Initial;
        var path = new List<int> { current };

        for (var i = word.Length - 1; i >= 0; i--)
        {
            var symbol = word[i];

            if (!automaton.Transitions.ContainsKey((current, symbol)))
                return (false, path);

            current = automaton.Transitions[(current, symbol)];
            path.Add(current);
        }

        return (automaton.Final.Contains(current), path);
    }
}