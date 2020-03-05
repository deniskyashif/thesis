using System;
using System.Collections.Generic;
using System.Text;

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

    public Dfsa Left { get; private set; }
    public Dfsa Right { get; private set; }
    public IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> Output { get; private set; }

    public string Process(string word)
    {
        var leftRun = this.Left.RecognitionPath(word);
        var rightRun = this.Right.RecognitionPath(Reverse(word));

        if (!(leftRun.Success && rightRun.Success))
            throw new ArgumentException("Unrecognized input.");

        var output = new StringBuilder();

        for (int i = 0; i < word.Length; i++)
        {
            var rightIndex = rightRun.Path.Count - 2 - i;
            var triple = (leftRun.Path[i], word[i], rightRun.Path[rightIndex]);
            output.Append(this.Output[triple]);
        }

        return output.ToString();
    }

    private static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
}