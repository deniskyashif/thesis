# Lexical Analysis using Bimachines

### Regular Expression Parser

Supported syntax

- Concatenation `ab`
- Union `a|b`
- Zero-or-more `a*`
- One-or-more `a+`
- Optional `a?`
- Grouping `(a|b)*c`
- Character ranges
  - Positive `[a-z0-9,]`
  - Negative `[^a-z0-9,]`
- Count
  - `a{2}` - match exactly 2 'a's
  - `a{2,}` - match at least 2 'a's
  - `a{2,4}` - match between 2 and 4 'a's
- Escaping `a\*` (match "a*"), `[a\-z]` (match 'a', '-' or 'z')

[\[Implementation\]](https://github.com/deniskyashif/thesis/blob/master/project/src/RegExp.cs) [\[Tests\]](https://github.com/deniskyashif/thesis/blob/master/project/test/RegExpTests.cs)

### Finite-state device construction and operations

- Finite-State Automata \[[NFA-Representation](https://github.com/deniskyashif/thesis/blob/master/project/src/Fsa/Fsa.cs)\] \[[DFA-Representation](https://github.com/deniskyashif/thesis/blob/master/project/src/Fsa/Dfsa.cs)\] \[[Constructions](https://github.com/deniskyashif/thesis/blob/master/project/src/Fsa/FsaBuilder.cs)\] \[[Operations](https://github.com/deniskyashif/thesis/blob/master/project/src/Fsa/FsaOperations.cs)\]
- Finite-State Transducers \[[Representation](https://github.com/deniskyashif/thesis/blob/master/project/src/Fst/Fst.cs)\] \[[Construction](https://github.com/deniskyashif/thesis/blob/master/project/src/Fst/FstBuilder.cs)\] \[[Operations](https://github.com/deniskyashif/thesis/blob/master/project/src/Fst/FstOperations.cs)\]
- Bimachines \[[Representation](https://github.com/deniskyashif/thesis/blob/master/project/src/Bimachine/Bimachine.cs)\] \[[Construction](https://github.com/deniskyashif/thesis/blob/598a69f5b1dccffd63f1935e6f14661c81d66ecb/project/src/Fst/FstOperations.cs#L351)\]

### Text Rewriters based on Regular Relations

- Optional rewrite transducer
- Obligatory rewrite transducer
- Leftmost-longest match rewrite transducer

[\[Implementations\]](https://github.com/deniskyashif/thesis/blob/master/project/src/Rewriters.cs) [\[Tests\]](https://github.com/deniskyashif/thesis/blob/master/project/test/RewriterTests.cs)

### Bimachine-based Lexers

- Arithmetic expression
- JSON
- Regular expression
- Tokenizer for the English language

[\[Lexer Generator\]](https://github.com/deniskyashif/thesis/blob/master/project/src/Lexer/Lexer.cs) [\[Tests\]](https://github.com/deniskyashif/thesis/blob/master/project/test/LexerTests.cs)

### Misc

- Trie (prefix tree) [implementation](https://github.com/deniskyashif/thesis/blob/master/project/src/Trie.cs)
- [Direct construction](https://github.com/deniskyashif/thesis/blob/master/project/src/MinDfaAlgorithm.cs) of a minimal, deterministic, acyclic finite-state automaton from a set of strings

Example usages of the APIs can be found in the [unit tests' project](https://github.com/deniskyashif/thesis/tree/master/project/test).

### References

- [Finite-State Techniques Automata, Transducers and Bimachines](https://www.cambridge.org/core/books/finitestate-techniques/E21E748468F0310DA12A2CFAEB989185)
- [Regular Models of Phonological Rule
Systems](https://web.stanford.edu/~mjkay/Kaplan%26Kay.pdf)
- [Incremental Construction of Minimal
Acyclic Finite-State Automata](https://www.aclweb.org/anthology/J00-1002.pdf)