using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

[Serializable]
public class Lexer
{
    const char StartOfToken = '\u0002';
    const char EndOfToken = '\u0003';

    private Lexer(Bimachine bm) =>
        this.Bimachine = bm;

    public Bimachine Bimachine { get; private set; }

    public IEnumerable<Token> GetNextToken(string input)
    {
        var rPath = this.Bimachine.Right.RecognitionPathRToL(input);

        if (rPath.Count != input.Length + 1)
            throw new ArgumentException($"Unrecognized input. {input[input.Length - rPath.Count]}");

        var leftState = Bimachine.Left.Initial;
        var token = new StringBuilder();
        var tokenIndex = 0;
        var tokenStartPos = 0;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            var rightIndex = rPath.Count - 2 - i;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!Bimachine.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized input. {ch}");

            var outStr = Bimachine.Output[triple];
            token.Append(outStr);

            if (token[token.Length - 1] == EndOfToken)
            {
                token.Remove(token.Length - 1, 1);
                var type = new StringBuilder();

                for (var k = 0; k < token.Length && token[k] != StartOfToken; k++)
                    type.Append(token[k]);

                // keep only the token text
                token.Remove(0, type.Length + 1);

                yield return new Token
                {
                    Index = tokenIndex,
                    Position = (tokenStartPos, i),
                    Text = token.ToString(),
                    Type = type.ToString()
                };

                token.Clear();
                tokenIndex++;
                tokenStartPos = i + 1;
            }

            if (!Bimachine.Left.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = Bimachine.Left.Transitions[(leftState, ch)];
        }
    }

    public static Lexer Create(IList<Rule> grammar)
    {
        Console.WriteLine("Constructing the token transducers.");

        var tokenFst = grammar
            .Select(ToTokenFst)
            .Aggregate((u, f) => u.Union(f));
        var alphabet = tokenFst.Transitions
            .Where(t => !string.IsNullOrEmpty(t.In))
            .Select(t => t.In.Single())
            .ToHashSet();

        Console.WriteLine("Constructing the combined LML token transducer.");
        var lml = tokenFst.ToLmlRewriter(alphabet);

        Console.WriteLine("Constructing the bimachine.");
        var bm = lml.ToBimachine(alphabet);

        return new Lexer(bm);
    }

    static Fst ToTokenFst(Rule rule)
    {
        var ruleFsa = new RegExp(rule.Pattern).Automaton
            .Determinize()
            .Minimal();

        var ruleFst = FstBuilder.FromWordPair(string.Empty, $"{rule.Name}{StartOfToken}")
            .Concat(ruleFsa.Identity())
            .Concat(FstBuilder.FromWordPair(string.Empty, $"{EndOfToken}"));

        return ruleFst;
    }

    public void ExportToFile(string path)
    {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, this);
        stream.Close();
    }

    public static Lexer LoadFromFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var formatter = new BinaryFormatter();

        return (Lexer)formatter.Deserialize(stream);
    }
}
