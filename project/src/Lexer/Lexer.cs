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
        this.Bm = bm;

    public Bimachine Bm { get; private set; }

    public InputStream Input { get; set; }

    public IEnumerable<Token> GetNextToken()
    {
        var rPath = this.Bm.Reverse.RecognitionPathRToL(this.Input);

        if (rPath.Count != this.Input.Size + 1)
            throw new ArgumentException($"Unrecognized input. {this.Input.CharAt(this.Input.Size - rPath.Count)}");

        var leftState = this.Bm.Forward.Initial;
        var token = new StringBuilder();
        var type = new StringBuilder();
        var tokenIndex = 0;
        var tokenStartPos = 0;

        for (this.Input.SetToStart(); !this.Input.IsExhausted; this.Input.MoveForward())
        {
            var ch = this.Input.Peek();
            var rightIndex = rPath.Count - 2 - this.Input.Pos;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!Bm.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized input. {ch}");

            var outStr = Bm.Output[triple];
            token.Append(outStr);

            if (token[token.Length - 1] == EndOfToken)
            {
                token.Remove(token.Length - 1, 1);

                for (var k = 0; k < token.Length && token[k] != StartOfToken; k++)
                    type.Append(token[k]);

                // keep only the token text
                token.Remove(0, type.Length + 1);

                yield return new Token
                {
                    Index = tokenIndex,
                    Position = (tokenStartPos, this.Input.Pos),
                    Text = token.ToString(),
                    Type = type.ToString()
                };

                token.Clear();
                type.Clear();
                tokenIndex++;
                tokenStartPos = this.Input.Pos + 1;
            }

            if (!this.Bm.Forward.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = this.Bm.Forward.Transitions[(leftState, ch)];
        }
    }

    public static Lexer Create(IList<Rule> grammar)
    {
        // Console.WriteLine("Constructing the token transducers.");

        var tokenFst = grammar
            .Select(ToTokenFst)
            .Aggregate((u, f) => u.Union(f));

        var alphabet = tokenFst.Transitions
            .Where(t => !string.IsNullOrEmpty(t.In))
            .Select(t => t.In.Single())
            .ToHashSet();

        // Console.WriteLine("Constructing the combined LML token transducer.");
        var lml = tokenFst.ToLmlRewriter(alphabet);

        // Console.WriteLine("Constructing the bimachine.");
        var bm = lml.ToBimachine(alphabet);

        return new Lexer(bm);
    }

    static Fst ToTokenFst(Rule rule)
    {
        var ruleFsa = new RegExp(rule.Pattern).Automaton;

        // <ε,Type SoT> · Id(R) · <ε,EoT>
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
