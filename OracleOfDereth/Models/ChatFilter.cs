using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    public static class ChatFilter
    {
        private static readonly Regex PeriodicHealingRegex = new Regex(
            Util.ChatPrefixPattern + @"You receive 0 points of periodic healing\.\s*$",
            RegexOptions.IgnoreCase);

        public static bool ShouldSuppress(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            return Setting.SuppressPeriodicHealingChat.IsYes && PeriodicHealingRegex.IsMatch(text);
        }
    }
}
