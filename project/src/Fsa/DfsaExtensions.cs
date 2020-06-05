using System.Collections.Generic;

public static class DfsaExtensions
{
    public static IList<int> RecognitionPathRToL(this Dfsa automaton, string input) =>
        ReverseRecognitionPath(automaton, new InputStream(input));

    public static IList<int> ReverseRecognitionPath(this Dfsa automaton, InputStream input)
    {
        var current = automaton.Initial;
        var path = new List<int> { current };

        for (input.SetToEnd(); !input.IsExhausted; input.MoveBackward())
        {
            var symbol = input.Peek();

            if (automaton.Alphabet.Contains(symbol))
            {
                if (!automaton.Transitions.ContainsKey((current, symbol)))
                    return path;

                current = automaton.Transitions[(current, symbol)];
                path.Add(current);
            }
            else
            {
                if (!automaton.Transitions.ContainsKey((current, Fsa.AnySymbolOutsideAlphabet)))
                    return path;

                current = automaton.Transitions[(current, Fsa.AnySymbolOutsideAlphabet)];
                path.Add(current);
            }
        } 

        return path;
    }
}