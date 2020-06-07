/*  
    Classical bimachine
*/
using System;
using System.Collections.Generic;

[Serializable]
public class Bimachine
{
    public Bimachine(
        Dfsa left,
        Dfsa right,
        IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> output)
    {
        this.Left = left;
        this.Right = right;
        this.Output = output;
    }

    public Dfsa Left { get; }

    public Dfsa Right { get; }

    public IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> Output { get; }
}