/*  
    Classical bimachine
*/
using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class Bimachine
{
    public Bimachine(
        Dfsa left,
        Dfsa right,
        IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> output)
    {
        this.Left = left;
        this.Right = right;
        this.Output = output;
    }

    public Dfsa Left { get; }
    public Dfsa Right { get; }
    public IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> Output { get; }

    public string Process(string input)
    {
        var rPath = this.Right.RecognitionPathRToL(input);

        if (rPath.Count != input.Length + 1)
            throw new ArgumentException($"Unrecognized input. {input[input.Length - rPath.Count]}");

        var output = new StringBuilder();
        var leftState = this.Left.Initial;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            var rightIndex = rPath.Count - 2 - i;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!this.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized input. {ch}");

            output.Append(this.Output[triple]);

            if (!this.Left.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = this.Left.Transitions[(leftState, ch)];
        }

        return output.ToString();
    }
}