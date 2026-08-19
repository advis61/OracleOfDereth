using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OracleOfDereth
{
    // Exports uncurated quest flags, posts them to Discord, and records successful submissions.
    public static class QuestSubmit
    {
        private const string WebhookResource = ".Resources.webhook.txt";
        private const string SentKey = "SentQuestFlags";

        private const int TimeoutMs = 15000;
        private static SendResult pendingResult;
        private static int sending;
        private static int generation;

        private sealed class SendResult
        {
            public bool Success;
            public string Reason;
            public string Server;
            public string[] Flags;
            public Action<bool, string> Completed;
            public Exception Exception;
            public int Generation;
        }

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
        public static bool SendAsync(string fileName, string content, string summary, string server, string[] flags, Action<bool, string> completed, out string reason)
        {
            reason = "";
            if (!CanSend) { reason = "posting isn't configured in this build"; return false; }
            if (Interlocked.CompareExchange(ref sending, 1, 0) != 0) { reason = "a submission is already in progress"; return false; }

            var result = new SendResult
            {
                Server = server,
                Flags = flags,
                Completed = completed,
                Generation = Volatile.Read(ref generation)
            };

            new Thread(() => Send(fileName, content, summary, result)) { IsBackground = true }.Start();
            return true;
        }

        public static void Tick()
        {
            SendResult result = Interlocked.Exchange(ref pendingResult, null);
            if (result == null) return;

            try
            {
                if (result.Exception != null) Util.Log(result.Exception);
                if (result.Success) MarkSent(result.Server, result.Flags);
                result.Completed?.Invoke(result.Success, result.Reason);
            }
            finally
            {
                Interlocked.Exchange(ref sending, 0);
            }
        }

        private static void Send(string fileName, string content, string summary, SendResult result)
        {
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

                if (status < 200 || status >= 300)
                {
                    result.Reason = $"Discord answered {status}";
                    return;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Reason = "couldn't reach Discord - see errors.txt";
            }
            finally
            {
                if (result.Generation == Volatile.Read(ref generation))
                    Interlocked.Exchange(ref pendingResult, result);
            }
        }

        public static void Shutdown()
        {
            Interlocked.Increment(ref generation);
            Interlocked.Exchange(ref pendingResult, null);
            Interlocked.Exchange(ref sending, 0);
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
            Part("{\"content\":" + Util.JsonString(summary) + ",\"allowed_mentions\":{\"parse\":[]}}\r\n");
            Part($"--{boundary}\r\n");
            Part($"Content-Disposition: form-data; name=\"files[0]\"; filename=\"{fileName}\"\r\n");
            Part("Content-Type: text/plain\r\n\r\n");
            Part(content);
            Part($"\r\n--{boundary}--\r\n");

            return stream.ToArray();
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
