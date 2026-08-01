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

        // The bank is a Conquest feature, so support is just the Conquest check. Used by the UI
        // (e.g. the trade view's bank button). The "/od checkbank" machinery below is a manual
        // probe/diagnostic that reports its result to chat.
        public static bool IsSupported => Server.IsConquest;

        // "/bank" reply on a bank server begins with "[BANK]" (e.g. "[BANK] Bank Commands ...").
        // Anchored to the start of the line (after an optional chat timestamp) so a pasted
        // 'Someone says, "[BANK] ..."' can't spoof our own server reply.
        public static readonly Regex BankReplyRegex = new Regex(@"^\s*(?:\[[^\]]*\]\s*)?\[BANK\]", RegexOptions.IgnoreCase);

        // The client's own reply on a server without bank: "Unknown command: bank". Anchored so a
        // pasted copy of the phrase can't falsely resolve an in-flight bank check.
        public static readonly Regex NoBankReplyRegex = new Regex(@"^\s*(?:\[[^\]]*\]\s*)?unknown command.*bank", RegexOptions.IgnoreCase);

        // Confirmation of a successful withdrawal, e.g.
        // "[BANK] Withdrew 1 250,000 pyreal trade notes (250,000 pyreals). Balance: 349,274,916".
        // Anchored so a pasted 'Someone says, "[BANK] Withdrew ..."' can't trigger a spurious
        // Trade.RecheckFunds() (PluginCore matches this outside the Bank.Matches gate).
        public static readonly Regex WithdrawConfirmRegex = new Regex(@"^\s*(?:\[[^\]]*\]\s*)?\[BANK\]\s*Withdrew", RegexOptions.IgnoreCase);

        // A check is in flight, waiting on a reply.
        private static bool pending = false;

        // Servers whose bank support we already know — skip the live "/bank" probe for these.
        private static readonly Dictionary<string, bool> KnownServers = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { Server.Levistras, false },
            { Server.Conquest, true },
        };

        // "/od checkbank" — resolve from the known-server list if possible, else send "/bank" and
        // wait for the server's reply.
        public static void Check()
        {
            string server = Server.Name;
            if (KnownServers.TryGetValue(server, out bool known)) { Resolve(known); return; }

            pending = true;
            Util.Chat("Checking for bank...", Util.ColorCyan, ChatPrefix);
            Util.Command("/bank");
        }

        // Hard safety cap: never withdraw more than this many MMDs in a single request, no matter
        // what a caller asks for. Guards against a runaway/buggy amount draining the bank.
        public const int MaxWithdrawMmds = 5000;

        // Withdraw `mmds` MMD trade notes (250k each) from the server bank. The base "trade notes"
        // denomination is MMD, so the command is "/b w n mmd <count>". The trade view uses it to
        // cover a purchase shortfall. Requests over the MaxWithdrawMmds cap are refused outright
        // rather than partially filled.
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

        // ---- Bank command API (Conquest "/bank ...") ---------------------------------------------
        // A bank currency: its UI label and the token passed to the "/bank" command. The server's
        // currency parser (Conquest-ACE PlayerCommands.cs) matches CASE-SENSITIVELY on the exact
        // CamelCase names OR the single-letter abbreviations. We use the abbreviations (all
        // lowercase) to sidestep the casing traps (e.g. "Eventtokens", lowercase "notes"):
        //   p=Pyreals  l=Luminance  e=Eventtokens  c=ConquestCoins  s=SoulFragments
        //   n=Notes (default MMD 250k denomination)  k=LegendaryKeys
        public sealed class Currency
        {
            public string Label { get; }
            public string Token { get; }
            public Currency(string label, string token) { Label = label; Token = token; }
        }

        // Everything the player can withdraw. Luminance is excluded — the server spends it directly
        // from the bank and refuses to withdraw it. Non-note currencies first (alphabetical), then
        // note denominations largest to smallest. MMD Notes is the preselected default (see
        // DefaultWithdrawIndex), not the first entry.
        public static readonly IReadOnlyList<Currency> Withdrawable = new List<Currency>
        {
            new Currency("Conquest Coins", "c"),
            new Currency("Event Tokens", "e"),
            new Currency("Legendary Keys", "k"),
            new Currency("Pyreals", "p"),
            new Currency("Soul Fragments", "s"),

            // Note denominations, largest to smallest. The token is "n <denom>", so the amount field
            // is the COUNT of notes (e.g. "n D" + 10 -> "/bank withdraw n D 10" = ten 50k notes).
            // MMD (250k) is the plain "n" default.
            new Currency("MMD Notes (250k)", "n"),
            new Currency("MM Notes (200k)", "n MM"),
            new Currency("M Notes (100k)", "n M"),
            new Currency("D Notes (50k)", "n D"),
            new Currency("C Notes (10k)", "n C"),
            new Currency("L Notes (5k)", "n L"),
            new Currency("X Notes (1k)", "n X"),
            new Currency("V Notes (500)", "n V"),
            new Currency("I Notes (100)", "n I"),
        };

        // The withdraw option the UI preselects: MMD Notes (the plain "n" default), which isn't the
        // first entry in the list.
        public static readonly int DefaultWithdrawIndex = IndexOfToken(Withdrawable, "n");

        private static int IndexOfToken(IReadOnlyList<Currency> list, string token)
        {
            for (int i = 0; i < list.Count; i++) { if (list[i].Token == token) { return i; } }
            return 0;
        }

        // What the player can transfer to another character. Only these three are handled by the
        // server's transfer switch (Conquest-ACE PlayerCommands.cs cases 1/2/7); every other
        // currency — Notes included — silently no-ops, so they're deliberately not offered here.
        // Alphabetical.
        public static readonly IReadOnlyList<Currency> Transferable = new List<Currency>
        {
            new Currency("Legendary Keys", "k"),
            new Currency("Luminance", "l"),
            new Currency("Pyreals", "p"),
        };

        // The transfer option the UI preselects: Luminance.
        public static readonly int DefaultTransferIndex = IndexOfToken(Transferable, "l");

        // Deposit all bankable items ("/bank deposit").
        public static void DepositAll() { Util.Command("/bank deposit"); }

        // Withdraw a whole-number amount of a currency ("/bank withdraw <token> <amount>").
        public static void Withdraw(Currency currency, string amount)
        {
            if (currency == null || string.IsNullOrEmpty(amount)) return;
            Util.Command($"/bank withdraw {currency.Token} {amount}");
        }

        // Transfer a whole-number amount of a currency to another character
        // ("/bank transfer <token> <amount> "<target>""). The target is quoted because character
        // names contain a space (first + last), which the server otherwise reads as extra args.
        public static void Transfer(Currency currency, string amount, string target)
        {
            if (currency == null || string.IsNullOrEmpty(amount) || string.IsNullOrEmpty(target)) return;
            Util.Command($"/bank transfer {currency.Token} {amount} \"{target}\"");
        }

        // ---- Auto-deposit ------------------------------------------------------------------------
        // Enabled state persists in settings.xml (key "BankAutoDeposit"); the Bank tab's checkbox is
        // just a view onto it. AutoDepositTick, driven from the main 1s tick, fires "/bank deposit"
        // once at each 10-minute wall-clock mark (:00/:10/.../:50) while enabled.
        private const string AutoDepositKey = "BankAutoDeposit";
        private static long lastAutoDepositBlock = -1;

        public static bool AutoDepositEnabled
        {
            get => SettingsFile.GetSetting(AutoDepositKey, "No") == "Yes";
            set => SettingsFile.PutSetting(AutoDepositKey, value ? "Yes" : "No");
        }

        public static void AutoDepositTick()
        {
            if (!Server.IsConquest || !AutoDepositEnabled) return;

            DateTime now = DateTime.Now;
            if (now.Minute % 10 != 0) return;

            long block = now.Ticks / TimeSpan.TicksPerMinute / 10;
            if (block == lastAutoDepositBlock) return;

            lastAutoDepositBlock = block;
            AutoDeposit.Sent();
            DepositAll();
            Util.Chat("Auto-deposited to bank.", Util.ColorPink);
        }

        // Marks the window in which the reply to an automatic deposit is expected. Deliberately set
        // only here and not in DepositAll: pressing "Deposit Now" is you asking, so that reply keeps
        // printing. This one fires unattended every ten minutes, and its nine-line reply is the
        // spam worth losing — the "Auto-deposited to bank." line above still confirms it ran.
        private static readonly ChatRequest AutoDeposit = new ChatRequest();

        // The reply to "/bank deposit": a "No <currency> found to deposit" line per currency that
        // had nothing to move, a "Deposited <amount> <currency>" line per one that did, and a
        // closing summary. Anything opening with "Deposited" is taken, rather than enumerating
        // every currency's wording. That breadth is safe because it's claimed only while an
        // automatic deposit is awaiting its reply — the same output from the button or a
        // hand-typed command is left alone. Nothing here is worth parsing; it exists purely to be
        // suppressed.
        private static readonly Regex DepositReplyRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*(?:No .+? (?:found|available) to deposit|Deposited\b.*)\s*$",
            RegexOptions.IgnoreCase);

        public static bool MatchesAutoDeposit(string text)
        {
            return text != null && AutoDeposit.Awaiting && DepositReplyRegex.IsMatch(text);
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
            Util.Chat(supported ? "Yes bank" : "No bank", Util.ColorCyan, ChatPrefix);
        }
    }
}
