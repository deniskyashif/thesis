using System;
using System.Collections.Generic;
using System.Linq;

public class RegExp
{
    static readonly char[] digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    static readonly char[] lowerCaseLetters = new char[]
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    };
    static readonly char[] upperCaseLetters = new char[]
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    };
    static readonly char[] symbols = new char[]
    {
        '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':',
        ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_', '`', '{', '|', '}', '~',
    };
    static readonly ISet<char> allChars 
        = symbols.Concat(lowerCaseLetters).Concat(upperCaseLetters).Concat(digits).ToHashSet();
    static readonly ISet<char> metaChars = new HashSet<char> { '?', '*', '+' };

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

    bool HasMore() => this.pos < this.pattern.Length;

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

        if (this.HasMore() && this.Peek() == '|')
        {
            this.Eat('|');
            return term.Union(this.Expr());
        }

        return term;
    }

    Fsa Term()
    {
        if (this.HasMore() && this.Peek() != ')' && this.Peek() != '|')
            return this.Factor().Concat(this.Term());

        return FsaBuilder.FromEpsilon();
    }

    Fsa Factor()
    {
        var atom = this.Atom();

        if (this.HasMore() && metaChars.Contains(this.Peek()))
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
        if (this.Peek() == '(')
        {
            this.Eat('(');
            var exp = this.Expr();
            this.Eat(')');

            return exp;
        }

        return this.Char();
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