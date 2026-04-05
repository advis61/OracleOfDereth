using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    public class TradeItem
    {
        // Collection of Trade Items
        public static List<TradeItem> TradeItems = new List<TradeItem>();
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

        // Items pending identification before being added
        private static HashSet<int> PendingIds = new HashSet<int>();

        // Queue of item ids waiting to be identified (for Add All)
        private static List<int> IdentifyQueue = new List<int>();
        public static bool IsProcessingQueue = false;

        public static bool AutoAddEnabled = false;

        // Callback to refresh the UI after an item is added from the queue
        public static Action OnTradeListChanged;
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
            TradeItems.Clear();
            PendingIds.Clear();
            IdentifyQueue.Clear();
            IsProcessingQueue = false;
        }

        public static int QueueCount => IdentifyQueue.Count + PendingIds.Count;

        public static bool IsTradeable(WorldObject wo)
        {
            if (wo == null) return false;
            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (wo.ObjectClass == ObjectClass.MissileWeapon && wo.Values(LongValueKey.StackMax, 0) > 0) return false;
            if (wo.Values(LongValueKey.Attuned, 0) > 0) return false;
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
        /// Request to add an item by id. If already identified, adds immediately and returns true.
        /// Otherwise requests identification and returns false (will be added via Identified()).
        /// </summary>
        public static bool RequestAdd(int id)
        {
            if (id == 0) return false;

            WorldObject wo = CoreManager.Current.WorldFilter[id];
            if (wo == null) return false;

            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (!IsInInventory(wo)) return false;
            if (TradeItems.Any(t => t.Id == id)) return false;

            if (wo.HasIdData)
            {
                if (!IsTradeable(wo)) return false;
                AddFromWorldObject(wo);
                return true;
            }

            PendingIds.Add(id);
            CoreManager.Current.Actions.RequestId(id);
            return false;
        }

        /// <summary>
        /// Called from WorldObjectIdentifier_Identified when an item finishes identification.
        /// Returns true if this item was pending for the trade list.
        /// </summary>
        public static bool Identified(WorldObject item)
        {
            if (!PendingIds.Contains(item.Id)) return false;

            PendingIds.Remove(item.Id);

            if (!IsTradeable(item)) return true;

            AddFromWorldObject(item);
            return true;
        }

        /// <summary>
        /// Scans the entire inventory. Adds identified tradeable items immediately,
        /// queues unidentified items for identification one at a time.
        /// </summary>
        public static void AddAll()
        {
            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    //if (!IsAddAllClass(wo.ObjectClass)) continue;
                    //if (wo.ObjectClass == ObjectClass.MissileWeapon && wo.Values(LongValueKey.StackMax, 0) > 0) continue;
                    if (!IsInInventory(wo)) continue;
                    if (TradeItems.Any(t => t.Id == wo.Id)) continue;

                    // Salvage doesn't need identification
                    if (wo.ObjectClass == ObjectClass.Salvage)
                    {
                        AddFromWorldObject(wo);
                        continue;
                    }

                    //// Skip Misc items that aren't useful
                    //if (wo.ObjectClass == ObjectClass.Misc)
                    //{
                    //    ItemInfo check = new ItemInfo(wo);
                    //    if (!check.IsSummon && !check.IsAetheria && !check.IsFoolproof) continue;
                    //}

                    if (wo.HasIdData)
                    {
                        if (!IsTradeable(wo)) continue;
                        AddFromWorldObject(wo);
                    }
                    else
                    {
                        if (!PendingIds.Contains(wo.Id) && !IdentifyQueue.Contains(wo.Id))
                        {
                            IdentifyQueue.Add(wo.Id);
                        }
                    }
                }
            }

            if (IdentifyQueue.Count > 0 && !IsProcessingQueue)
            {
                IsProcessingQueue = true;
                CoreManager.Current.WorldFilter.ChangeObject += TradeItem_ChangeObject;
                ProcessNextInQueue();
            }
        }

        private const int MaxConcurrentRequests = 3;

        private static void ProcessNextInQueue()
        {
            while (IdentifyQueue.Count > 0 && PendingIds.Count < MaxConcurrentRequests)
            {
                int id = IdentifyQueue[0];
                IdentifyQueue.RemoveAt(0);

                WorldObject wo = CoreManager.Current.WorldFilter[id];
                if (wo == null) continue;
                if (TradeItems.Any(t => t.Id == id)) continue;

                // Skip non-tradeable Misc items (not summons or aetheria)
                if (wo.ObjectClass == ObjectClass.Misc)
                {
                    ItemInfo check = new ItemInfo(wo);
                    if (!check.IsSummon && !check.IsAetheria && !check.IsFoolproof) continue;
                }

                // Already identified by another plugin — process immediately
                if (wo.HasIdData)
                {
                    if (IsTradeable(wo)) AddFromWorldObject(wo);
                    OnTradeListChanged?.Invoke();
                    continue;
                }

                PendingIds.Add(id);
                CoreManager.Current.Actions.RequestId(id);
            }

            // Queue is empty and no pending requests, stop listening
            if (IdentifyQueue.Count == 0 && PendingIds.Count == 0)
            {
                StopProcessingQueue();
            }
        }

        private static void TradeItem_ChangeObject(object sender, ChangeObjectEventArgs e)
        {
            try
            {
                if (e.Change != WorldChangeType.IdentReceived) return;

                bool wasPending = PendingIds.Remove(e.Changed.Id);
                bool wasQueued = IdentifyQueue.Remove(e.Changed.Id);

                if (!wasPending && !wasQueued) return;

                if (IsTradeable(e.Changed))
                {
                    AddFromWorldObject(e.Changed);
                }

                OnTradeListChanged?.Invoke();

                if (wasPending) ProcessNextInQueue();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void CancelQueue()
        {
            IdentifyQueue.Clear();
            PendingIds.Clear();
            StopProcessingQueue();
        }

        private static void StopProcessingQueue()
        {
            if (!IsProcessingQueue) return;
            IsProcessingQueue = false;
            CoreManager.Current.WorldFilter.ChangeObject -= TradeItem_ChangeObject;
            OnQueueFinished?.Invoke();
        }

        public static string StatusText()
        {
            int pending = IdentifyQueue.Count + PendingIds.Count;
            if (pending > 0)
                return $"Trade Items: {TradeItems.Count} done, {pending} pending";
            return $"Trade Items: {TradeItems.Count} selected";
        }

        private static void AddFromWorldObject(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Add(new TradeItem
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
            if (info.IsCloak) return "";
            if (info.IsArmorClothing || info.IsJewelry) return info.GetCantripsString();
            return "";
        }

        private static int GetSortCategory(ItemInfo info)
        {
            if (info.IsWeapon) return 0;
            if (info.IsArmorClothing) return 1;
            if (info.IsJewelry) return 2;
            if (info.IsCloak) return 3;
            if (info.IsSummon) return 4;
            if (info.IsAetheria) return 5;
            if (info.IsFoolproof) return 6;
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
                    TradeItems = TradeItems.OrderBy(t => t.Name).ToList();
                    break;
                case SortType.NameDescending:
                    TradeItems = TradeItems.OrderByDescending(t => t.Name).ToList();
                    break;
                case SortType.Col1Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenBy(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col1Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenByDescending(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol2).ThenBy(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol2).ThenByDescending(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol3).ThenBy(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol3).ThenByDescending(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol4).ThenBy(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol4).ThenByDescending(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
            }
        }

        public static void Add(TradeItem item)
        {
            if (TradeItems.Any(t => t.Id == item.Id)) return;
            TradeItems.Add(item);
            Sort(CurrentSortType);
        }

        public static void Remove(int id)
        {
            TradeItems.RemoveAll(t => t.Id == id);
        }

        public static void Clear()
        {
            TradeItems.Clear();
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

            var lines = TradeItems.Select(t => t.Description).ToList();
            File.WriteAllLines(path, lines);

            return path;
        }

        private static string[] GetExportHeaders()
        {
            return new[] {
                "Character", "Server", "Name", "ObjectClass", "Type", "Set", "Armor Level", "Imbues", "Tinks",
                "OD", "OA", "OM", "Damage", "Dmg Low", "Dmg High", "Elem Bonus", "Missile %", "Caster %",
                "Attack", "Melee D", "Magic D", "Missile D", "Mana C",
                "Spells", "Wield Req", "Wield Req Level", "Lore", "Craft", "Value", "Burden",
                "Summon DMG", "Summon DEF",
                "D", "DR", "C", "CR", "CD", "CDR", "HB", "V"
            };
        }

        private static string[] GetExportRow(TradeItem item)
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
                info.GetLoreValue() > 0 ? info.GetLoreValue().ToString() : "",
                info.GetWorkmanshipString(),
                info.GetValue() > 0 ? info.GetValue().ToString() : "",
                info.GetBurden() > 0 ? info.GetBurden().ToString() : "",
                info.GetSummonDamageString(),
                info.GetSummonDefenseString(),
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

            foreach (var item in TradeItems)
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

            for (int i = 0; i < TradeItems.Count; i++)
            {
                var row = GetExportRow(TradeItems[i]);
                sb.AppendLine("  {");

                int colCount = Math.Min(headers.Length, row.Length);
                for (int c = 0; c < colCount; c++)
                {
                    string comma = c < colCount - 1 ? "," : "";
                    sb.AppendLine($"    {JsonEscape(headers[c])}: {JsonEscape(row[c])}{comma}");
                }

                string itemComma = i < TradeItems.Count - 1 ? "," : "";
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
