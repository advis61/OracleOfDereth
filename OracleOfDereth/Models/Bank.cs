using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Backs the "/od checkbank" command. Some servers add a bank feature that responds to "/bank"
    // with a "[BANK] ..." line; servers without it answer "Unknown command: bank". We fire "/bank"
    // and watch chat for whichever reply comes back. The check is asynchronous — the reply resolves
    // it — so PluginCore feeds us the relevant chat lines (NoteChat), keeping event wiring
    // centralized there rather than subscribing here. There's no timeout: if neither reply is ever
    // seen the check just stays open until the next "/od checkbank" supersedes it.
    public static class Bank
    {
        private const string ChatPrefix = "[OD] ";

        // Whether this server supports the bank feature: null = not checked yet, true/false = known.
        public static bool? Supported = null;

        // "/bank" reply on a bank server begins with "[BANK]" (e.g. "[BANK] Bank Commands ...").
        public static readonly Regex BankReplyRegex = new Regex(@"\[BANK\]", RegexOptions.IgnoreCase);

        // The client's reply on a server without bank: "Unknown command: bank".
        public static readonly Regex NoBankReplyRegex = new Regex(@"unknown command.*bank", RegexOptions.IgnoreCase);

        // Confirmation of a successful withdrawal, e.g.
        // "[BANK] Withdrew 1 250,000 pyreal trade notes (250,000 pyreals). Balance: 349,274,916".
        public static readonly Regex WithdrawConfirmRegex = new Regex(@"\[BANK\]\s*Withdrew", RegexOptions.IgnoreCase);

        // A check is in flight, waiting on a reply.
        private static bool pending = false;

        // Servers whose bank support we already know — skip the live "/bank" probe for these.
        private static readonly Dictionary<string, bool> KnownServers = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "Levistras", false },
            { "Conquest", true },
        };

        // "/od checkbank" — resolve from the known-server list if possible, else send "/bank" and
        // wait for the server's reply.
        public static void Check()
        {
            string server = CoreManager.Current.CharacterFilter.Server ?? "";
            if (KnownServers.TryGetValue(server, out bool known)) { Resolve(known); return; }

            pending = true;
            Util.Chat("Checking for bank...", Util.ColorCyan, ChatPrefix);
            Util.Command("/bank");
        }

        // Silently set Supported from the known-server list if it isn't determined yet. Lets the UI
        // (e.g. the trade view's bank button) know a server like Conquest has bank without the
        // player having run "/od checkbank". No-op once Supported is known, and never probes.
        public static void ResolveKnownServer()
        {
            if (Supported != null) return;
            string server = CoreManager.Current.CharacterFilter.Server ?? "";
            if (KnownServers.TryGetValue(server, out bool known)) Supported = known;
        }

        // Hard safety cap: never withdraw more than this many MMDs in a single request, no matter
        // what a caller asks for. Guards against a runaway/buggy amount draining the bank.
        public const int MaxWithdrawMmds = 5000;

        // Withdraw `mmds` MMD trade notes (250k each) from the server bank. The base "trade notes"
        // denomination is MMD, so the command is "/b w n mmd <count>". First of the (eventual) bank
        // command API; the trade view uses it to cover a purchase shortfall. Requests over the
        // MaxWithdrawMmds cap are refused outright rather than partially filled.
        public static void Withdraw(int mmds)
        {
            if (mmds <= 0) return;
            if (mmds > MaxWithdrawMmds)
            {
                Util.Chat($"Refusing to withdraw {mmds} MMD — over the {MaxWithdrawMmds} MMD safety cap.", Util.ColorPink, ChatPrefix);
                return;
            }
            Util.Command($"/b w n mmd {mmds}");
        }

        // True when this chat line is one of the replies we're waiting on — lets PluginCore route
        // only the relevant lines here.
        public static bool Matches(string text)
        {
            return text != null && (BankReplyRegex.IsMatch(text) || NoBankReplyRegex.IsMatch(text));
        }

        // Forwarded from PluginCore's chat handler. While a check is pending, "[BANK]" means this
        // server has bank and "Unknown command: bank" means it doesn't. (Self-guards so it's a
        // no-op when no check is running.)
        public static void NoteChat(string text)
        {
            if (!pending || text == null) return;
            if (BankReplyRegex.IsMatch(text)) { Resolve(true); }
            else if (NoBankReplyRegex.IsMatch(text)) { Resolve(false); }
        }

        private static void Resolve(bool supported)
        {
            pending = false;
            Supported = supported;
            Util.Chat(supported ? "Yes bank" : "No bank", Util.ColorCyan, ChatPrefix);
        }
    }
}
