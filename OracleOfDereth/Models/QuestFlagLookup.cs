using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace OracleOfDereth
{
    // Backs the "/od questflag" chat command. For the selected NPC, prints:
    //  1. The NPC's ACPortalStorm wiki URL and any "Related Quests:" found on
    //     that page (parsed from the rendered HTML).
    //  2. The InqQuest flags referenced by the NPC's weenie .es script in the
    //     ACEmulator 16PY patches repo. The first flag is copied to clipboard.
    public static class QuestFlagLookup
    {
        private const string EsUrlTemplate = "https://raw.githubusercontent.com/ACEmulator/ACE-World-16PY-Patches/master/Database/Patches/9%20WeenieDefaults/Creature/Human/{0}.es";
        private const string EsBrowseUrlTemplate = "https://github.com/ACEmulator/ACE-World-16PY-Patches/blob/master/Database/Patches/9%20WeenieDefaults/Creature/Human/{0}.es";
        private const string WikiUrlTemplate = "https://acportalstorm.com/wiki/{0}";
        private const string ChatPrefix = "[OD] ";

        private static readonly Regex InqQuestRegex = new Regex(@"InqQuest:\s*(\S+)", RegexOptions.IgnoreCase);
        private static readonly Regex WikiLinkRegex = new Regex( @"<a\s+href=""/wiki/([^""#]+)""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);
        private static readonly Regex RelatedQuestsBlockRegex = new Regex(@"Related(?:\s|&#160;|&nbsp;)+Quests:.*?</td>\s*<td[^>]*>(.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public static void Execute()
        {
            WorldObject npc = Target.GetCurrent().Item();
            if (npc == null)
            {
                Util.Chat("No target selected. Select an NPC first.", Util.ColorRed, ChatPrefix);
                return;
            }

            int type = npc.Type;
            string name = string.IsNullOrEmpty(npc.Name) ? "<unknown>" : npc.Name;

            if (type == 0)
            {
                Util.Chat($"(no Type / weenie id; skipping lookup)", Util.ColorRed, ChatPrefix);
                return;
            }

            Util.Chat($"[{type}] {name}", Util.ColorCyan, ChatPrefix);

            PrintWikiInfo(name);
            PrintInqQuests(type, name);
        }

        private static void PrintWikiInfo(string name)
        {
            string slug = Uri.EscapeDataString(name.Replace(' ', '_'));
            string wikiUrl = string.Format(WikiUrlTemplate, slug);
            Util.Chat($"Wiki: {Util.WikiUrl(wikiUrl)}", Util.ColorCyan, ChatPrefix);

            string html;
            try
            {
                html = Fetch(wikiUrl);
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                Util.Chat($"(no wiki page at that URL)", Util.ColorCyan, ChatPrefix);
                return;
            }
            catch (Exception ex)
            {
                Util.Chat($"Wiki fetch failed: {ex.Message}", Util.ColorCyan, ChatPrefix);
                return;
            }

            var quests = ExtractRelatedQuests(html);
            if (quests.Count == 0)
            {
                Util.Chat($"(page found, no Related Quests section)", Util.ColorCyan, ChatPrefix);
                return;
            }

            foreach (var quest in quests)
            {
                Util.Chat($"Related quest: {Util.WikiUrl(quest.Url)}", Util.ColorCyan, ChatPrefix);
            }
        }

        private static List<(string Name, string Url)> ExtractRelatedQuests(string html)
        {
            var results = new List<(string Name, string Url)>();

            foreach (Match block in RelatedQuestsBlockRegex.Matches(html))
            {
                foreach (Match link in WikiLinkRegex.Matches(block.Groups[1].Value))
                {
                    string linkSlug = link.Groups[1].Value;
                    string linkText = WebUtility.HtmlDecode(link.Groups[2].Value);
                    string url = string.Format(WikiUrlTemplate, linkSlug);

                    if (!results.Any(r => r.Url == url))
                    {
                        results.Add((linkText, url));
                    }
                }
            }

            return results;
        }

        private static void PrintInqQuests(int type, string name)
        {
            string esUrl = string.Format(EsUrlTemplate, type);
            string content;

            try
            {
                content = Fetch(esUrl);
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                Util.Chat($"No script for [{type}] {name} (not in Creature/Human/).", Util.ColorCyan, ChatPrefix);
                return;
            }
            catch (Exception ex)
            {
                Util.Chat($"Script fetch failed: {ex.Message}", Util.ColorCyan, ChatPrefix);
                return;
            }

            // Script was found — always surface the human-browseable .es URL.
            Util.Chat($"Script: {string.Format(EsBrowseUrlTemplate, type)}", Util.ColorCyan, ChatPrefix);

            var flags = InqQuestRegex.Matches(content).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();

            if (flags.Count == 0)
            {
                Util.Chat($"No InqQuest entries in script.", Util.ColorCyan, ChatPrefix);
                return;
            }

            // Prefer the Wait/Complete flag for clipboard — that's usually the
            // daily-timer or completion marker (more useful than Started or main).
            int copyIndex = 0;
            for (int i = 0; i < flags.Count; i++)
            {
                if (flags[i].IndexOf("Wait", StringComparison.OrdinalIgnoreCase) >= 0 || flags[i].IndexOf("Complete", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    copyIndex = i;
                    break;
                }
            }

            bool copied = false;
            try
            {
                System.Windows.Forms.Clipboard.SetText(flags[copyIndex]);
                copied = true;
            }
            catch { }

            Util.Chat($"InqQuest flags:", Util.ColorCyan, ChatPrefix);
            for (int i = 0; i < flags.Count; i++)
            {
                string suffix = (i == copyIndex && copied) ? " (copied to clipboard)" : "";
                Util.Chat($"{flags[i]}{suffix}", Util.ColorCyan, ChatPrefix);
            }
        }

        private static string Fetch(string url)
        {
            // GitHub + ACPortalStorm require TLS 1.2+; ensure it's enabled without
            // disturbing other protocols another part of the app may have set.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "OracleOfDereth-Plugin");
                return client.DownloadString(url);
            }
        }
    }
}
