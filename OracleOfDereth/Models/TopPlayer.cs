namespace OracleOfDereth
{
    // One row of a "/top" leaderboard, e.g. "1: 5,682 - Stannonkor" -> Rank 1, Value "5,682",
    // Name "Stannonkor". Value is kept as the server printed it (already grouped with commas)
    // since we only ever display it — the ranking is the server's, so nothing sorts on it.
    // See TopBoard, which owns the parsing and the collection of these.
    public class TopPlayer
    {
        public int Rank { get; }
        public string Value { get; }
        public string Name { get; }

        public TopPlayer(int rank, string value, string name)
        {
            Rank = rank;
            Value = value;
            Name = name;
        }
    }
}
