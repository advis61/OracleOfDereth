using System.Globalization;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    public sealed class IncomeConquestCoins
    {
        private const string Prefix = @"^\s*(?:\[\d{1,2}:\d{2}(?::\d{2})?\]\s*)?";
        private const string Amount = @"\d{1,3}(?:,\d{3})*|\d+";
        private static readonly Regex Given = new Regex(Prefix
            + @"\S[^\r\n]*? gives you (?:(?<amount>" + Amount + @") )?Conquest Coins?\.\s*$",
            RegexOptions.CultureInvariant);
        private static readonly Regex Received = new Regex(Prefix
            + @"You have received (?:\((?<amount>" + Amount + @")\) |(?<amount>" + Amount
            + @") )?Conquest Coins?(?: \(Bonus\))?!\s*$", RegexOptions.CultureInvariant);
        private readonly IncomeMetric metric;
        public IncomeConquestCoins(IncomeMetric metric) { this.metric = metric; }

        public void NoteChat(string text)
        {
            if (text == null) return;
            var match = Given.Match(text);
            if (!match.Success) match = Received.Match(text);
            if (!match.Success) return;
            var quantity = match.Groups["amount"];
            if (!quantity.Success) metric.AddGain(1);
            else if (long.TryParse(quantity.Value, NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out long amount)) metric.AddGain(amount);
        }
    }
}
