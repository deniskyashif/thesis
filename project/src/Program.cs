using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main()
    {
        var grammar = GetGrammar();

        var sw = new Stopwatch();
        sw.Start();
        
        Console.WriteLine("Constructing the lexer.");

        var lexer = new Lexer(grammar);
        sw.Stop();
        Console.WriteLine($"Lexer constructed in {sw.Elapsed}");

        lexer.bm.ExportToFile("bmout");
        lexer.bm.PseudoMinimal().ExportToFile("bmoutmin");
        return;

        while (true)
        {
            // var json = "{ \"key\": -4.040 }";
            var json = File.ReadAllText("data.json");
            // Console.WriteLine("File read.");

            // Console.WriteLine(json + '\n');

            sw.Restart();
            foreach (var token in lexer.GetNextToken(json))
            {
                // if (token.Type == "WS") continue;
                // Console.WriteLine(token);
            }
            sw.Stop();
            Console.WriteLine($"Tokenization completed in {sw.Elapsed}");

            break;
        }
    }

    private static Rule[] GetGrammar()
    {
        var intRe = "0|[1-9][0-9]*";
        var expRe = $"[Ee][+\\-]?({intRe})";
        var hexRe = "[0-9a-fA-F]";
        var unicodeRe = $"u{hexRe}{hexRe}{hexRe}{hexRe}";
        var escRe = "\\\\([\"/bfnrt]|" + unicodeRe + ")";
        var safeCodepointRe = "[^\"\u0000-\u001F\\\\]";

        var grammar = new[]
        {
            new Rule(@"\{", "OBJ_START"),
            new Rule(@"\}", "OBJ_END"),
            new Rule(@"\[", "ARR_START"),
            new Rule(@"\]", "ARR_END"),
            new Rule(":", "PAIR_DELIMITER"),
            new Rule(",", "COMMA"),
            new Rule($"\"({escRe}|{safeCodepointRe})*\"", "STRING"),
            new Rule($"-?({intRe})(\\.[0-9]+)?({expRe})?", "NUMBER"),
            new Rule("true|false", "BOOLEAN"),
            new Rule("[ \t\n\r]+", "WS"),
        };
        return grammar;
    }
}
