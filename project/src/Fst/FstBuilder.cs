public static class FstBuilder
{
    public static Fst FromWordPair(string input, string output)
        => new Fst(
            new[] { 0, 1 },
            new[] { 0 },
            new[] { 1 },
            new[] { (0, input, output, 1) });
}