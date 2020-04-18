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
        var transitions = new List<(int, Range, int)>();

        foreach (var symbol in word)
        {
            var next = state + 1;
            transitions.Add((state, new Range(symbol), next));
            states.Add(next);
            state = next;
        }

        return new Pfsa(states, initialStates, new int[] { state }, transitions);
    }

    public static Pfsa FromSymbol(char symbol) => FromWord(symbol.ToString());

    public static Pfsa FromSymbolSet(ISet<char> symbols)
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, Range, int)>();

        foreach (var ch in symbols)
            transitions.Add((initial, new Range(ch), final));

        return new Pfsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }

    public static Pfsa Any()
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, Range, int)>();
        transitions.Add((initial, Range.All, final));

        return new Pfsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }

    public static Pfsa FromCharRange(char from, char to)
    {
        if (from > to)
            throw new ArgumentException($"Invalid character range '{from}'-'{to}'.");
        
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, Range, int)>();
        transitions.Add(
            (initial, new Range(from, to), final));

        return new Pfsa(
            states: new int[] { initial, final },
            initial: new int[] { initial },
            final: new int[] { final },
            transitions);
    }
}