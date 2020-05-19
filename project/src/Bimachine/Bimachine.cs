/*  
    Classical bimachine
*/
using System;
using System.Collections.Generic;

[Serializable]
public class Bimachine
{
    public Bimachine(
        Dfsa forward,
        Dfsa reverse,
        IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> output)
    {
        this.Left = forward;
        this.Right = reverse;
        this.Output = output;
    }

    public Dfsa Left { get; private set; }

    public Dfsa Right { get; private set; }

    public IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> Output { get; private set; }
}