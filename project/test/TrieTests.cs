using Xunit;

public class TrieTests
{
    [Fact]
    public void TrieConstructionTest()
    {
        var trie = new Trie();
        trie.Insert("apple");

        Assert.True(trie.Recognize("apple"));
        Assert.False(trie.Recognize("app"));
        Assert.True(trie.StartsWith("app"));

        trie.Insert("app");

        Assert.True(trie.Recognize("app"));
    }
}