using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

[Serializable]
public class Lexer
{
    const char SoT = '\0';
    readonly IList<Rule> grammar;

    Lexer(Bimachine bm, IList<Rule> grammar)
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
        var tokenIndex = 0;
        var tokenStartPos = 0;

        for (this.Input.SetToStart(); !this.Input.IsExhausted; this.Input.MoveForward())
        {
            var ch = this.Input.Peek();
            var rightIndex = rPath.Count - 2 - this.Input.Pos;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!this.Bm.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized token '{token.ToString() + ch}'");

            token.Append(Bm.Output[triple]);

            if (token[token.Length - 1] > RegExp.AlphabetMax)
            {
                if (token[0] != SoT)
                {
                    var unrecognizedToken = new StringBuilder();

                    for (var k = 0; k < token.Length && token[k] != SoT; k++)
                        unrecognizedToken.Append(token[k]);

                    throw new ArgumentException($"Invalid token \"{unrecognizedToken.ToString()}\"");
                }    

                var typeIndex = token[token.Length - 1] - RegExp.AlphabetMax - 1;
                token.Remove(0, 1); // remove the start of token marker
                token.Remove(token.Length - 1, 1); // remove the end of token marker

                yield return new Token
                {
                    Index = tokenIndex,
                    Position = (tokenStartPos, this.Input.Pos),
                    Text = token.ToString(),
                    Type = this.grammar[int.Parse(typeIndex.ToString())].Name
                };

                token.Clear();
                tokenIndex++;
                tokenStartPos = this.Input.Pos + 1;
            }

            if (!this.Bm.Left.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = this.Bm.Left.Transitions[(leftState, ch)];
        }

        if (token.Length > 0)
            throw new ArgumentException($"Unrecognized token '{token.ToString()}'");
    }

    public static Lexer Create(IList<Rule> grammar)
    {
        var tokenFsts = new List<Fst>();

        for (int i = 0; i < grammar.Count; i++)
        {
            var ruleFsa = new RegExp(grammar[i].Pattern).Automaton;
            var eot = (char)(RegExp.AlphabetMax + 1 + i);
            // { <ε,SoT> } · Id(L(R)) · { <ε,EoT_R> }
            var tokenFst = FstBuilder.FromWordPair(string.Empty, SoT.ToString())
                .Concat(ruleFsa.Identity())
                .Concat(FstBuilder.FromWordPair(string.Empty, eot.ToString()));

            tokenFsts.Add(tokenFst);
        }

        var unionTokenFst = tokenFsts.Aggregate((u, f) => u.Union(f));
        var alphabet = unionTokenFst.InputAlphabet
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(c => c.Single())
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
