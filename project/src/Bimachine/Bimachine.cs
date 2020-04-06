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
        this.Forward = forward;
        this.Reverse = reverse;
        this.Output = output;
    }

    public Dfsa Forward { get; private set; }

    public Dfsa Reverse { get; private set; }

    public IReadOnlyDictionary<(int Lstate, char Symbol, int Rstate), string> Output { get; private set; }
}