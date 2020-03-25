using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

public static class BimachineExtensions
{
    public static string Process(this Bimachine bm, string input)
    {
        var (rSuccess, rPath) = bm.Right.RecPathRightToLeft(input);

        if (!rSuccess)
            throw new ArgumentException($"Unrecognized input. {input[rPath.Count - 1]}");

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

    static void Export(this Bimachine bm, string path)
    {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, bm);  
        stream.Close();
    }

    static Bimachine Load(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var formatter = new BinaryFormatter();  

        return (Bimachine)formatter.Deserialize(stream);
    }
}