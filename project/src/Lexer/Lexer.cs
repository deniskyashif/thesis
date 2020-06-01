using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

[Serializable]
public class Lexer
{
    const char SoT = '\u0002';
    const char EoT = '\u0003';
    readonly IList<Rule> grammar;

    private Lexer(Bimachine bm, IList<Rule> grammar)
    {
        this.Bm = bm;
        this.grammar = grammar;
    }

    public Bimachine Bm { get; private set; }

    public InputStream Input { get; set; }

    public IEnumerable<Token> GetNextToken()
    {
        var rPath = this.Bm.Right.ReverseRecognitionPath(this.Input);

        if (rPath.Count != this.Input.Size + 1)
            throw new ArgumentException(
                $"Unrecognized input symbol. {this.Input.CharAt(this.Input.Size - rPath.Count)}");

        var leftState = this.Bm.Left.Initial;
        var token = new StringBuilder();
        var typeIndex = new StringBuilder();
        var tokenIndex = 0;
        var tokenStartPos = 0;

        for (this.Input.SetToStart(); !this.Input.IsExhausted; this.Input.MoveForward())
        {
            var ch = this.Input.Peek();
            var rightIndex = rPath.Count - 2 - this.Input.Pos;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!this.Bm.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized token '{token.ToString()+ch}'");

            token.Append(Bm.Output[triple]);

            if (token[token.Length - 1] == EoT)
            {
                token.Remove(token.Length - 1, 1);

                for (var i = 0; token[i] != SoT; i++)
                    typeIndex.Append(token[i]);

                // keep only the token text
                token.Remove(0, typeIndex.Length + 1);

                yield return new Token
                {
                    Index = tokenIndex,
                    Position = (tokenStartPos, this.Input.Pos),
                    Text = token.ToString(),
                    Type = this.grammar[int.Parse(typeIndex.ToString())].Name
                };

                token.Clear();
                typeIndex.Clear();
                tokenIndex++;
                tokenStartPos = this.Input.Pos + 1;
            }

            if (!this.Bm.Left.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = this.Bm.Left.Transitions[(leftState, ch)];
        }
    }

    public static Lexer Create(IList<Rule> grammar)
    {
        var tokenFsts = new List<Fst>();

        for (int i = 0; i < grammar.Count; i++)
        {
            var ruleFsa = new RegExp(grammar[i].Pattern).Automaton;
            // {<ε,TypeIndex SOT>} · Id(R) · {<ε,EOT>}
            var tokenFst = FstBuilder.FromWordPair(string.Empty, $"{i}{SoT}")
                .Concat(ruleFsa.Identity())
                .Concat(FstBuilder.FromWordPair(string.Empty, $"{EoT}"));

            tokenFsts.Add(tokenFst);
        }

        var unionTokenFst = tokenFsts.Aggregate((u, f) => u.Union(f)).PseudoMinimal();
        var alphabet = unionTokenFst.Transitions
            .Where(t => !string.IsNullOrEmpty(t.In))
            .Select(c => c.In.Single())
            .ToHashSet();

        var lmlFst = unionTokenFst.ToLmlRewriter(alphabet);
        var bm = lmlFst.ToBimachine(alphabet);

        return new Lexer(bm, grammar);
    }

    public void ExportToFile(string path)
    {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var formatter = new BinaryFormatter();

        formatter.Serialize(
            stream,
            new LexerExport 
            { 
                grammar = this.grammar, 
                bm = this.Bm.PseudoMinimal() 
            });
        stream.Close();
    }

    public static Lexer LoadFromFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var formatter = new BinaryFormatter();

        var exp = (LexerExport)formatter.Deserialize(stream);
        return new Lexer(exp.bm, exp.grammar);
    }

    [Serializable]
    class LexerExport
    {
        public IList<Rule> grammar;
        public Bimachine bm;
    }
}
