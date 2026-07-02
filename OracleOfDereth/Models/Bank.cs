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

        // The bank is a Conquest feature, so support is just the Conquest check. Used by the UI
        // (e.g. the trade view's bank button) instead of the async "/od checkbank" probe above.
        public static bool IsSupported => Server.IsConquest;

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
        // from the bank and refuses to withdraw it. MMD Notes is first (the UI default); the rest
        // are alphabetical.
        public static readonly IReadOnlyList<Currency> Withdrawable = new List<Currency>
        {
            new Currency("MMD Notes", "n"),
            new Currency("Conquest Coins", "c"),
            new Currency("Event Tokens", "e"),
            new Currency("Legendary Keys", "k"),
            new Currency("Pyreals", "p"),
            new Currency("Soul Fragments", "s"),
        };

        // What the player can transfer to another character. MMD Notes first (the UI default).
        public static readonly IReadOnlyList<Currency> Transferable = new List<Currency>
        {
            new Currency("MMD Notes", "n"),
            new Currency("Legendary Keys", "k"),
            new Currency("Luminance", "l"),
        };

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
