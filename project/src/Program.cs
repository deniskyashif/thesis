using System;

class Program
{
    static void Main()
    {
        var lexer = Lexer.Create(new[]
        {
            new Rule("[0-9]+\\.?[0-9]*", "NUM"),
            new Rule("[+*/-]", "OP"),
            new Rule("=", "EQ"),
            new Rule("[ \t\r\n]", "WS")
        });

        lexer.Input = new InputStream("3.14+1.86=5");

        foreach (Token token in lexer.GetNextToken())
            Console.WriteLine(token);
    }
}