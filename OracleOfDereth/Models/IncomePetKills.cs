using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Conquest sends this message to the pet owner instead of KillerNotification.
    public sealed class IncomePetKills
    {
        private static readonly Regex KillMessage = new Regex(
            @"^\s*(?:\[\d{1,2}:\d{2}(?::\d{2})?\]\s*)?Your \S[^\r\n]*? killed \S[^\r\n]*?!\s*$",
            RegexOptions.CultureInvariant);
        private readonly IncomeMetric metric;
        public IncomePetKills(IncomeMetric metric) { this.metric = metric; }

        public void NoteChat(string text)
        {
            if (text != null && KillMessage.IsMatch(text)) metric.AddGain(1);
        }
    }
}
