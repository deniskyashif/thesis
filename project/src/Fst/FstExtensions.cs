using System;
using System.Collections.Generic;
using System.Linq;

public static class FstExtensions
{
    static int NewState(IReadOnlyCollection<int> states) => states.Count;

    static Fst Remap(this Fst fst, IReadOnlyCollection<int> states)
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

    public static Fst Union(this Fst first, Fst second)
    {
        second = second.Remap(first.States);

        return new Fst(
            first.States.Concat(second.States),
            first.Initial.Concat(second.Initial),
            first.Final.Concat(second.Final),
            first.Transitions.Concat(second.Transitions));
    }

    public static Fst Concat(this Fst first, Fst second)
    {
        second = second.Remap(first.States);
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

    public static Fst Star(this Fst fst)
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

    public static Fst Plus(this Fst fst)
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

    public static Fst Option(this Fst fst)
    {
        var newState = new[] { NewState(fst.States) };

        return new Fst(
            fst.States.Concat(newState),
            fst.Initial.Concat(newState),
            fst.Final.Concat(newState),
            fst.Transitions);
    }

    public static Fst EpsilonFree(this Fst fst)
    {
        var transitions = fst.Transitions
            .Where(t => !(string.IsNullOrEmpty(t.In) && string.IsNullOrEmpty(t.Out)))
            .SelectMany(t =>
                fst.EpsilonClosure(t.To)
                    .Select(to => (t.From, t.In, t.Out, to)));

        return new Fst(
            fst.States,
            fst.Initial.SelectMany(s => fst.EpsilonClosure(s)),
            fst.Final,
            transitions);
    }

    public static Fst Trim(this Fst fst)
    {
        ISet<(int From, int To)> transitiveClosure = fst.Transitions
            .Select(t => (t.From, t.To))
            .ToHashSet()
            .TransitiveClosure();

        var reachableFromInitial = fst.Initial.Union(
            transitiveClosure.Where(p => fst.Initial.Contains(p.From)).Select(p => p.To));
        var leadingToFinal = fst.Final.Union(
            transitiveClosure.Where(p => fst.Final.Contains(p.To)).Select(p => p.From));

        var states = reachableFromInitial.Intersect(leadingToFinal).ToArray();
        var initial = states.Intersect(fst.Initial);
        var final = states.Intersect(fst.Final);
        var transitions = fst.Transitions
            .Where(t => states.Contains(t.From) && states.Contains(t.To))
            .Select(t => (Array.IndexOf(states, t.From), t.In, t.Out, Array.IndexOf(states, t.To)));

        return new Fst(
            states.Select(s => Array.IndexOf(states, s)),
            initial.Select(s => Array.IndexOf(states, s)),
            final.Select(s => Array.IndexOf(states, s)),
            transitions);
    }

    public static Fsa Domain(this Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fsa Range(this Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Inverse(this Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Identity(this Fsa fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Product(this Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fst Expand(this Fst fst)
    {
        throw new NotImplementedException();
    }

    public static Fst Compose(this Fst first, Fst second)
    {
        throw new NotImplementedException();
    }

    public static Fst Reverse(this Fst fst)
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