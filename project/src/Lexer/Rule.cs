using System;

public class Rule
{
    public Rule(string pattern, string name)
    {
        this.Pattern = pattern;
        this.Name = name;
    }

    public string Pattern { get; private set; }
    
    public string Name { get; private set; }
}