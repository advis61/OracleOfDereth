using System.Globalization;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Accept banked chat awards and Int64 property 6 gains on every world.
    // Observe each property update so spending between UI ticks does not hide gains.
    public sealed class IncomeLuminance
    {
        private static readonly Regex Banked = new Regex(
            @"^\s*(?:\[\d{1,2}:\d{2}(?::\d{2})?\]\s*)?You've banked (?<amount>\d{1,3}(?:,\d{3})*|\d+) Luminance\.\s*$",
            RegexOptions.CultureInvariant);
        private readonly IncomeMetric metric;
        private long? balance;

        public IncomeLuminance(IncomeMetric metric)
        {
            this.metric = metric;
        }

        public void NoteChat(string text)
        {
            if (text == null) return;
            var match = Banked.Match(text);
            if (match.Success && long.TryParse(match.Groups["amount"].Value,
                NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long amount))
                metric.AddGain(amount);
        }

        public void ObserveBalance(long value)
        {
            if (value < 0) return;
            balance = value;
            metric.ObserveCounter(value);
        }

        // Called after the shared session resets. Keep the last known balance
        // as the baseline so the first award after reset is still counted.
        public void ResetBaseline()
        {
            if (balance.HasValue) metric.ObserveCounter(balance.Value);
        }
    }
}
