using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace OracleOfDereth
{
    // Sends quest flags the master list doesn't cover yet back for curation:
    //
    //   diff  flags held that quests.csv doesn't cover
    //   up    export -> Discord webhook -> sent log
    //
    // The webhook post runs on the calling (main) thread, like QuestFlagLookup's. The client must
    // only be touched on its own thread and the simplest way to honour that is never to leave it —
    // no worker, no hand-off, no in-flight state. The timeout bounds the stall, and nothing here
    // runs unless the player presses Send.
    public static class QuestSubmit
    {
        private const string WebhookResource = ".Resources.webhook.txt";
        private const string SentKey = "SentQuestFlags";

        private const int TimeoutMs = 15000;

        // ---- diff ------------------------------------------------------------------------------

        // The one definition of "worth reporting", so the button's visibility and the export can't
        // disagree. VerifiedConquest alone covers both cases: a flag discovered from /myquests has
        // no CSV row, so it's false by construction.
        public static bool IsPending(Quest quest, string server)
        {
            return !quest.VerifiedConquest && quest.IsComplete() && !WasSent(server, quest.Flag);
        }

        public static int PendingCount()
        {
            string server = Server.Name;
            int count = 0;

            foreach (Quest quest in Quest.Quests)
            {
                if (IsPending(quest, server)) { count++; }
            }

            return count;
        }

        // Two curation jobs: unknown flags need a new row written, unverified ones only need the
        // column filled in.
        public static void Pending(out List<Quest> unknown, out List<Quest> unverified)
        {
            Quest.MergeQuestFlags();

            string server = Server.Name;

            unknown = Quest.Quests.Where(q => q.IsNew && IsPending(q, server)).OrderBy(q => q.Flag).ToList();
            unverified = Quest.Quests.Where(q => !q.IsNew && IsPending(q, server)).OrderBy(q => q.Flag).ToList();
        }

        // ---- up --------------------------------------------------------------------------------

        public static bool CanSend => WebhookUrl().Length > 0;

        // Records the flags only on a 2xx — never on the strength of having tried, so a failure
        // leaves them pending for the next attempt.
        public static bool Send(string fileName, string content, string summary, string server, string[] flags, out string reason)
        {
            reason = "";
            if (!CanSend) { reason = "posting isn't configured in this build"; return false; }

            try
            {
                string boundary = "----OracleOfDereth" + DateTime.UtcNow.Ticks;
                byte[] body = BuildMultipart(boundary, fileName, content, summary);

                // Discord needs TLS 1.2+; enable without disturbing anything else.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var request = (HttpWebRequest)WebRequest.Create(WebhookUrl());
                request.UserAgent = "OracleOfDereth-Plugin";
                request.Timeout = TimeoutMs;
                request.ReadWriteTimeout = TimeoutMs;
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.ContentLength = body.Length;

                int status;
                using (Stream stream = request.GetRequestStream()) { stream.Write(body, 0, body.Length); }
                using (var response = (HttpWebResponse)request.GetResponse()) { status = (int)response.StatusCode; }

                if (status < 200 || status >= 300) { reason = $"Discord answered {status}"; return false; }

                MarkSent(server, flags);
                return true;
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                reason = "couldn't reach Discord - see errors.txt";
                return false;
            }
        }

        // The export goes up as an attachment, not message text: Discord caps content at 2000
        // characters, which a few hundred flag lines blows straight past.
        private static byte[] BuildMultipart(string boundary, string fileName, string content, string summary)
        {
            var stream = new MemoryStream();

            void Part(string text)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
            }

            Part($"--{boundary}\r\n");
            Part("Content-Disposition: form-data; name=\"payload_json\"\r\n");
            Part("Content-Type: application/json\r\n\r\n");
            Part("{\"content\":" + JsonString(summary) + ",\"allowed_mentions\":{\"parse\":[]}}\r\n");
            Part($"--{boundary}\r\n");
            Part($"Content-Disposition: form-data; name=\"files[0]\"; filename=\"{fileName}\"\r\n");
            Part("Content-Type: text/plain\r\n\r\n");
            Part(content);
            Part($"\r\n--{boundary}--\r\n");

            return stream.ToArray();
        }

        // The summary carries a character name, which is user-controlled enough to escape properly.
        private static string JsonString(string value)
        {
            var sb = new StringBuilder("\"");

            foreach (char c in value ?? "")
            {
                if (c == '"') { sb.Append("\\\""); }
                else if (c == '\\') { sb.Append("\\\\"); }
                else if (c == '\n') { sb.Append("\\n"); }
                else if (c == '\r') { sb.Append("\\r"); }
                else if (c == '\t') { sb.Append("\\t"); }
                else if (c < ' ') { sb.Append("\\u").Append(((int)c).ToString("x4")); }
                else { sb.Append(c); }
            }

            return sb.Append("\"").ToString();
        }

        // Resources\webhook.txt is embedded at build time and gitignored. It must NOT be committed:
        // GitHub secret scanning reports Discord webhooks and Discord auto-revokes them, which
        // would break Send for every user. A build without the file is valid and simply can't post.
        private static string webhookUrl;

        private static string WebhookUrl()
        {
            if (webhookUrl != null) { return webhookUrl; }

            webhookUrl = "";

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(WebhookResource, StringComparison.OrdinalIgnoreCase));
                if (name == null) { return webhookUrl; }

                using (var stream = assembly.GetManifestResourceStream(name))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0 || line.StartsWith("#")) { continue; }

                        webhookUrl = line;
                        break;
                    }
                }
            }
            catch (Exception ex) { Util.Log(ex); }

            return webhookUrl;
        }

        // ---- sent log --------------------------------------------------------------------------

        // Keyed "server|flag": Verified Conquest is Conquest-specific, so the same flag from
        // another world is new information. Not keyed by character — once a flag is reported for a
        // server, a second character holding it adds nothing.
        private static HashSet<string> sent;

        private static HashSet<string> Sent()
        {
            if (sent != null) { return sent; }

            sent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (string entry in SettingsFile.GetSetting(SentKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (entry.Trim().Length > 0) { sent.Add(entry.Trim()); }
                }
            }
            catch (Exception ex) { Util.Log(ex); }

            return sent;
        }

        public static int SentCount => Sent().Count;

        private static bool WasSent(string server, string flag) => Sent().Contains($"{server}|{flag}");

        private static void MarkSent(string server, IEnumerable<string> flags)
        {
            if (flags == null) { return; }

            HashSet<string> current = Sent();
            if (flags.Where(f => !string.IsNullOrEmpty(f)).Count(f => current.Add($"{server}|{f}")) == 0) { return; }

            SettingsFile.PutSetting(SentKey, string.Join(",", current.ToArray()));
        }

        public static void ClearSent()
        {
            sent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SettingsFile.PutSetting(SentKey, "");
        }
    }
}
