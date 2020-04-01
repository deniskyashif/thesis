/*
    C# Lexical specification:
    https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#lexical-analysis
*/
using System.Threading.Tasks;

class CSharpLexer
{
    public static Lexer Create()
    {
        var new_line = "\r|\n|\r\n";
        var input_character = "[^\r\n]";
        var whitespace = "\u0009|\u000B|\u000C";

        var single_line_comment = $"//{input_character}*";
        var asterisk = "\\*";
        var not_slash_or_asterisk = $"[^{asterisk}/]";
        var delimited_comment_section = $"/|{asterisk}*{not_slash_or_asterisk}";
        var delimited_comment = $"/\\*{delimited_comment_section}*{asterisk}+/";
        var comment = $"{single_line_comment}|{delimited_comment}";

        var keyword = string.Join('|', new[] {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
            "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
            "explicit' ", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",  "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",  "null",
            "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
            "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        });

        var boolean_literal = "true|false";

        var decimal_digit = "[0-9]";
        var hex_digit = "[0-9a-fA-F]";
        var unicode_escape_sequence = $"\\\\u{hex_digit}{hex_digit}{hex_digit}{hex_digit}";

        var integer_type_suffix = "U|u|L|l|UL|Ul|uL|ul|LU|Lu|lU|lu";
        var hexadecimal_integer_literal = $"0x{hex_digit}+{integer_type_suffix}?|0X${hex_digit}+{integer_type_suffix}?";
        var decimal_integer_literal = $"{decimal_digit}+{integer_type_suffix}?";
        var integer_literal = $"{decimal_integer_literal}|{hexadecimal_integer_literal}";
        var sign = "\\+|-";
        var exponent_part = $"e({sign})?({decimal_digit})+|E({sign})?({decimal_digit})+";
        var real_type_suffix = "F|f|D|d|M|m";
        var real_literal = $"(({decimal_digit})+\\.({decimal_digit})+({exponent_part})?({real_type_suffix})?\\.({decimal_digit})+({exponent_part})?({real_type_suffix})?)"
            + $"|(({decimal_digit})+({exponent_part})({real_type_suffix})?)"
            + $"|(({decimal_digit})+({real_type_suffix}))";

        var single_character = "[^'\r\n\\]";
        var simple_escape_sequence = "\\'|\\\"|\\\\|\\0|\\a|\\b|\\f|\\n|\\r|\\t|\\v";
        var hexadecimal_escape_sequence = $"\\x{hex_digit}{hex_digit}?{hex_digit}?{hex_digit}?";
        var character = $"({single_character})|({simple_escape_sequence})|({hexadecimal_escape_sequence})|({unicode_escape_sequence})";
        var character_literal = $"'{character}'";

        var quote_escape_sequence = "\"\"";
        var single_verbatim_string_literal_character = "[^\"]";
        var verbatim_string_literal_character = $"({single_verbatim_string_literal_character})|({quote_escape_sequence})";
        var verbatim_string_literal = $"@\"{verbatim_string_literal_character}*\"";
        var single_regular_string_literal_character = "[^/r/n\"\\\\]";
        var regular_string_literal_character = $"({single_regular_string_literal_character})|({simple_escape_sequence})|({hexadecimal_escape_sequence})|({unicode_escape_sequence})";
        var regular_string_literal = $"\"{regular_string_literal_character}*\"";
        // var interpolated_string_literal = ...
        var string_literal = $"({regular_string_literal})|({verbatim_string_literal})";
        var null_literal = "null";

        var operator_or_punctuator = string.Join('|', new[] {
            "\\{", "\\}", "\\[", "\\]", "\\(", "\\)", "\\.", ",", ":", ";",
            "\\+", "-", "\\*", "/", "%", "&", "\\|", "\\^", "!", "~",
            "=", "<", ">", "\\?", "\\?\\?", "::", "\\+\\+", "\\-\\-", "&&", "\\|\\|",
            "->", "==", "!=", "<=", ">=", "\\+=", "-=", "\\*=", "/=", "%=",
            "&=", "\\|=", "^=", "<<", "<<=", "=>"
        });
        var right_shift = ">>";
        var right_shift_assignment = ">>=";

        var literal = $"({boolean_literal})|({integer_literal})|({real_literal})|({character_literal})|({string_literal})|({null_literal})";

        var letter_character = "[a-zA-Z]";
        var decimal_digit_character = "[0-9]";
        var identifier_part_character = $"({letter_character})|({decimal_digit_character})"; // |{connecting_character}|{combining_character}|{formatting_character}";
        var identifier_start_character = $"letter_character|_";
        var identifier_or_keyword = $"({identifier_start_character})({identifier_part_character})*";
        // var available_identifier = "";
        var identifier = $"@?{identifier_or_keyword}"; // |{available_identifier}

        var token = $"({identifier})|({keyword})|({integer_literal})|({real_literal})|({character_literal})|({string_literal})|({operator_or_punctuator})"; // TODO add {interpolated_string_literal}

        var input_element = $"({whitespace})|({comment})|({token})";
        var input_section_part = $"({input_element})*({new_line})"; // TODO ADD |pp_directive";
        var input_section = $"({input_section_part})+";
        var input = $"({input_section})?";

        var grammar = new[]
        {
            new Rule(identifier, "ID"),
            new Rule(keyword, "KEYWORD"),
            new Rule(integer_literal, "INTEGER_LITERAL"),
            new Rule(real_literal, "REAL_LITERAL"),
            new Rule(character_literal, "CHAR_LITERAL"),
            new Rule(string_literal, "STRING_LITERAL"),
            new Rule(operator_or_punctuator, "OP_OR_PUNC"),
            new Rule(whitespace, "WS"),
            new Rule(new_line, "NEW_LINE"),
        };

        return Lexer.Create(grammar);
    }
}