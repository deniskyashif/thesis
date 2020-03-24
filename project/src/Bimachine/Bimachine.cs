/*  
    Classical bimachine
*/
using System;
using System.Collections.Generic;
using System.Linq;
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

    public Dfsa Left { get; private set; }

    public Dfsa Right { get; private set; }

    public IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> Output { get; private set; }

    public string Process(string word)
    {
        var rightRun = this.RecognitionPathRToL(word);

        if (!rightRun.Success)
            throw new ArgumentException($"Unrecognized input. {word[rightRun.Path.Count - 1]}");

        var leftRun = this.RecognitionPathLToR(word);

        if (!leftRun.Success) 
            throw new ArgumentException($"Unrecognized input. {word[leftRun.Path.Count - 1]}");

        var output = new StringBuilder();

        for (int i = 0; i < word.Length; i++)
        {
            var rightIndex = rightRun.Path.Count - 2 - i;
            var triple = (leftRun.Path[i], word[i], rightRun.Path[rightIndex]);
            output.Append(this.Output[triple]);
        }

        return output.ToString();
    }

    (bool Success, IList<int> Path) RecognitionPathLToR(string word)
    {
        var curr = this.Left.Initial;
        var path = new List<int> { curr };

        foreach (var symbol in word)
        {
            if (!this.Left.Transitions.ContainsKey((curr, symbol)))
                return (false, path);

            curr = this.Left.Transitions[(curr, symbol)];
            path.Add(curr);
        }

        return (this.Left.Final.Contains(curr), path);
    }

    (bool Success, IList<int> Path) RecognitionPathRToL(string word)
    {
        var current = this.Right.Initial;
        var path = new List<int> { current };

        for (var i = word.Length - 1; i >= 0; i--)
        {
            var symbol = word[i];

            if (!this.Right.Transitions.ContainsKey((current, symbol)))
                return (false, path);

            current = this.Right.Transitions[(current, symbol)];
            path.Add(current);
        }

        return (this.Right.Final.Contains(current), path);
    }
}