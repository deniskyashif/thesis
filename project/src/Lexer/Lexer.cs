using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Lexer
{
    const char StartOfToken = '\u0002';
    const char EndOfToken = '\u0003';
    private readonly ICollection<Rule> grammar;
    private readonly Bimachine bm;

    public Lexer(ICollection<Rule> grammar)
    {
        this.grammar = grammar;
        this.bm = this.InitBimachine();
    }

    public IEnumerable<Lexeme> GetNextToken(string input)
    {
        var (rSuccess, rPath) = this.bm.Right.RecPathRightToLeft(input);

        if (!rSuccess)
            throw new ArgumentException($"Unrecognized input. {input[rPath.Count - 1]}");

        var leftState = bm.Left.Initial;
        var token = new StringBuilder();
        var tokenIndex = 0;
        var tokenStartPos = 0;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            var rightIndex = rPath.Count - 2 - i;
            var triple = (leftState, ch, rPath[rightIndex]);

            if (!bm.Output.ContainsKey(triple))
                throw new ArgumentException($"Unrecognized input. {ch}");

            var outStr = bm.Output[triple];
            token.Append(outStr);

            if (token[token.Length - 1] == EndOfToken)
            {
                token.Remove(token.Length - 1, 1);
                var type = new StringBuilder();

                for (var k = 0; k < token.Length && token[k] != StartOfToken; k++)
                    type.Append(token[k]);

                // keep only the token text
                token.Remove(0, type.Length + 1);

                yield return new Lexeme
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

            if (!bm.Left.Transitions.ContainsKey((leftState, ch)))
                throw new ArgumentException($"Unrecognized input. {ch}");

            leftState = bm.Left.Transitions[(leftState, ch)];
        }
    }

    Bimachine InitBimachine()
    {
        var tokenFst = this.grammar
            .Select(ToTokenFst)
            .Aggregate((u, f) => u.Union(f));

        // TODO: Possible bug with multuletter transitions
        var alphabet = tokenFst.Transitions
            .Where(t => !string.IsNullOrEmpty(t.In))
            .Select(t => t.In.Single())
            .ToHashSet();

        var min = tokenFst.PseudoMinimal();
        var lml = min.ToLmlRewriter(alphabet);
        var bm = lml.ToBimachine(alphabet);

        return bm;
    }

    Fst ToTokenFst(Rule rule)
    {
        var ruleFsa = new RegExp(rule.Pattern).Automaton;
        var ruleFst = FstBuilder.FromWordPair(string.Empty, $"{rule.Name}{StartOfToken}")
            .Concat(ruleFsa.Identity())
            .Concat(FstBuilder.FromWordPair(string.Empty, $"{EndOfToken}"));

        return ruleFst;
    }
}