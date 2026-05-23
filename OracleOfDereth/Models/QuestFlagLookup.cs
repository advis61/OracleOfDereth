using Decal.Adapter.Wrappers;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Fetches a creature's weenie .es script from the ACEmulator 16PY patches
    // repo and extracts the InqQuest entries the script references. Backs the
    // "/od questflag" chat command.
    public static class QuestFlagLookup
    {
        private const string UrlTemplate =
            "https://raw.githubusercontent.com/ACEmulator/ACE-World-16PY-Patches/master/Database/Patches/9%20WeenieDefaults/Creature/Human/{0}.es";

        private static readonly Regex InqQuestRegex = new Regex(@"InqQuest:\s*(\S+)", RegexOptions.IgnoreCase);

        public static void Execute()
        {
            WorldObject npc = Target.GetCurrent().Item();
            if (npc == null)
            {
                Util.Chat("No target selected. Select an NPC first.", Util.ColorRed);
                return;
            }

            int type = npc.Type;
            string name = string.IsNullOrEmpty(npc.Name) ? "<unknown>" : npc.Name;

            if (type == 0)
            {
                Util.Chat($"{name} has no Type / weenie id.", Util.ColorRed);
                return;
            }

            string url = string.Format(UrlTemplate, type);
            string content;

            try
            {
                // GitHub requires TLS 1.2+; ensure it's enabled without disturbing other protocols.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "OracleOfDereth-Plugin");
                    content = client.DownloadString(url);
                }
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                Util.Chat($"No script for [{type}] {name} (not in Creature/Human/).", Util.ColorRed);
                return;
            }
            catch (Exception ex)
            {
                Util.Chat($"Fetch failed for [{type}] {name}: {ex.Message}", Util.ColorRed);
                return;
            }

            var flags = InqQuestRegex.Matches(content)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            if (flags.Count == 0)
            {
                Util.Chat($"[{type}] {name}: no InqQuest entries found in script.", Util.ColorYellow);
                return;
            }

            Util.Chat($"[{type}] {name} - InqQuest flags:", Util.ColorCyan);
            foreach (var flag in flags)
            {
                Util.Chat($"  {flag}", Util.ColorCyan);
            }
        }
    }
}
