/*
    On the fly compilation of a regular expression to a 
    finite-state automaton using the "recursive descent" parsing technique. 
    It implements the following grammar:

    Exp: Term 
        | Term '|' Exp
    Term: Factor 
        | Factor Term
    Factor: Atom 
        | Atom MetaChar 
        | Atom '{' CharCount '}'
    Atom: Char 
        | '.' 
        | '(' Exp ')' 
        | '[' CharClass ']' 
        | '[' '^' CharClass ']'
    CharClass: CharClassItem 
        | CharClassItem CharClass
    CharClassItem: Char 
        | Char '-' Char
    CharCount: Integer 
        | Integer ',' 
        | Integer ',' Integer
    Integer: Digit 
        | Digit Integer
    Char: AnyCharExceptMeta 
        | '\' AnyChar

    AnyChar: alphabet
    MetaChar: '?' | '*' | '+'
    Digit: '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'
*/
using System;
using System.Collections.Generic;
using System.Linq;

public class RegExp2
{
    static readonly ISet<char> metaChars = new HashSet<char> { '?', '*', '+' };

    string pattern;
    int pos = 0;

    public RegExp2(string pattern)
    {
        this.pattern = pattern;
        this.Automaton = this.Expr();
    }

    public Pfsa Automaton { get; private set; }

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

    Pfsa Expr()
    {
        var term = this.Term();

        if (this.HasMoreChars() && this.Peek() == '|')
        {
            this.Eat('|');
            return term.Union(this.Expr());
        }

        return term;
    }

    Pfsa Term()
    {
        if (this.HasMoreChars() && this.Peek() != ')' && this.Peek() != '|')
            return this.Factor().Concat(this.Term());

        return PfsaBuilder.FromEpsilon();
    }

    Pfsa Factor()
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
                _ => throw new ArgumentException($"Unhandled meta character '{metaCh}' for {pattern} at pos {pos}.")
            };
        }

        if (this.HasMoreChars() && this.Peek() == '{')
        {
            var (min, max) = this.CharCount();
            var fsa = PfsaBuilder.FromEpsilon();

            for (var i = 0; i < min; i++)
                fsa = fsa.Concat(atom);

            if (max == -1)
                fsa = fsa.Concat(atom.Star());
            else
            {
                var optionalAtom = atom.Optional();

                for (var i = min; i < max; i++)
                    fsa = fsa.Concat(optionalAtom);
            }

            return fsa;
        }

        return atom;
    }

    Pfsa Atom()
    {
        if (this.Peek() == '.')
        {
            this.Eat('.');
            return PfsaBuilder.Any();
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
            Pfsa @class;

            if (this.Peek() == '^')
            {
                this.Eat('^');
                throw new NotSupportedException();
                // @class = this.CharClass().Complement();
            }
            else @class = this.CharClass();

            this.Eat(']');

            return @class;
        }

        return this.Char();
    }

    Pfsa CharClass()
    {
        var setItem = this.CharClassItem();

        if (this.HasMoreChars() && this.Peek() != ']')
            return setItem.Union(this.CharClass());

        return setItem;
    }

    Pfsa CharClassItem()
    {
        if (this.Peek() == '\\')
            this.Eat('\\');

        var from = this.Next();
        var symbols = new HashSet<char> { from };

        if (this.Peek() == '-')
        {
            this.Eat('-');

            // if a set ends with a '-' then treat it as a character
            if (this.Peek() == ']')
                symbols.Add('-');
            else
                return PfsaBuilder.FromCharRange(from, to: this.Next());
        }

        return PfsaBuilder.FromSymbolSet(symbols);
    }

    Pfsa Char()
    {
        if (this.Peek() == '\\')
        {
            this.Eat('\\');
            var ch = this.Next();

            return PfsaBuilder.FromSymbol(ch);
        }
        else
        {
            var ch = this.Next();

            if (metaChars.Contains(ch))
                throw new ArgumentException($"Unescaped meta character {ch} for {pattern} at pos {pos}");

            return PfsaBuilder.FromSymbol(ch);
        }
    }

    (int Min, int Max) CharCount()
    {
        this.Eat('{');
        var min = int.Parse(this.Integer());
        var max = min; // exact match count

        if (this.Peek() == ',')
        {
            this.Eat(',');
            max = -1; // no upper bound
        }
        if (this.Peek() != '}')
        {
            max = int.Parse(this.Integer());

            if (min > max)
                throw new ArgumentException($"Invalid count [{min}, {max}].");
        }
        this.Eat('}');

        return (min, max);
    }

    string Integer()
    {
        var digit = this.Digit();

        if (char.IsDigit(this.Peek()))
            return digit + this.Integer();

        return digit.ToString();
    }

    char Digit()
    {
        var digit = this.Next();

        if (!char.IsDigit(digit))
            throw new ArgumentException($"Invalid digit '{digit}'.");

        return digit;
    }
}
