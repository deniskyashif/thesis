using System;
using System.Collections.Generic;
using System.Linq;

public static class FstBuilder
{
    static int NewState(IReadOnlyCollection<int> states) => states.Count;

    public static Fst Remap(Fst fst, IReadOnlyCollection<int> states)
    {
        var k = states.Count;

        return new Fst(
            fst.States.Select(s => s + k),
            fst.Initial.Select(s => s + k),
            fst.Final.Select(s => s + k),
            fst.Transitions.Select(t => (t.From + k, t.In, t.Out, t.To + k)));
    }
    public static Fst FromWordPair(string input, string output)
        => new Fst(
            new[] { 0, 1 },
            new[] { 0 },
            new[] { 1 },
            new[] { (0, input, output, 1) });

    public static Fst Union(Fst first, Fst second)
    {
        second = Remap(second, first.States);

        return new Fst(
            first.States.Concat(second.States),
            first.Initial.Concat(second.Initial),
            first.Final.Concat(second.Final),
            first.Transitions.Concat(second.Transitions));
    }

    public static Fst Concat(Fst first, Fst second)
    {
        second = Remap(second, first.States);
        var transitions = first.Transitions
            .Concat(second.Transitions)
            .Concat(first.Final
                .SelectMany(f1 =>
                    second.Initial.Select(i2 => (f1, string.Empty, string.Empty, i2))));

        return new Fst(
            first.States.Concat(second.States),
            first.Initial,
            second.Final,
            transitions);
    }

    public static Fst Star(Fst fst)
    {
        var initial = new[] { NewState(fst.States) };
        var transitions = fst.Transitions
            .Concat(fst.Initial.Select(i => (initial[0], string.Empty, string.Empty, i)))
            .Concat(fst.Final.Select(f => (f, string.Empty, string.Empty, initial[0])));

        return new Fst(
            fst.States.Concat(initial),
            initial,
            fst.Final.Concat(initial),
            transitions);
    }

    public static Fst Plus(Fst fst)
    {
        var initial = new[] { NewState(fst.States) };
        var transitions = fst.Transitions
            .Concat(fst.Initial.Select(i => (initial[0], string.Empty, string.Empty, i)))
            .Concat(fst.Final.Select(f => (f, string.Empty, string.Empty, initial[0])));

        return new Fst(
            fst.States.Concat(initial),
            initial,
            fst.Final,
            transitions);
    }

    public static Fst Option(Fst fst)
    {
        var newState = new[] { NewState(fst.States) };

        return new Fst(
            fst.States.Concat(newState),
            fst.Initial.Concat(newState),
            fst.Final.Concat(newState),
            fst.Transitions);
    }

    public static Fst EpsilonFree(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Trim(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Product(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Domain(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fsa Range(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Inverse(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Identity(Fsa fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Expand(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Compose(Fst first, Fst second)
    {
        throw new NotImplementedException();
    }

    public static Fst Reverse(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst ToRealTime(Fst fst)
    {
        throw new NotImplementedException();
    }

    private static Fst RemoveUpperEpsilon(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst SquaredOutput(Fst fst)
    {
        throw new NotImplementedException();
    }

    public static bool IsFunctional(Fst fst)
    {
        throw new NotImplementedException();
    }
}