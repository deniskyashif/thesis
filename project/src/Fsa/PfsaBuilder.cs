using System;
using System.Collections.Generic;

public static class PfsaBuilder
{
    public static Pfsa FromEpsilon() => FromWord(string.Empty);

    public static Pfsa FromWord(string word)
    {
        var state = 0;
        var states = new List<int> { state };
        var initialStates = new int[] { state };
        var transitions = new List<(int, Func<char, bool>, int)>();

        foreach (var symbol in word)
        {
            var next = state + 1;
            transitions.Add((state, c => c == symbol, next));
            states.Add(next);
            state = next;
        }

        return new Pfsa(states, initialStates, new int[] { state }, transitions);
    }

    public static Pfsa FromSymbol(char symbol) => FromWord(symbol.ToString());

    public static Pfsa FromSymbolSet(ISet<char> alphabet)
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, Func<char, bool>, int)>();
        transitions.Add((initial, c => alphabet.Contains(c), final));

        return new Pfsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }

    public static Pfsa All()
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, Func<char, bool>, int)>();
        transitions.Add((initial, c => true, final));

        return new Pfsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }

    public static Pfsa AllExcept(ISet<char> symbols)
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, Func<char, bool>, int)>();
        transitions.Add((initial, c => !symbols.Contains(c), final));

        return new Pfsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }
}