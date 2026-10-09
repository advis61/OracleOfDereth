using System.Globalization;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // /lph counts silent allegiance and fellowship awards as well as bank chat.
    public sealed class ConquestIncomeLuminance
    {
        private const string Prefix = @"^\s*(?:\[\d{1,2}:\d{2}(?::\d{2})?\]\s*)?\[LPH\] ";
        private static readonly Regex Started = new Regex(Prefix + @"Luminance tracking started\.\s*$");
        private static readonly Regex Report = new Regex(Prefix + @"You've earned (?<total>\d{1,3}(?:,\d{3})*|\d+) Luminance in \d+:\d{2}:\d{2} for [\d,]+ Lum/hour \(5min [\d,]+ Lum/hour\)\.\s*$");
        private static readonly Regex Inactive = new Regex(Prefix + @"Tracking not active\. Use /lph start to begin tracking\.\s*$");
        private readonly IncomeMetric metric;
        public bool Ready { get; private set; }
        public ConquestIncomeLuminance(IncomeMetric metric) { this.metric = metric; }
        public void Reset() { Ready = false; metric.ObserveCounter(0); }
        public bool NoteChat(string text, out bool report)
        {
            report = false;
            if (text == null) return false;
            if (Started.IsMatch(text))
            {
                Ready = true;
                metric.ObserveCounter(0);
                return true;
            }
            if (Inactive.IsMatch(text)) { Ready = false; return true; }
            var match = Report.Match(text);
            if (!match.Success || !Ready || !long.TryParse(match.Groups["total"].Value,
                NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long total)) return false;
            metric.ObserveCounter(total);
            report = true;
            return true;
        }
    }
}
