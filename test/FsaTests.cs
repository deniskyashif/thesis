using System;
using Xunit;

namespace Automata.UnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void EpsilonFsaBuilderTest()
        {
            var fsa = FsaBuilder.FromEpsilon();

            Assert.Single(fsa.States);
            Assert.True(fsa.Recognize(string.Empty));
            Assert.False(fsa.Recognize("a"));
            Assert.False(fsa.Recognize("abc"));
        }

        [Fact]
        public void WordFsaBuilderTest()
        {
            var fsa = FsaBuilder.FromWord("abc");

            Assert.Equal(4, fsa.States.Count);
            Assert.False(fsa.Recognize(string.Empty));
            Assert.False(fsa.Recognize("a"));
            Assert.True(fsa.Recognize("abc"));
            Assert.False(fsa.Recognize("abca"));
        }

        [Fact]
        public void UniversalLanguageFsaBuilderTest()
        {
            var fsa = FsaBuilder.UniversalLanguage(new[] { 'a', 'b', 'c' });

            Assert.Single(fsa.States);
            Assert.True(fsa.Recognize(string.Empty));
            Assert.True(fsa.Recognize("b"));
            Assert.True(fsa.Recognize("abc"));
            Assert.True(fsa.Recognize("abbbacacbb"));
            Assert.False(fsa.Recognize("baaaaaXccc"));
        }

        [Fact]
        public void ConcatFsaTest()
        {
            var fsa1 = FsaBuilder.FromWord("abc");
            var fsa2 = FsaBuilder.FromWord("de");
            var fsa = FsaBuilder.Concat(fsa1, fsa2);

            Assert.Equal(7, fsa.States.Count);
            Assert.Single(fsa.InitialStates);
            Assert.Single(fsa.FinalStates);
            Assert.False(fsa.Recognize(string.Empty));
            Assert.False(fsa.Recognize("a"));
            Assert.False(fsa.Recognize("abc"));
            Assert.False(fsa.Recognize("de"));
            Assert.True(fsa.Recognize("abcde"));
        }
    }
}
