using System.Collections.Generic;

public class Bimachine
{
    private Dfsa left;
    private Dfsa right;
    private IDictionary<(int Lstate, char Symbol, int Rstate), string> output;

    public Bimachine(
        Dfsa left, 
        Dfsa right,
        IDictionary<(int Lstate, char Symbol, int Rstate), string> output)
    {
        this.left = left;
        this.right = right;
        this.output = output;
    }

    public string Process(string word)
    {
        return string.Empty;
    }
}