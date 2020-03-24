using System;
using System.Collections.Generic;
using System.Linq;

public class RegExp
{
    static readonly ISet<char> allChars =
        Enumerable.Range(char.MinValue, char.MaxValue)
            .Select(Convert.ToChar)
            .Where(c => !char.IsControl(c))
            .ToHashSet();
    static readonly ISet<char> metaChars = new HashSet<char> { '?', '*', '+' };
    static readonly Fsa allCharsFsa = FsaBuilder.FromSymbolSet(allChars);

    string originalPattern;
    string pattern;
    int pos = 0;

    public RegExp(string pattern)
    {
        this.pattern = pattern;
        this.Automaton = this.Expr();
    }

    public Fsa Automaton { get; private set; }

    public bool Match(string word) => this.Automaton.Recognize(word);

    char Peek() => this.pattern[this.pos];

    bool HasMoreChars() => this.pos < this.pattern.Length;

    bool IsMetaChar(char ch) => metaChars.Contains(ch);

    void Eat(char ch)
    {
        if (this.Peek() != ch)
            throw new ArgumentException($"Expected '{ch}' but got {this.Peek()} instead.");

        this.pos++;
    }

    char Next()
    {
        var ch = this.Peek();
        this.Eat(ch);

        return ch;
    }

    Fsa Expr()
    {
        var term = this.Term();

        if (this.HasMoreChars() && this.Peek() == '|')
        {
            this.Eat('|');
            return term.Union(this.Expr());
        }

        return term;
    }

    Fsa Term()
    {
        if (this.HasMoreChars() && this.Peek() != ')' && this.Peek() != '|')
            return this.Factor().Concat(this.Term());

        return FsaBuilder.FromEpsilon();
    }

    Fsa Factor()
    {
        var atom = this.Atom();

        if (this.HasMoreChars() && this.IsMetaChar(this.Peek()))
        {
            var metaCh = this.Next();

            return metaCh switch
            {
                '?' => atom.Optional(),
                '*' => atom.Star(),
                '+' => atom.Plus(),
                _ => throw new ArgumentException($"Unhandled meta character '{metaCh}'.")
            };
        }

        return atom;
    }

    Fsa Atom()
    {
        if (this.Peek() == '.')
        {
            this.Eat('.');
            return allCharsFsa;
        }

        if (this.Peek() == '(')
        {
            this.Eat('(');
            var exp = this.Expr();
            this.Eat(')');

            return exp;
        }

        if (this.Peek() == '[')
        {
            this.Eat('[');
            var @class = this.CharClass();
            this.Eat(']');

            return @class;
        }

        return this.Char();
    }

    Fsa CharClass()
    {
        var range = this.CharRange();

        if (this.HasMoreChars() && this.Peek() != ']')
            return range.Union(this.CharClass());

        return range;
    }

    Fsa CharRange()
    {
        var from = this.Next();
        var fsa = FsaBuilder.FromWord(from.ToString());

        if (this.Peek() == '-')
        {
            this.Eat('-');
            var to = this.Next();

            if (from > to)
                throw new ArgumentException($"Invalid character range '{from}'-'{to}'.");

            for (var i = from + 1; i <= to; i++)
                fsa = fsa.Union(FsaBuilder.FromWord(((char)i).ToString()));
        }

        return fsa;
    }

    Fsa Char()
    {
        if (this.Peek() == '\\')
        {
            this.Eat('\\');
            var ch = this.Next();

            if (!allChars.Contains(ch))
                throw new ArgumentException($"Invalid character {ch}");

            return FsaBuilder.FromWord(ch.ToString());
        }
        else
        {
            var ch = this.Next();

            if (metaChars.Contains(ch))
                throw new ArgumentException($"Unescaped meta character {ch}");

            return FsaBuilder.FromWord(ch.ToString());
        }
    }
}