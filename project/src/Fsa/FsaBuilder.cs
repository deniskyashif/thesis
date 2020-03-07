using System.Collections.Generic;
using System.Linq;

public static class FsaBuilder
{
    public static Fsa FromEpsilon() => FromWord(string.Empty);

    public static Fsa FromWord(string word)
    {
        var state = 0;
        var states = new List<int> { state };
        var initialStates = new int[] { state };
        var transitions = new List<(int, string, int)>();

        foreach (var symbol in word)
        {
            var next = state + 1;
            transitions.Add((state, symbol.ToString(), next));
            states.Add(next);
            state = next;
        }

        return new Fsa(states, initialStates, new int[] { state }, transitions);
    }

    public static Fsa FromSymbolSet(IEnumerable<char> alphabet)
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, string, int)>();

        foreach (var symbol in alphabet.Distinct())
            transitions.Add((initial, symbol.ToString(), final));

        return new Fsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }

    public static Fsa All(IEnumerable<char> alphabet)
        => FsaBuilder.FromSymbolSet(alphabet).Star();
}