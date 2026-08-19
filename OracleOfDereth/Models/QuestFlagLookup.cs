using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
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
        private static LookupResult pending;
        private static int running;

        private sealed class LookupResult
        {
            public readonly List<string> Messages = new List<string>();
            public string ClipboardText;
        }

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
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                Util.Chat("A quest flag lookup is already running.", Util.ColorCyan, ChatPrefix);
                return;
            }

            new Thread(() => Run(type, name)) { IsBackground = true }.Start();
        }

        public static void Tick()
        {
            LookupResult result = Interlocked.Exchange(ref pending, null);
            if (result == null) return;

            bool copied = false;
            if (!string.IsNullOrEmpty(result.ClipboardText))
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetText(result.ClipboardText);
                    copied = true;
                }
                catch { }
            }

            foreach (string message in result.Messages)
            {
                string suffix = copied && message == result.ClipboardText ? " (copied to clipboard)" : "";
                Util.Chat(UseSelectedWiki(message) + suffix, Util.ColorCyan, ChatPrefix);
            }
            Interlocked.Exchange(ref running, 0);
        }

        private static void Run(int type, string name)
        {
            var result = new LookupResult();
            try
            {
                AddWikiInfo(result, name);
                AddInqQuests(result, type, name);
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Lookup failed: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref pending, result);
            }
        }

        private static string UseSelectedWiki(string message)
        {
            int urlStart = message.IndexOf("https://acportalstorm.com/wiki/", StringComparison.OrdinalIgnoreCase);
            if (urlStart < 0) return message;
            return message.Substring(0, urlStart) + Util.WikiUrl(message.Substring(urlStart));
        }

        private static void AddWikiInfo(LookupResult result, string name)
        {
            string slug = Uri.EscapeDataString(name.Replace(' ', '_'));
            string wikiUrl = string.Format(WikiUrlTemplate, slug);
            result.Messages.Add($"Wiki: {wikiUrl}");

            string html;
            try
            {
                html = Fetch(wikiUrl);
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                result.Messages.Add("(no wiki page at that URL)");
                return;
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Wiki fetch failed: {ex.Message}");
                return;
            }

            var quests = ExtractRelatedQuests(html);
            if (quests.Count == 0)
            {
                result.Messages.Add("(page found, no Related Quests section)");
                return;
            }

            foreach (var quest in quests)
            {
                result.Messages.Add($"Related quest: {quest.Url}");
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

        private static void AddInqQuests(LookupResult result, int type, string name)
        {
            string esUrl = string.Format(EsUrlTemplate, type);
            string content;

            try
            {
                content = Fetch(esUrl);
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                result.Messages.Add($"No script for [{type}] {name} (not in Creature/Human/).");
                return;
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Script fetch failed: {ex.Message}");
                return;
            }

            // Script was found — always surface the human-browseable .es URL.
            result.Messages.Add($"Script: {string.Format(EsBrowseUrlTemplate, type)}");

            var flags = InqQuestRegex.Matches(content).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();

            if (flags.Count == 0)
            {
                result.Messages.Add("No InqQuest entries in script.");
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

            result.ClipboardText = flags[copyIndex];
            result.Messages.Add("InqQuest flags:");
            for (int i = 0; i < flags.Count; i++)
            {
                result.Messages.Add(flags[i]);
            }
        }

        private static string Fetch(string url)
        {
            // GitHub + ACPortalStorm require TLS 1.2+; ensure it's enabled without
            // disturbing other protocols another part of the app may have set.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "OracleOfDereth-Plugin";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            using (var response = request.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
