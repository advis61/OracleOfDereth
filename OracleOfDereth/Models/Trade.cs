using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Session state and chat parsing for a trade with a CyWorks/CyTrader bot. Separate from
    // TradeView (presentation) the same way Target is separate from TargetView: this owns the
    // regexes + state, PluginCore feeds it events/chat, and the view repaints via OnChanged.
    // (The partner's offered *items* live in ItemList.Trade — this is the surrounding session.)
    public static class Trade
    {
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

        // Fired whenever the above changes so the view can repaint.
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
        public static void Reset()
        {
            ItemList.Trade.Clear();
            OnChanged?.Invoke();
        }

        // An item was dropped into the trade window. Show only the partner's side — skip our
        // own offers. Ignored when no trade is open (stray event between trades).
        public static void AddItem(int itemId)
        {
            if (!IsOpen) return;
            if (IsOurs(CoreManager.Current.WorldFilter[itemId])) return;
            ItemList.Trade.AddTradeItem(itemId);
        }

        // A CyWorks-style "// ..." tell from the partner — flag it as a CyTrader bot.
        public static void NoteBotTell(string chatText)
        {
            Match m = TradeStartedRegex.Match(chatText);
            if (m.Success && IsPartner(m.Groups[1].Value)) MarkCyTrader();
        }

        // A price-check reply ("// <item> is worth <N> points.") — record the quote.
        public static void NotePriceTell(string chatText)
        {
            Match m = CheckPriceRegex.Match(chatText);
            if (!m.Success || !IsPartner(m.Groups[1].Value)) return;

            MarkCyTrader();
            PricedItem = m.Groups[2].Value;
            PricePoints = m.Groups[3].Value;
            OnChanged?.Invoke();
        }

        // Send a command tell to the partner, the same way CyTrader bots talk to each other.
        public static void SendCommand(string message)
        {
            if (string.IsNullOrEmpty(PartnerName)) return;
            CoreManager.Current.Actions.InvokeChatParser($"@t {PartnerName}, {message}");
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

        private static void Set(string partnerName, bool open)
        {
            IsOpen = open;
            PartnerName = partnerName ?? "";
            IsCyTrader = false;
            PricedItem = "";
            PricePoints = "";
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
