using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Session state and chat parsing for a trade with a CyWorks/CyTrader bot. Separate from
    // TradeView (presentation) the same way Target is separate from TargetView: this owns the
    // regexes + state, PluginCore feeds it events/chat, and the view repaints via OnChanged.
    // (The partner's offered *items* live in ItemList.Trade — this is the surrounding session.)
    public static class Trade
    {
        // The bot prices items in "points"; we pay with these trade notes ("MMDs"). One note is
        // worth PointsPerMmd points — the bot tells us the rate when we ask it for "points".
        private const string PaymentItemName = "Trade Note (250,000)";

        // Current stack size of a WorldObject (LongValueKey.StackCount; raw key to match the
        // value cytrader reads — 0xD000006). Non-stacked items count as 1.
        private const int StackCountKey = unchecked((int)0xD000006);

        // Chat patterns for the bot, matched in PluginCore's chat handler (same style as
        // Target/QuestFlag). Group 1 is the sender; CheckPriceRegex also captures item + points.
        public static readonly Regex TradeStartedRegex = new Regex("^(.+?) tells you, \"//");
        public static readonly Regex CheckPriceRegex = new Regex("^(.+?) tells you, \"// (.+?) is worth ([\\d.,]+) points");

        // "// My points: <name> [<value>], ..." — the items the bot accepts as payment and what
        // each is worth. We pull our note's value out of the (comma-joined) list by name.
        public static readonly Regex PointsReplyRegex = new Regex("^(.+?) tells you, \"// My points: (.*)\"");
        private static readonly Regex NoteValueRegex = new Regex(Regex.Escape(PaymentItemName) + @"\s*\[(\d+(?:\.\d+)?)\]");

        // True between EnterTrade and EndTrade. Lets handlers ignore stray events that arrive
        // while no trade is open, so one trade's leftovers can't bleed into the next.
        public static bool IsOpen = false;

        // The partner we send "add"/"check" tells to, empty when no trade is open.
        public static string PartnerName = "";

        // True once the partner sends a CyWorks-style "// ..." tell.
        public static bool IsCyTrader = false;

        // The last price the bot quoted from a "check".
        public static string PricedItem = "";
        public static string PricePoints = "";

        // Points one trade note (MMD) is worth, learned from the bot's "points" reply. 0 = unknown
        // (we then treat a note as one point, matching the original behaviour).
        public static int PointsPerMmd = 0;

        // The bot's raw "points" reply (the list of payment items + values), shown verbatim in
        // the status line so the player sees exactly what the bot accepts and at what value.
        public static string PointsList = "";

        // The single status line the view renders. Blank until a bot trade opens (then set once
        // to the bot's points list), and overwritten by each check/buy result.
        public static string TradeStatus = "";

        // True when the last price check found we can afford the item — drives the Add button's
        // label ("Checkout" vs "Add to Trade"). The button works either way.
        public static bool CanCheckout = false;

        // Ids of items we owned when the trade opened. An item we drag into the trade pane
        // can leave our inventory chain, so a live check can't always tell our own offers from
        // the partner's — this snapshot does.
        private static readonly HashSet<int> MyItems = new HashSet<int>();

        // The last item we price-checked and how many notes (MMDs) its price needs. Add uses
        // these to decide payment without re-checking. PayNotes is the amount to drag in once the
        // added item lands; PendingSplitCount is the stack split we're waiting on.
        private static int LastCheckId = 0;
        private static int LastCheckNotes = 0;
        private static int PayNotes = 0;
        private static int PendingSplitCount = 0;

        // Ask the bot for its point values once per trade (on first bot detection).
        private static bool AskedPoints = false;

        // Fired whenever the visible state changes so the view can repaint.
        public static Action OnChanged;

        // A trade window opened. Snapshot our inventory, work out the partner, and reset the
        // item list + session for a clean start.
        public static void Begin(EnterTradeEventArgs e)
        {
            int myId = CoreManager.Current.CharacterFilter.Id;
            int partnerId = e.TradeeId == myId ? e.TraderId : e.TradeeId;
            string partnerName = CoreManager.Current.WorldFilter[partnerId]?.Name ?? "";

            SnapshotInventory();
            ItemList.Trade.Clear();
            Set(partnerName, true);
        }

        // The trade window closed. Wipe everything so the next trade starts fresh.
        public static void End()
        {
            MyItems.Clear();
            ItemList.Trade.Clear();
            Set("", false);
        }

        // Both sides cleared their offered items; the window stays open. Drop the list.
        // (Does NOT clear the auto-pay state — the bot resets mid-Buy, then adds the item.)
        public static void Reset()
        {
            ItemList.Trade.Clear();
            OnChanged?.Invoke();
        }

        // An item was dropped into the trade window. Show only the partner's side — skip our
        // own offers. When this is the item from a Buy, pay for it now (after the bot's reset).
        public static void AddItem(int itemId)
        {
            if (!IsOpen) return;
            if (IsOurs(CoreManager.Current.WorldFilter[itemId])) return;

            ItemList.Trade.AddTradeItem(itemId);

            if (PayNotes > 0)
            {
                int notes = PayNotes;
                PayNotes = 0;
                PayWithNotes(notes);
            }
        }

        // Add the item to the trade window. No price check here — selecting the item already did
        // one. If that check showed we can afford it, pay the notes when it lands; otherwise add
        // it without dragging any notes in and let the player sort out the funds. Add by item id
        // (exact, unlike a name which can match several items).
        public static void Add(int itemId)
        {
            SendCommand("add " + itemId);
            PayNotes = (CanCheckout && LastCheckId == itemId) ? LastCheckNotes : 0;
        }

        // Price-check an item (e.g. when it's selected). The reply (NotePriceTell) updates the
        // status, the affordability flag, and the note count Add will use.
        public static void CheckPrice(int itemId)
        {
            LastCheckId = itemId;
            SendCommand("check " + itemId);
        }

        // A CyWorks-style "// ..." tell from the partner — flag it as a CyTrader bot.
        public static void NoteBotTell(string chatText)
        {
            Match m = TradeStartedRegex.Match(chatText);
            if (m.Success && IsPartner(m.Groups[1].Value)) MarkCyTrader();
        }

        // The bot's "points" reply. Pull our note's value out of the list to learn the MMD rate.
        public static void NotePointsTell(string chatText)
        {
            Match m = PointsReplyRegex.Match(chatText);
            if (!m.Success || !IsPartner(m.Groups[1].Value)) return;

            MarkCyTrader();

            PointsList = m.Groups[2].Value.Trim();

            Match v = NoteValueRegex.Match(PointsList);
            PointsPerMmd = (v.Success && double.TryParse(v.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                ? (int)Math.Round(d)
                : 0;

            // The one-time line shown on opening a bot trade: what the bot takes as payment.
            TradeStatus = PointsList.Length > 0 ? $"Points: {PointsList}" : "";
            OnChanged?.Invoke();
        }

        // A price-check reply ("// <item> is worth <N> points."). Shows the price in points and
        // MMDs with whether we can afford it, and records what Add would need to pay.
        public static void NotePriceTell(string chatText)
        {
            Match m = CheckPriceRegex.Match(chatText);
            if (!m.Success || !IsPartner(m.Groups[1].Value)) return;

            PricedItem = m.Groups[2].Value;
            PricePoints = m.Groups[3].Value;

            ParsePoints(PricePoints, out double price);
            int mmdsNeeded = MmdsFor(price);

            GatherNotes(out int have);
            LastCheckNotes = mmdsNeeded;
            CanCheckout = have >= mmdsNeeded;

            TradeStatus = CanCheckout
                ? $"{PriceLabel()} -- You have enough ({mmdsNeeded} MMD)"
                : $"{PriceLabel()} -- Insufficient funds. Need {mmdsNeeded - have} more MMD";
            OnChanged?.Invoke();
        }

        // Send a command tell to the partner, the same way CyTrader bots talk to each other.
        public static void SendCommand(string message)
        {
            if (string.IsNullOrEmpty(PartnerName)) return;
            CoreManager.Current.Actions.InvokeChatParser($"@t {PartnerName}, {message}");
        }

        // A new object appeared — if it's the stack our payment split off, trade it.
        public static void OnObjectCreated(WorldObject wo)
        {
            if (PendingSplitCount <= 0 || wo == null) return;
            if (wo.Name != PaymentItemName || StackCount(wo) != PendingSplitCount) return;

            PendingSplitCount = 0;
            MyItems.Add(wo.Id);   // it's ours — keep it out of the partner's item list
            CoreManager.Current.Actions.TradeAdd(wo.Id);
        }

        // MMDs (trade notes) needed to cover `points` at the current rate; rate unknown → 1:1.
        public static int MmdsFor(double points)
        {
            if (points <= 0) return 0;
            int rate = PointsPerMmd > 0 ? PointsPerMmd : 1;
            return (int)Math.Ceiling(points / rate);
        }

        // "<points> points (<n> MMD)" — the MMD part is dropped when the rate is unknown.
        public static string PointsLabel(double points)
        {
            string s = $"{points:0.##} points";
            if (PointsPerMmd > 0) s += $" ({MmdsFor(points)} MMD)";
            return s;
        }

        // The bot's last price quote, formatted for the view (item + points + MMD equivalent).
        public static string PriceLabel()
        {
            if (PricePoints.Length == 0) return "";
            ParsePoints(PricePoints, out double p);
            return $"{PricedItem}: {PointsLabel(p)}";
        }

        // Our trade notes in inventory; `total` is their combined count (i.e. MMDs on hand).
        private static List<WorldObject> GatherNotes(out int total)
        {
            var notes = new List<WorldObject>();
            total = 0;
            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    if (wo.Name != PaymentItemName) continue;
                    notes.Add(wo);
                    total += StackCount(wo);
                }
            }
            return notes;
        }

        // Add exactly `notes` trade notes to our side of the trade, or chat if we can't. A count
        // of zero means our balance alone covers the item — nothing to add.
        private static void PayWithNotes(int notes)
        {
            if (notes <= 0)
            {
                Util.Chat($"Balance covers {PricedItem}; no {PaymentItemName} needed.", Util.ColorOrange, "[Oracle of Dereth] ");
                return;
            }

            List<WorldObject> have = GatherNotes(out int total);

            if (total < notes)
            {
                Util.Chat($"Not enough {PaymentItemName}: need {notes}, have {total}.", Util.ColorOrange, "[Oracle of Dereth] ");
                return;
            }

            // Add whole stacks (smallest first), splitting the last one for the remainder.
            have.Sort((a, b) => StackCount(a).CompareTo(StackCount(b)));

            int remaining = notes;
            foreach (WorldObject note in have)
            {
                if (remaining <= 0) break;

                int count = StackCount(note);
                if (count <= remaining)
                {
                    MyItems.Add(note.Id);   // ours — keep it out of the partner's item list
                    CoreManager.Current.Actions.TradeAdd(note.Id);
                    remaining -= count;
                }
                else
                {
                    SplitAndAdd(note, remaining);
                    remaining = 0;
                }
            }

            Util.Chat($"Paying {notes} {PaymentItemName} for {PricedItem}.", Util.ColorOrange, "[Oracle of Dereth] ");
        }

        // Split `count` off a stack; OnObjectCreated trades the new stack once it appears.
        private static void SplitAndAdd(WorldObject note, int count)
        {
            PendingSplitCount = count;
            CoreManager.Current.Actions.SelectItem(note.Id);
            CoreManager.Current.Actions.SelectedStackCount = count;
            CoreManager.Current.Actions.MoveItem(note.Id, CoreManager.Current.CharacterFilter.Id, 0, false);
        }

        private static void SnapshotInventory()
        {
            MyItems.Clear();
            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv) { MyItems.Add(wo.Id); }
            }
        }

        // Whether the item is one of ours (vs. the partner's) — by the open-trade snapshot or
        // because it still chains up to us in inventory.
        private static bool IsOurs(WorldObject wo)
        {
            if (wo == null) return false;
            return MyItems.Contains(wo.Id) || ItemList.IsInInventory(wo);
        }

        private static int StackCount(WorldObject wo)
        {
            int c = wo.Values((LongValueKey)StackCountKey, 0);
            return c <= 0 ? 1 : c;
        }

        // Parse a points value that may carry thousands separators ("1,250" -> 1250).
        private static bool ParsePoints(string text, out double value)
        {
            return double.TryParse((text ?? "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static void Set(string partnerName, bool open)
        {
            IsOpen = open;
            PartnerName = partnerName ?? "";
            IsCyTrader = false;
            PricedItem = "";
            PricePoints = "";
            PointsPerMmd = 0;
            PointsList = "";
            TradeStatus = "";
            CanCheckout = false;
            AskedPoints = false;
            LastCheckId = 0;
            LastCheckNotes = 0;
            PayNotes = 0;
            PendingSplitCount = 0;
            OnChanged?.Invoke();
        }

        private static void MarkCyTrader()
        {
            IsCyTrader = true;

            // Learn the MMD rate once per trade. The reply (NotePointsTell) sets the status line
            // to balance + the bot's points list.
            if (!AskedPoints && !string.IsNullOrEmpty(PartnerName))
            {
                AskedPoints = true;
                SendCommand("points");
            }
            OnChanged?.Invoke();
        }

        // Only react to tells from the player we're actually trading with.
        private static bool IsPartner(string sender)
        {
            return !string.IsNullOrEmpty(PartnerName) && sender.IndexOf(PartnerName, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
