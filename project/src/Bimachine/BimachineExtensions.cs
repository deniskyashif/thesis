using System;
using System.Text;

public static class BimachineExtensions
{
    // Process an input string throught the bimachine to get the corresponding output string.
    public static string Process(this Bimachine bm, string input)
    {
        var rPath = bm.Right.RecognitionPathRToL(input);

        if (rPath.Count != input.Length + 1)
            throw new ArgumentException($"Unrecognized input. {input[input.Length - rPath.Count]}");

        var output = new StringBuilder();
        var leftState = bm.Left.Initial;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            var rightIndex = rPath.Count - 2 - i;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!bm.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized input. {ch}");

            output.Append(bm.Output[triple]);

            if (!bm.Left.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = bm.Left.Transitions[(leftState, ch)];
        }

        return output.ToString();
    }
}