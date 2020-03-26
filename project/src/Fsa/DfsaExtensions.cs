using System.Collections.Generic;

public static class DfsaExtensions
{
    public static IList<int> RecPathRightToLeft(this Dfsa automaton, string word)
    {
        var current = automaton.Initial;
        var path = new List<int> { current };

        for (var i = word.Length - 1; i >= 0; i--)
        {
            var symbol = word[i];

            if (!automaton.Transitions.ContainsKey((current, symbol)))
                return path;

            current = automaton.Transitions[(current, symbol)];
            path.Add(current);
        }

        return path;
    }
}