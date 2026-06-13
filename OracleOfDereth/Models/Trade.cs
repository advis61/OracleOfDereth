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
        // The bot prices items in "points"; we pay with these trade notes, one point each.
        private const string PaymentItemName = "Trade Note (250,000)";

        // Current stack size of a WorldObject (LongValueKey.StackCount; raw key to match the
        // value cytrader reads — 0xD000006). Non-stacked items count as 1.
        private const int StackCountKey = unchecked((int)0xD000006);

        // Chat patterns for the bot, matched in PluginCore's chat handler (same style as
        // Target/QuestFlag). Group 1 is the sender; CheckPriceRegex also captures item + points.
        public static readonly Regex TradeStartedRegex = new Regex("^(.+?) tells you, \"//");
        public static readonly Regex CheckPriceRegex = new Regex("^(.+?) tells you, \"// (.+?) is worth ([\\d.,]+) points");

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

        // Ids of items we owned when the trade opened. An item we drag into the trade pane
        // can leave our inventory chain, so a live check can't always tell our own offers from
        // the partner's — this snapshot does.
        private static readonly HashSet<int> MyItems = new HashSet<int>();

        // Auto-pay bookkeeping for a Buy: how many points to pay once the bought item lands,
        // and the count we're waiting on a stack split to produce.
        private static bool PayArmed = false;
        private static int PayPoints = 0;
        private static int PendingSplitCount = 0;

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

            if (PayArmed && PayPoints > 0)
            {
                int points = PayPoints;
                PayArmed = false;
                PayPoints = 0;
                PayWithNotes(points);
            }
        }

        // Ask the bot to add an item for purchase and arm auto-payment. The bot replies with a
        // price (NotePriceTell), then adds the item (AddItem), which triggers the payment. We
        // check/add by item id — exact, unlike a name which can match several items.
        public static void Buy(int itemId)
        {
            SendCommand("check " + itemId);
            SendCommand("add " + itemId);
            PayArmed = true;
            PayPoints = 0;
        }

        // A CyWorks-style "// ..." tell from the partner — flag it as a CyTrader bot.
        public static void NoteBotTell(string chatText)
        {
            Match m = TradeStartedRegex.Match(chatText);
            if (m.Success && IsPartner(m.Groups[1].Value)) MarkCyTrader();
        }

        // A price-check reply ("// <item> is worth <N> points.") — record the quote, and if a
        // Buy is armed, remember how many points to pay (rounded up to whole notes).
        public static void NotePriceTell(string chatText)
        {
            Match m = CheckPriceRegex.Match(chatText);
            if (!m.Success || !IsPartner(m.Groups[1].Value)) return;

            MarkCyTrader();
            PricedItem = m.Groups[2].Value;
            PricePoints = m.Groups[3].Value;
            if (PayArmed) PayPoints = PointsFromPrice(PricePoints);
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
            CoreManager.Current.Actions.TradeAdd(wo.Id);
        }

        // Add exactly `points` trade notes to our side of the trade, or chat if we can't.
        private static void PayWithNotes(int points)
        {
            var notes = new List<WorldObject>();
            int total = 0;
            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    if (wo.Name != PaymentItemName) continue;
                    notes.Add(wo);
                    total += StackCount(wo);
                }
            }

            if (total < points)
            {
                Util.Chat($"Not enough {PaymentItemName}: need {points}, have {total}.", Util.ColorOrange, "[Oracle of Dereth] ");
                return;
            }

            // Add whole stacks (smallest first), splitting the last one for the remainder.
            notes.Sort((a, b) => StackCount(a).CompareTo(StackCount(b)));

            int remaining = points;
            foreach (WorldObject note in notes)
            {
                if (remaining <= 0) break;

                int count = StackCount(note);
                if (count <= remaining)
                {
                    CoreManager.Current.Actions.TradeAdd(note.Id);
                    remaining -= count;
                }
                else
                {
                    SplitAndAdd(note, remaining);
                    remaining = 0;
                }
            }

            Util.Chat($"Paying {points} {PaymentItemName} for {PricedItem}.", Util.ColorOrange, "[Oracle of Dereth] ");
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

        // Whole notes needed to cover a quoted price (round up so we never underpay).
        private static int PointsFromPrice(string price)
        {
            if (double.TryParse((price ?? "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                return (int)Math.Ceiling(d);
            return 0;
        }

        private static void Set(string partnerName, bool open)
        {
            IsOpen = open;
            PartnerName = partnerName ?? "";
            IsCyTrader = false;
            PricedItem = "";
            PricePoints = "";
            PayArmed = false;
            PayPoints = 0;
            PendingSplitCount = 0;
            OnChanged?.Invoke();
        }

        private static void MarkCyTrader()
        {
            if (IsCyTrader) return;
            IsCyTrader = true;
            OnChanged?.Invoke();
        }

        // Only react to tells from the player we're actually trading with.
        private static bool IsPartner(string sender)
        {
            return !string.IsNullOrEmpty(PartnerName) && sender.IndexOf(PartnerName, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
