public class Token
{
    public int Index { get; set; }

    public (int Start, int End) Position { get; set; }

    public string Text { get; set; }

    public string Type { get; set; }

    public override string ToString() => 
        $"[@{Index},{Position.Start}:{Position.End}='{Text}',<{Type}>]";
}