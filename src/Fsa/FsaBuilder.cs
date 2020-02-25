using System;
using System.Collections.Generic;
using System.Linq;

public static class FsaBuilder
{
    private static int NewState(IReadOnlyList<int> states) => states.Count;

    // Creates a new Fsa by renaming the states 
    private static Fsa Remap(Fsa automaton, int k)
    {
        var states = automaton.States.Select(s => s + k).ToArray();
        var initial = automaton.InitialStates.Select(s => s + k).ToArray();
        var final = automaton.FinalStates.Select(s => s + k).ToArray();
        var transitions = automaton.Transitions.Select(t => (t.From + k, t.Via, t.To + k)).ToArray();

        return new Fsa(states, initial, final, transitions);
    }

    public static Fsa FromEpsilon() => FromWord(string.Empty);

    public static Fsa FromWord(string word)
    {
        var state = 0;
        var states = new List<int> { state };
        var initialStates = new int[] { state };
        var transitions = new List<(int, string, int)>();

        foreach (var symbol in word)
        {
            var next = state + 1;
            transitions.Add((state, symbol.ToString(), next));
            states.Add(next);
            state = next;
        }

        return new Fsa(
            states,
            initialStates,
            finalStates: new int[] { state },
            transitions);
    }

    public static Fsa FromSymbolSet(ISet<string> alphabet)
    {
        var initial = 0;
        var final = 1;
        var transitions = new List<(int, string, int)>();

        foreach (var token in alphabet)
            transitions.Add((initial, token, final));

        return new Fsa(
            states: new int[] { initial, final },
            initialStates: new int[] { initial },
            finalStates: new int[] { final },
            transitions);
    }

    public static Fsa Concat(Fsa first, Fsa second)
    {
        var firstFinalStates = first.FinalStates;
        second = Remap(second, first.States.Count);
        var secondInitialStates = second.InitialStates;

        var initialStates = first.InitialStates.Intersect(first.FinalStates).Any()
            ? first.InitialStates.Union(second.InitialStates)
            : first.InitialStates;

        var transitions = first.Transitions.Union(second.Transitions).ToList();

        foreach (var tr in first.Transitions.Where(t => first.FinalStates.Contains(t.To)))
            foreach (var state in second.InitialStates)
                transitions.Add((tr.From, tr.Via, state));

        return new Fsa(
            states: first.States.Union(second.States).ToArray(),
            initialStates.ToArray(),
            second.FinalStates.ToArray(),
            transitions);
    }

    public static Fsa Union(Fsa first, Fsa second)
    {
        second = Remap(second, first.States.Count);
        return new Fsa(
            states: first.States.Union(second.States).ToArray(),
            initialStates: first.InitialStates.Union(second.InitialStates).ToArray(),
            finalStates: first.FinalStates.Union(second.FinalStates).ToArray(),
            transitions: first.Transitions.Union(second.Transitions).ToArray());
    }

    public static Fsa Star(Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, string, int)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates).ToArray(),
            initialStates,
            finalStates: automaton.FinalStates.Union(initialStates).ToArray(),
            automaton.Transitions.Union(newTransitions).ToArray());
    }

    public static Fsa Plus(Fsa automaton)
    {
        var initial = NewState(automaton.States);
        var initialStates = new int[] { initial };
        var newTransitions = new List<(int, string, int)>();

        foreach (var state in automaton.InitialStates)
            newTransitions.Add((initial, string.Empty, state));

        foreach (var state in automaton.FinalStates)
            newTransitions.Add((state, string.Empty, initial));

        return new Fsa(
            states: automaton.States.Union(initialStates).ToArray(),
            initialStates,
            finalStates: automaton.FinalStates.ToArray(),
            automaton.Transitions.Union(newTransitions).ToArray());
    }

    public static Fsa Option(Fsa automaton)
    {
        var state = new[] { NewState(automaton.States) };

        return new Fsa(
            automaton.States.Union(state).ToArray(),
            automaton.InitialStates.Union(state).ToArray(),
            automaton.FinalStates.Union(state).ToArray(),
            automaton.Transitions.ToArray());
    }

    public static Fsa All(ISet<string> alphabet) 
        => FsaBuilder.Star(FsaBuilder.FromSymbolSet(alphabet));

    /* Preserves the automaton's language but 
       does not preserve the language of individual states */
    public static Fsa EpsilonFree(Fsa automaton)
    {
        var initial = automaton.InitialStates
            .SelectMany(automaton.EpsilonClosure)
            .ToArray();

        var transitions = automaton.Transitions
            .Where(t => !string.IsNullOrEmpty(t.Via))
            .SelectMany(t =>
                automaton
                    .EpsilonClosure(t.To)
                    .Select(es => (t.From, t.Via, es)))
            .ToArray();

        return new Fsa(automaton.States.ToArray(), initial, automaton.FinalStates.ToArray(), transitions);
    }

    public static Fsa Reverse(Fsa automaton)
    {
        throw new NotImplementedException();
    }

    public static Fsa Trim(Fsa automaton)
    {
        var rel = automaton.Transitions
            .Select(t => (t.From, t.To))
            .TransitiveClosure();

        var newStates = automaton.InitialStates
            .Union(
                rel.Where(x => automaton.InitialStates.Contains(x.Item1))
                    .Select(x => x.Item2))
            .Intersect(
                automaton.FinalStates
                    .Union(
                        rel.Where(x => automaton.FinalStates.Contains(x.Item2))
                            .Select(x => x.Item1)))
            .ToArray();

        var newTransitions = automaton.Transitions
            .Where(t => newStates.Contains(t.From) && newStates.Contains(t.To))
            .Select(t => (
                Array.IndexOf(newStates, t.From),
                t.Via,
                Array.IndexOf(newStates, t.To)))
            .ToArray();

        var newInitial = newStates
            .Intersect(automaton.InitialStates)
            .Select(s => Array.IndexOf(newStates, s))
            .ToArray();

        var newFinal = newStates
            .Intersect(automaton.FinalStates)
            .Select(s => Array.IndexOf(newStates, s)).ToArray()
            .ToArray();

        return new Fsa(
            newStates.Select(s => Array.IndexOf(newStates, s)).ToArray(),
            newInitial,
            newFinal,
            newTransitions);
    }

    public static Fsa Expand(Fsa automaton)
    {
        IEnumerable<int> KNewStates(int k, int startAfter)
            => Enumerable.Range(startAfter, k);

        var multiSymbolTransitions = automaton.Transitions.Where(t => t.Via.Length > 1);

        var newStates = automaton.States;
        IEnumerable<(int, string, int)> newTransitions = automaton.Transitions;

        foreach (var tr in multiSymbolTransitions)
        {
            var wordLen = tr.Via.Length;
            var stateSeq = new[]{ tr.From }
                .Concat(KNewStates(wordLen - 1, newStates.Count))
                .Concat(new[] { tr.To })
                .ToArray();

            newStates = newStates.Union(stateSeq).ToList();
            var path = Enumerable.Range(0, stateSeq.Length - 1)
                    .Select(i => (stateSeq[i], tr.Via[i].ToString(), stateSeq[i+1]));

            newTransitions = newTransitions
                .Except(new[] { tr })
                .Union(path);
        }

        return new Fsa(
            newStates,
            automaton.InitialStates.ToArray(),
            automaton.FinalStates.ToArray(),
            newTransitions.ToArray());
    }

    public static Dfsa Determinize(Fsa automaton)
    {
        throw new NotImplementedException();
    }

    public static Fsa Product(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Intersect(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Difference(Fsa first, Fsa second)
    {
        throw new NotImplementedException();
    }

    public static Fsa Complement(Fsa automaton)
    {
        throw new NotImplementedException();
    }
}