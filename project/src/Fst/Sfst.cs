using System.Collections.Generic;

public class Sfst
{
    public IEnumerable<int> States { get; set; }
    public IEnumerable<int> Initial { get; set; }
    public IEnumerable<int> Final { get; set; }
}