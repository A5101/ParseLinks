using Newtonsoft.Json;
using System.Runtime.Serialization;

public class SparseMatrix
{
    private Dictionary<(int, int), int> pairs;

    public SparseMatrix()
    {
        pairs = new Dictionary<(int, int), int>();
    }

    public int this[int i, int j]
    {
        get
        {
            if (pairs.TryGetValue((i, j), out int res))
            {
                return res;
            }
            return 0;
        }
        set
        {
            if (pairs.ContainsKey((i, j)))
            {
                pairs[(i, j)] = value;
            }
            else
            {
                pairs.Add((i, j), value);
            }
        }
    }

    public int GetMax()
    {
        return pairs.OrderByDescending(pa => pa.Value).First().Value;
    }

    [JsonProperty("items")]
    public List<Tuple<int, int, int>> Items
    {
        get
        {
            return pairs.Select(pair => Tuple.Create(pair.Key.Item1, pair.Key.Item2, pair.Value)).ToList();
        }
        set
        {
            pairs = value.ToDictionary(tuple => (tuple.Item1, tuple.Item2), tuple => tuple.Item3);
        }
    }
}