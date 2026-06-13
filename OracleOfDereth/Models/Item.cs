using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    public class Item
    {
        // Collection of Items
        public static List<Item> Items = new List<Item>();
        public static SortType CurrentSortType = SortType.NameAscending;

        public enum SortType
        {
            NameAscending,
            NameDescending,
            Col1Ascending,
            Col1Descending,
            Col2Ascending,
            Col2Descending,
            Col3Ascending,
            Col3Descending,
            Col4Ascending,
            Col4Descending,
        }

        // In-flight identify requests: item id -> when we sent it + how many tries.
        // Tracked so a dropped server response can be retried instead of stalling.
        private struct Pending { public DateTime SentAt; public int Attempts; }
        private static Dictionary<int, Pending> PendingIds = new Dictionary<int, Pending>();

        // Queue of item ids waiting to be identified (for Add All)
        private static List<int> IdentifyQueue = new List<int>();
        public static bool IsProcessingQueue = false;

        // Up to this many identify requests in flight at once. Safe to keep high
        // because Tick() retries/drops anything the server doesn't answer.
        private const int MaxConcurrentRequests = 6;
        private static readonly TimeSpan IdTimeout = TimeSpan.FromSeconds(3);
        private const int MaxIdAttempts = 3;

        // Throttle list rebuilds during bulk identify (sort + repaint is O(n)).
        private static DateTime _lastRefresh = DateTime.MinValue;
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

        public static bool AutoAddEnabled = false;

        // Callbacks to refresh the UI / signal the identify queue finished.
        public static Action OnItemsListChanged;
        public static Action OnQueueFinished;

        // Properties
        public string Name = "";
        public int Id = 0;
        public int Icon = 0;
        public int ObjectClassId = 0;
        public int SortCategory = 0; // Groups like items together: 0=weapon, 1=armor, 2=jewelry, 3=cloak, 4=summon, 5=aetheria, 9=other
        public string SummaryCol1 = "";
        public string SummaryCol2 = "";
        public string SummaryCol3 = "";
        public string SummaryCol4 = "";
        public int SortCol2 = 0;
        public int SortCol3 = 0;
        public int SortCol4 = 0;
        public string Description = "";

        public static void Init()
        {
            Items.Clear();
            PendingIds.Clear();
            IdentifyQueue.Clear();
            IsProcessingQueue = false;
            _lastRefresh = DateTime.MinValue;
        }

        public static int QueueCount => IdentifyQueue.Count + PendingIds.Count;

        public static bool IsTradeable(WorldObject wo)
        {
            if (wo == null) return false;
            if (wo.Values(LongValueKey.Attuned, 0) > 0) return false;

            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (wo.ObjectClass == ObjectClass.MissileWeapon && wo.Values(LongValueKey.StackMax, 0) > 0) return false;

            if (!IsInInventory(wo)) return false;
            return true;
        }

        private static bool IsInInventory(WorldObject wo)
        {
            int characterId = CoreManager.Current.CharacterFilter.Id;
            int containerId = wo.Container;
            while (containerId != 0)
            {
                if (containerId == characterId) return true;
                WorldObject container = CoreManager.Current.WorldFilter[containerId];
                if (container == null) return false;
                containerId = container.Container;
            }
            return false;
        }

        private static bool IsAddAllClass(ObjectClass objClass)
        {
            return objClass == ObjectClass.MeleeWeapon
                || objClass == ObjectClass.MissileWeapon
                || objClass == ObjectClass.WandStaffOrb
                || objClass == ObjectClass.Armor
                || objClass == ObjectClass.Clothing
                || objClass == ObjectClass.Jewelry
                || objClass == ObjectClass.Misc   // Aetheria and Summons
                || objClass == ObjectClass.Salvage;
        }

        /// <summary>
        /// Request to add an item by id. If already identified, adds it immediately.
        /// Otherwise it's queued for identification and added when the id arrives
        /// (via IdentReceived). Returns true if added immediately.
        /// </summary>
        public static bool RequestAdd(int id)
        {
            if (id == 0) return false;

            WorldObject wo = CoreManager.Current.WorldFilter[id];
            if (wo == null) return false;

            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (!IsInInventory(wo)) return false;
            if (Items.Any(t => t.Id == id)) return false;
            if (PendingIds.ContainsKey(id) || IdentifyQueue.Contains(id)) return false;

            if (wo.HasIdData)
            {
                AddFromWorldObject(wo);
                RefreshList();
                return true;
            }

            IdentifyQueue.Add(id);
            PumpQueue();
            return false;
        }

        /// <summary>
        /// Scans the entire inventory. Adds already-identified tradeable items
        /// immediately and queues the rest for identification.
        /// </summary>
        public static void AddAll()
        {
            bool added = false;

            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    if (!IsInInventory(wo)) continue;
                    if (Items.Any(t => t.Id == wo.Id)) continue;
                    if (PendingIds.ContainsKey(wo.Id) || IdentifyQueue.Contains(wo.Id)) continue;

                    // Salvage and Spell Components don't need identification
                    if (wo.ObjectClass == ObjectClass.Salvage || wo.ObjectClass == ObjectClass.SpellComponent)
                    {
                        AddFromWorldObject(wo);
                        added = true;
                        continue;
                    }

                    if (wo.HasIdData)
                    {
                        if (IsTradeable(wo)) { AddFromWorldObject(wo); added = true; }
                    }
                    else
                    {
                        IdentifyQueue.Add(wo.Id);
                    }
                }
            }

            if (added) RefreshList();
            PumpQueue();
        }

        // Issue identify requests until we hit the concurrency cap. Items already
        // identified (by us or another plugin) are added without a request.
        private static void PumpQueue()
        {
            bool added = false;

            while (IdentifyQueue.Count > 0 && PendingIds.Count < MaxConcurrentRequests)
            {
                int id = IdentifyQueue[0];
                IdentifyQueue.RemoveAt(0);

                WorldObject wo = CoreManager.Current.WorldFilter[id];
                if (wo == null) continue;
                if (Items.Any(t => t.Id == id)) continue;
                if (PendingIds.ContainsKey(id)) continue;

                // Skip non-tradeable Misc items (not summons or aetheria)
                if (wo.ObjectClass == ObjectClass.Misc)
                {
                    ItemInfo check = new ItemInfo(wo);
                    if (!check.IsSummon && !check.IsAetheria && !check.IsFoolproof) continue;
                }

                // Already identified — no request needed
                if (wo.HasIdData)
                {
                    if (IsTradeable(wo)) { AddFromWorldObject(wo); added = true; }
                    continue;
                }

                SendId(id);
            }

            if (added) MaybeRefresh();
            UpdateProcessingState();
        }

        // Send an identify request and remember when, for timeout/retry in Tick().
        private static void SendId(int id)
        {
            PendingIds[id] = new Pending { SentAt = DateTime.UtcNow, Attempts = 1 };
            CoreManager.Current.Actions.RequestId(id);
        }

        // An identification arrived (forwarded from PluginCore's ChangeObject).
        // Handles single Adds and the Add All queue uniformly.
        public static void IdentReceived(WorldObject changed)
        {
            if (changed == null) return;

            bool wasPending = PendingIds.Remove(changed.Id);
            bool wasQueued = IdentifyQueue.Remove(changed.Id);
            if (!wasPending && !wasQueued) return;

            if (IsTradeable(changed)) AddFromWorldObject(changed);

            PumpQueue();    // refill the freed slot
            MaybeRefresh();
        }

        // Called once per second from the plugin tick. Re-issues or gives up on
        // identify requests the server never answered, so a dropped response
        // can't permanently stall the queue.
        public static void Tick()
        {
            if (PendingIds.Count == 0) return;

            DateTime now = DateTime.UtcNow;
            List<int> timedOut = null;
            foreach (var kvp in PendingIds)
            {
                if (now - kvp.Value.SentAt < IdTimeout) continue;
                (timedOut ?? (timedOut = new List<int>())).Add(kvp.Key);
            }
            if (timedOut == null) return;

            foreach (int id in timedOut)
            {
                Pending p = PendingIds[id];
                if (p.Attempts < MaxIdAttempts && CoreManager.Current.WorldFilter[id] != null)
                {
                    PendingIds[id] = new Pending { SentAt = now, Attempts = p.Attempts + 1 };
                    CoreManager.Current.Actions.RequestId(id);
                }
                else
                {
                    PendingIds.Remove(id);   // give up; free the slot
                }
            }

            PumpQueue();
            MaybeRefresh();
        }

        // True while there's outstanding identify work; flips the "Adding..." button
        // back and fires OnQueueFinished when the last request resolves.
        private static void UpdateProcessingState()
        {
            bool active = PendingIds.Count > 0 || IdentifyQueue.Count > 0;

            if (active)
            {
                IsProcessingQueue = true;
            }
            else if (IsProcessingQueue)
            {
                IsProcessingQueue = false;
                RefreshList();          // final sorted repaint
                OnQueueFinished?.Invoke();
            }
        }

        // Sort + repaint now.
        private static void RefreshList()
        {
            Sort(CurrentSortType);
            _lastRefresh = DateTime.UtcNow;
            OnItemsListChanged?.Invoke();
        }

        // Sort + repaint, but at most once per RefreshInterval, so a bulk identify
        // doesn't re-sort and rebuild the whole list on every single item.
        private static void MaybeRefresh()
        {
            if (DateTime.UtcNow - _lastRefresh < RefreshInterval) return;
            RefreshList();
        }

        public static void CancelQueue()
        {
            IdentifyQueue.Clear();
            PendingIds.Clear();

            if (IsProcessingQueue)
            {
                IsProcessingQueue = false;
                RefreshList();
                OnQueueFinished?.Invoke();
            }
        }

        public static string StatusText()
        {
            int pending = IdentifyQueue.Count + PendingIds.Count;
            if (pending > 0) return $"Items: {Items.Count} done, {pending} pending";

            return $"Items: {Items.Count} selected";
        }

        private static void AddFromWorldObject(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Add(new Item
            {
                Id = wo.Id,
                Name = info.GetName(),
                Icon = wo.Icon,
                ObjectClassId = (int)wo.ObjectClass,
                SortCategory = GetSortCategory(info),
                SummaryCol1 = GetSummaryCol1(info),
                SummaryCol2 = GetSummaryCol2(info),
                SummaryCol3 = GetSummaryCol3(info),
                SummaryCol4 = GetSummaryCol4(info),
                SortCol2 = GetSortInt(info.GetODValue()),
                SortCol3 = GetSortInt(info.GetOMValue()),
                SortCol4 = GetSortInt(info.GetOAValue()),
                Description = info.ToString(),
            });
        }


        private static string GetSummaryCol1(ItemInfo info)
        {
            return info.GetItemSlotName();
        }

        private static string GetSummaryCol2(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetODString();
            if (info.IsCloak) return info.GetCloakProc();
            if (info.IsSummon) return "DMG " + info.GetSummonDamageString();
            if (info.IsAetheria) return info.GetSetName();
            if (info.IsArmorClothing) return info.GetSetName();
            return "";
        }

        private static string GetSummaryCol3(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOMString();
            if (info.IsCloak) return info.GetRatingsString();
            if (info.IsSummon) return "DEF " + info.GetSummonDefenseString();
            if (info.IsAetheria) return info.GetAetheriaSurge();
            if (info.IsArmorClothing) return info.GetRatingsString();
            if (info.IsJewelry) return info.GetRatingsString();
            return "";
        }

        private static string GetSummaryCol4(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOAString();
            if (info.IsCloak) return $"Level {info.GetCloakLevel()}, {info.GetFullSetName()}";
            if (info.IsAetheria) return info.GetAetheriaLevel() > 0 ? "Level " + info.GetAetheriaLevel() : "";
            if (info.IsArmorClothing || info.IsJewelry) return info.GetCantripsString();
            return "";
        }

        private static int GetSortCategory(ItemInfo info)
        {
            if (info.IsWeapon) return 0;
            if (info.IsClothing) return 7;
            if (info.IsArmorClothing) return 1;
            if (info.IsJewelry) return 2;
            if (info.IsCloak) return 3;
            if (info.IsSummon) return 4;
            if (info.IsAetheria) return 5;
            if (info.IsSalvage || info.IsFoolproof) return 6;
            return 9;
        }

        private static int GetSortInt(int? value)
        {
            return value ?? 0;
        }

        private static bool IsEmpty(string s) => string.IsNullOrEmpty(s);

        public static void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case SortType.NameAscending:
                    Items = Items.OrderBy(t => t.Name).ToList();
                    break;
                case SortType.NameDescending:
                    Items = Items.OrderByDescending(t => t.Name).ToList();
                    break;
                case SortType.Col1Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenBy(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col1Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenByDescending(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol2).ThenBy(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol2).ThenByDescending(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol3).ThenBy(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol3).ThenByDescending(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol4).ThenBy(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol4).ThenByDescending(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
            }
        }

        // Adds a row without sorting/repainting — callers refresh in batches.
        public static void Add(Item item)
        {
            if (Items.Any(t => t.Id == item.Id)) return;
            Items.Add(item);
        }

        public static void Remove(int id)
        {
            Items.RemoveAll(t => t.Id == id);
        }

        public static void Clear()
        {
            Items.Clear();
        }

        private static string ExportFilename(string extension)
        {
            string name = Regex.Replace(CoreManager.Current.CharacterFilter.Name.ToLower(), "[^a-z]", "-");
            return $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}-items.{extension}";
        }

        public static string ExportToText()
        {
            string filename = ExportFilename("txt");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

            var lines = Items.Select(t => t.Description).ToList();
            File.WriteAllLines(path, lines);

            return path;
        }

        private static string[] GetExportHeaders()
        {
            return new[] {
                "Character", "Server", "Name", "ObjectClass", "Type", "Set", "Armor Level", "Imbues", "Tinks",
                "OD", "OA", "OM", "Damage", "Dmg Low", "Dmg High", "Elem Bonus", "Missile %", "Caster %",
                "Attack", "Melee D", "Magic D", "Missile D", "Mana C",
                "Spells", "Wield Req", "Wield Req Level", "Activation Req", "Res Cleaving",
                "Lore", "Craft", "Value", "Burden",
                "Summon DMG", "Summon DEF",
                "Item Level",
                "D", "DR", "C", "CR", "CD", "CDR", "HB", "V"
            };
        }

        private static string[] GetExportRow(Item item)
        {
            WorldObject wo = CoreManager.Current.WorldFilter[item.Id];
            if (wo == null) return new[] { item.Name };

            ItemInfo info = new ItemInfo(wo);

            return new[] {
                CoreManager.Current.CharacterFilter.Name,
                CoreManager.Current.CharacterFilter.Server,
                info.GetName(),
                info.GetObjectClassName(),
                info.GetItemSlotName(),
                info.GetFullSetName(),
                info.GetArmorLevel() > 0 ? info.GetArmorLevel().ToString() : "",
                info.GetImbueString(),
                info.GetTinksValue() > 0 ? info.GetTinksValue().ToString() : "",
                info.GetODValue()?.ToString() ?? "",
                info.GetOAValue()?.ToString() ?? "",
                info.GetOMValue()?.ToString() ?? "",
                info.GetDamageString(),
                info.GetWeaponDamageLow() > 0 ? info.GetWeaponDamageLow().ToString("N2") : "",
                info.GetWeaponDamageHigh() > 0 ? info.GetWeaponDamageHigh().ToString() : "",
                info.GetElementalDamageBonus() != 0 ? info.GetElementalDamageBonus().ToString() : "",
                info.GetDamageBonusPct() != 0 ? info.GetDamageBonusPct().ToString() : "",
                info.GetElementalDamageVsMonsters() != 0 ? info.GetElementalDamageVsMonsters().ToString() : "",
                info.GetAttackBonus() != 0 ? info.GetAttackBonus().ToString() : "",
                info.GetMeleeDefenseBonus() != 0 ? info.GetMeleeDefenseBonus().ToString() : "",
                info.GetMagicDefenseBonus() != 0 ? info.GetMagicDefenseBonus().ToString() : "",
                info.GetMissileDefenseBonus() != 0 ? info.GetMissileDefenseBonus().ToString() : "",
                info.GetManaConversionBonus() != 0 ? info.GetManaConversionBonus().ToString() : "",
                info.GetSpellsString(),
                info.GetWieldReqName(),
                info.GetWieldReqLevel() > 0 ? info.GetWieldReqLevel().ToString() : "",
                info.GetActivationReqString(),
                info.GetResistanceCleavingString(),
                info.GetLoreValue() > 0 ? info.GetLoreValue().ToString() : "",
                info.GetWorkmanshipString(),
                info.GetValue() > 0 ? info.GetValue().ToString() : "",
                info.GetBurden() > 0 ? info.GetBurden().ToString() : "",
                info.GetSummonDamageString(),
                info.GetSummonDefenseString(),
                info.IsCloak ? info.GetCloakLevel().ToString() : info.IsAetheria ? info.GetAetheriaLevel().ToString() : "",
                info.RatingDamage > 0 ? info.RatingDamage.ToString() : "",
                info.RatingDamageResist > 0 ? info.RatingDamageResist.ToString() : "",
                info.RatingCrit > 0 ? info.RatingCrit.ToString() : "",
                info.RatingCritResist > 0 ? info.RatingCritResist.ToString() : "",
                info.RatingCritDamage > 0 ? info.RatingCritDamage.ToString() : "",
                info.RatingCritDamageResist > 0 ? info.RatingCritDamageResist.ToString() : "",
                info.RatingHealBoost > 0 ? info.RatingHealBoost.ToString() : "",
                info.RatingVitality > 0 ? info.RatingVitality.ToString() : "",
            };
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        public static string ExportToCsv()
        {
            string filename = ExportFilename("csv");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

            var lines = new List<string>();
            lines.Add(string.Join(",", GetExportHeaders().Select(CsvEscape)));

            foreach (var item in Items)
            {
                lines.Add(string.Join(",", GetExportRow(item).Select(CsvEscape)));
            }

            File.WriteAllLines(path, lines);
            return path;
        }

        private static string JsonEscape(string value)
        {
            if (value == null) return "null";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        public static string ExportToJson()
        {
            string filename = ExportFilename("json");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

            var headers = GetExportHeaders();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[");

            for (int i = 0; i < Items.Count; i++)
            {
                var row = GetExportRow(Items[i]);
                sb.AppendLine("  {");

                int colCount = Math.Min(headers.Length, row.Length);
                for (int c = 0; c < colCount; c++)
                {
                    string comma = c < colCount - 1 ? "," : "";
                    sb.AppendLine($"    {JsonEscape(headers[c])}: {JsonEscape(row[c])}{comma}");
                }

                string itemComma = i < Items.Count - 1 ? "," : "";
                sb.AppendLine("  }" + itemComma);
            }

            sb.AppendLine("]");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
