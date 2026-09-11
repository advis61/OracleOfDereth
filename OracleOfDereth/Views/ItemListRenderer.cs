using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    // View-agnostic filtering + row rendering for an ItemList, shared by the Items tab
    // (MainView.Items) and the standalone TradeView. Keeps both windows on one code path.

    // The current filter state for a list: which categories are visible plus the
    // free-text search box. Both views populate this from their checkboxes each refresh.
    public class ItemFilter
    {
        public string Text = "";
        public bool MineOnly = false;
        // Resolve against the character using the screen, including after loading a saved search.
        [System.Xml.Serialization.XmlIgnore]
        public string CurrentCharacter { get; set; }
        public bool Weapons = false;
        public bool WeaponHW = false;
        public bool WeaponFW = false;
        public bool WeaponLW = false;
        public bool Weapon2H = false;
        public bool WeaponWar = false;
        public bool WeaponVoid = false;
        public bool WeaponOther = false;
        public bool WeaponTW = false;
        public bool WeaponBow = false;
        public bool WeaponXbow = false;
        public bool ElementSlash = false;
        public bool ElementPierce = false;
        public bool ElementBludge = false;
        public bool ElementFire = false;
        public bool ElementFrost = false;
        public bool ElementStorm = false;
        public bool ElementAcid = false;
        public bool ElementNether = false;
        public bool Armor = false;
        public bool ArmorSetAdept = false;
        public bool ArmorSetDefender = false;
        public bool ArmorSetDexterous = false;
        public bool ArmorSetHearty = false;
        public bool ArmorSetWise = false;
        public bool ArmorSetNoSet = false;
        public bool ArmorSetOther = false;
        public ItemInfo.ArmorSlot ArmorSlots = ItemInfo.ArmorSlot.None;
        public bool Clothing = false;
        public bool ClothingShirt = false;
        public bool ClothingPants = false;
        public bool ClothingFullCoverage = false;
        public bool ClothingPartialCoverage = false;
        public bool Jewelry = false;
        public bool JewelryNecklace = false;
        public bool JewelryTrinket = false;
        public bool JewelryBracelet = false;
        public bool JewelryRing = false;
        public bool JewelryImbued = false;
        public bool JewelryNotImbued = false;
        public bool Cloaks = false;
        public bool CloakLevel1 = false;
        public bool CloakLevel2 = false;
        public bool CloakLevel3 = false;
        public bool CloakLevel4 = false;
        public bool CloakLevel5 = false;
        public bool CloakLevelOther = false;
        public bool CloakProcOther = false;
        public bool CloakProcDamage200 = false;
        public bool CloakProcCiS = false;
        public bool CloakProcMelee = false;
        public bool CloakProcMissile = false;
        public bool CloakProcMagic = false;
        public bool CloakProcAoE = false;
        public bool Summons = false;
        public bool SummonNaturalist = false;
        public bool SummonNecromancer = false;
        public bool SummonPrimalist = false;
        public bool SummonOther = false;
        public bool Aetheria = false;
        public bool AetheriaLevel1 = false;
        public bool AetheriaLevel2 = false;
        public bool AetheriaLevel3 = false;
        public bool AetheriaLevel4 = false;
        public bool AetheriaLevel5 = false;
        public bool AetheriaColorBlue = false;
        public bool AetheriaColorYellow = false;
        public bool AetheriaColorRed = false;
        public bool AetheriaSigilDefense = false;
        public bool AetheriaSigilDestruction = false;
        public bool AetheriaSigilFury = false;
        public bool AetheriaSigilGrowth = false;
        public bool AetheriaSigilVigor = false;
        public bool AetheriaSurgeAffliction = false;
        public bool AetheriaSurgeDestruction = false;
        public bool AetheriaSurgeFestering = false;
        public bool AetheriaSurgeProtection = false;
        public bool AetheriaSurgeRegeneration = false;
        public bool Salvage = false;
        public bool SalvageIron = false;
        public bool SalvageGranite = false;
        public bool SalvageMahogany = false;
        public bool SalvageGreenGarnet = false;
        public bool SalvageVelvet = false;
        public bool SalvageBrass = false;
        public bool SalvageSteel = false;
        public bool SalvageRends = false;
        public bool SalvageImbues = false;
        public bool SalvageOther = false;
        public bool Other = false;
        public bool OtherClassAlchemy = false;
        public bool OtherClassComponent = false;
        public bool OtherClassCooking = false;
        public bool OtherClassFood = false;
        public bool OtherClassGem = false;
        public bool OtherClassHealingKit = false;
        public bool OtherClassKey = false;
        public bool OtherClassLockpick = false;
        public bool OtherClassManaStone = false;
        public bool OtherClassMisc = false;
        public bool OtherClassRare = false;
        public bool OtherClassOther = false;

        // Not a category — an extra AND condition: items carrying two or more legendary spells.
        public bool Doubles = false;

        // True when the filter actually narrows the list (some category ticked, text typed, or Doubles set).
        public bool IsActive => AnyCategorySelected() || !string.IsNullOrWhiteSpace(Text) || Doubles || MineOnly;

        public bool Matches(ItemListRow t)
        {
            if (MineOnly && (string.IsNullOrEmpty(CurrentCharacter)
                || !string.Equals(t.Character, CurrentCharacter, StringComparison.OrdinalIgnoreCase))) return false;
            if (!IsCategoryVisible(t.SortCategory)) return false;
            if (Armor && t.SortCategory == ItemCategory.Armor && ArmorSlots != ItemInfo.ArmorSlot.None &&
                (new ItemInfo(t.Item).GetArmorSlots() & ArmorSlots) == 0) return false;
            if (!MatchesCloakLevel(t)) return false;
            if (!MatchesCloakProc(t)) return false;
            if (!MatchesJewelry(t)) return false;
            if (!MatchesAetheria(t)) return false;
            if (!MatchesSalvage(t)) return false;
            if (!MatchesOtherClass(t)) return false;
            if (!MatchesSummon(t)) return false;
            if (!MatchesClothing(t)) return false;
            if (!MatchesArmorSet(t)) return false;
            if (!MatchesWeaponType(t)) return false;
            if (!MatchesWeaponElement(t)) return false;
            if (!MatchesDoubles(t)) return false;
            return MatchesText(t);
        }

        private bool MatchesCloakProc(ItemListRow row)
        {
            if (!Cloaks || row.SortCategory != ItemCategory.Cloaks ||
                !(CloakProcDamage200 || CloakProcCiS || CloakProcMelee || CloakProcMissile ||
                  CloakProcMagic || CloakProcAoE || CloakProcOther)) return true;
            switch (row.SummaryCol2)
            {
                case "-200 Damage": return CloakProcDamage200;
                case "CiS": return CloakProcCiS;
                case "Melee Shroud": return CloakProcMelee;
                case "Missile Shroud": return CloakProcMissile;
                case "Magic Shroud": return CloakProcMagic;
                case "Blade Ring":
                case "Bludgeon Ring":
                case "Piercing Ring":
                case "Acid Ring":
                case "Fire Ring":
                case "Frost Ring":
                case "Lightning Ring":
                case "Void Ring":
                case "Melee Ring":
                case "Magic Ring": return CloakProcAoE;
                default: return CloakProcOther;
            }
        }

        private bool MatchesCloakLevel(ItemListRow row)
        {
            if (!Cloaks || row.SortCategory != ItemCategory.Cloaks ||
                !(CloakLevel1 || CloakLevel2 || CloakLevel3 || CloakLevel4 || CloakLevel5 || CloakLevelOther)) return true;
            switch (new ItemInfo(row.Item).GetCloakLevel())
            {
                case 1: return CloakLevel1;
                case 2: return CloakLevel2;
                case 3: return CloakLevel3;
                case 4: return CloakLevel4;
                case 5: return CloakLevel5;
                default: return CloakLevelOther;
            }
        }

        private bool MatchesJewelry(ItemListRow row)
        {
            if (!Jewelry || row.SortCategory != ItemCategory.Jewelry) return true;
            var info = new ItemInfo(row.Item);
            if (JewelryImbued != JewelryNotImbued)
            {
                // Unidentified items cannot be classified as unimbued just because
                // their appraisal properties have not arrived yet.
                bool imbued = info.IsImbued();
                if (!imbued && !row.Item.HasIdData) return false;
                if (imbued != JewelryImbued) return false;
            }
            if (!(JewelryNecklace || JewelryTrinket || JewelryBracelet || JewelryRing)) return true;
            // ItemInfo groups both wrist slots as Bracelet and both finger slots as Ring.
            switch (info.GetSlotName())
            {
                case "Necklace": return JewelryNecklace;
                case "Trinket": return JewelryTrinket;
                case "Bracelet": return JewelryBracelet;
                case "Ring": return JewelryRing;
                default: return false;
            }
        }

        private bool MatchesAetheria(ItemListRow row)
        {
            if (!Aetheria || row.SortCategory != ItemCategory.Aetheria) return true;
            var info = new ItemInfo(row.Item);
            int level = info.GetAetheriaLevel();
            if ((AetheriaLevel1 || AetheriaLevel2 || AetheriaLevel3 || AetheriaLevel4 || AetheriaLevel5) &&
                !((AetheriaLevel1 && level == 1) || (AetheriaLevel2 && level == 2) || (AetheriaLevel3 && level == 3) ||
                  (AetheriaLevel4 && level == 4) || (AetheriaLevel5 && level == 5))) return false;
            string color = info.GetAetheriaColor();
            string sigil = info.GetSetName();
            // Keep the surge separate: Destruction can appear in either group.
            string surge = row.AetheriaSurge;
            if ((AetheriaColorBlue || AetheriaColorYellow || AetheriaColorRed) &&
                !((AetheriaColorBlue && color == "Blue") || (AetheriaColorYellow && color == "Yellow") || (AetheriaColorRed && color == "Red"))) return false;
            if ((AetheriaSigilDefense || AetheriaSigilDestruction || AetheriaSigilFury || AetheriaSigilGrowth || AetheriaSigilVigor) &&
                !((AetheriaSigilDefense && sigil == "Defense") || (AetheriaSigilDestruction && sigil == "Destruction") || (AetheriaSigilFury && sigil == "Fury") || (AetheriaSigilGrowth && sigil == "Growth") || (AetheriaSigilVigor && sigil == "Vigor"))) return false;
            if ((AetheriaSurgeAffliction || AetheriaSurgeDestruction || AetheriaSurgeFestering || AetheriaSurgeProtection || AetheriaSurgeRegeneration) &&
                !((AetheriaSurgeAffliction && surge == "Affliction") || (AetheriaSurgeDestruction && surge == "Destruction") || (AetheriaSurgeFestering && surge == "Festering") || (AetheriaSurgeProtection && surge == "Protection") || (AetheriaSurgeRegeneration && surge == "Regeneration"))) return false;
            return true;
        }

        private bool MatchesSalvage(ItemListRow row)
        {
            if (!Salvage || row.SortCategory != ItemCategory.Salvage ||
                !(SalvageIron || SalvageGranite || SalvageMahogany || SalvageGreenGarnet || SalvageVelvet ||
                  SalvageBrass || SalvageSteel || SalvageRends || SalvageImbues || SalvageOther)) return true;
            switch (new ItemInfo(row.Item).GetMaterial())
            {
                case "Iron": return SalvageIron;
                case "Granite": return SalvageGranite;
                case "Mahogany": return SalvageMahogany;
                case "Green Garnet": return SalvageGreenGarnet;
                case "Velvet": return SalvageVelvet;
                case "Brass": return SalvageBrass;
                case "Steel": return SalvageSteel;
                case "Red Garnet":
                case "Jet":
                case "Imperial Topaz":
                case "Emerald":
                case "Black Garnet":
                case "Aquamarine":
                case "White Sapphire":
                case "Sunstone":
                case "Onyx": return SalvageRends;
                case "Yellow Topaz":
                case "Zircon":
                case "Peridot":
                case "Hematite":
                case "Fire Opal":
                case "Black Opal":
                case "Diamond":
                case "Ruby":
                case "Gromnie Hide":
                case "Pyreal": return SalvageImbues;
                default: return SalvageOther;
            }
        }

        private bool MatchesOtherClass(ItemListRow row)
        {
            if (!Other || row.SortCategory != ItemCategory.Other ||
                !(OtherClassAlchemy || OtherClassComponent || OtherClassCooking || OtherClassFood ||
                  OtherClassGem || OtherClassHealingKit || OtherClassKey || OtherClassLockpick ||
                  OtherClassManaStone || OtherClassMisc || OtherClassRare || OtherClassOther)) return true;
            // Rare is an appraisal marker, not a Decal object class; keep it distinct
            // from its underlying class, just like the existing Type column.
            if (new ItemInfo(row.Item).IsRare) return OtherClassRare;
            switch (row.Item.ObjectClass)
            {
                case ObjectClass.BaseAlchemy:
                case ObjectClass.CraftedAlchemy: return OtherClassAlchemy;
                case ObjectClass.SpellComponent: return OtherClassComponent;
                case ObjectClass.BaseCooking:
                case ObjectClass.CraftedCooking: return OtherClassCooking;
                case ObjectClass.Food: return OtherClassFood;
                case ObjectClass.Gem: return OtherClassGem;
                case ObjectClass.HealingKit: return OtherClassHealingKit;
                case ObjectClass.Key: return OtherClassKey;
                case ObjectClass.Lockpick: return OtherClassLockpick;
                case ObjectClass.ManaStone: return OtherClassManaStone;
                case ObjectClass.Misc: return OtherClassMisc;
                default: return OtherClassOther;
            }
        }

        private bool MatchesSummon(ItemListRow row)
        {
            if (!Summons || row.SortCategory != ItemCategory.Summons ||
                !(SummonNaturalist || SummonNecromancer || SummonPrimalist || SummonOther)) return true;
            switch (new ItemInfo(row.Item).GetSummonSpecString())
            {
                case "Naturalist": return SummonNaturalist;
                case "Necromancer": return SummonNecromancer;
                case "Primalist": return SummonPrimalist;
                default: return SummonOther;
            }
        }

        private bool MatchesClothing(ItemListRow row)
        {
            if (!Clothing || row.SortCategory != ItemCategory.Clothing) return true;
            var info = new ItemInfo(row.Item);
            string garment = info.GetSlotName();
            bool shirt = garment == "Shirt", pants = garment == "Pants";
            if ((ClothingShirt || ClothingPants) && !((ClothingShirt && shirt) || (ClothingPants && pants)))
                return false;
            if (!ClothingFullCoverage && !ClothingPartialCoverage) return true;
            if (!shirt && !pants) return false;
            var required = shirt
                ? ItemInfo.ArmorSlot.Chest | ItemInfo.ArmorSlot.UpperArms | ItemInfo.ArmorSlot.LowerArms
                : ItemInfo.ArmorSlot.Abdomen | ItemInfo.ArmorSlot.UpperLegs | ItemInfo.ArmorSlot.LowerLegs;
            bool full = (info.GetArmorSlots() & required) == required;
            return full ? ClothingFullCoverage : ClothingPartialCoverage;
        }

        private bool MatchesArmorSet(ItemListRow row)
        {
            if (!Armor || row.SortCategory != ItemCategory.Armor ||
                !(ArmorSetAdept || ArmorSetDefender || ArmorSetDexterous || ArmorSetHearty ||
                  ArmorSetWise || ArmorSetNoSet || ArmorSetOther)) return true;
            // Same Equipment Set property used by ItemInfo; unknown nonzero sets are Other.
            // Missing appraisal data cannot establish that an item has no set.
            if (!row.Item.TryGetValue((Decal.Adapter.Wrappers.LongValueKey)265, out int set) && !row.Item.HasIdData)
                return false;
            switch (set)
            {
                case 0: return ArmorSetNoSet;
                case 14: return ArmorSetAdept;
                case 16: return ArmorSetDefender;
                case 20: return ArmorSetDexterous;
                case 19: return ArmorSetHearty;
                case 21: return ArmorSetWise;
                default: return ArmorSetOther;
            }
        }

        // Subfilters narrow only the weapon category; selected armor/etc. still match.
        // Hidden selections are inactive when the parent Weapons checkbox is off.
        private bool MatchesWeaponType(ItemListRow row)
        {
            if (!Weapons || row.SortCategory != ItemCategory.Weapons ||
                !(WeaponHW || WeaponFW || WeaponLW || Weapon2H || WeaponWar || WeaponVoid || WeaponTW || WeaponBow || WeaponXbow || WeaponOther)) return true;
            switch (new ItemInfo(row.Item).GetWeaponTypeName())
            {
                case "Heavy": return WeaponHW;
                case "Finesse": return WeaponFW;
                case "Light": return WeaponLW;
                case "Two Hand": return Weapon2H;
                case "War": return WeaponWar;
                case "Nether": return WeaponVoid;
                case "Thrown": return WeaponTW;
                case "Bow": return WeaponBow;
                case "Crossbow": return WeaponXbow;
                default: return WeaponOther;
            }
        }

        // Search the Type summary (SummaryCol1), using the names displayed by the list.
        // Element choices are alternatives, combined with the weapon-type group by AND.
        private bool MatchesWeaponElement(ItemListRow row)
        {
            if (!Weapons || row.SortCategory != ItemCategory.Weapons ||
                !(ElementSlash || ElementPierce || ElementBludge || ElementFire ||
                  ElementFrost || ElementStorm || ElementAcid || ElementNether)) return true;
            string type = row.SummaryCol1;
            return (ElementSlash && HasElement(type, "Slash")) ||
                (ElementPierce && HasElement(type, "Pierce")) ||
                (ElementBludge && HasElement(type, "Bludge")) ||
                (ElementFire && HasElement(type, "Fire", "Flame")) ||
                (ElementFrost && HasElement(type, "Frost", "Cold")) ||
                (ElementStorm && HasElement(type, "Storm", "Lightning")) ||
                (ElementAcid && HasElement(type, "Acid")) ||
                (ElementNether && HasElement(type, "Void", "Nether"));
        }

        private static bool HasElement(string text, string name, string alias = null) =>
            text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (alias != null && text.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0);

        // Category-only match, ignoring the search text. Used for second-tier identify priority:
        // appraise everything in the selected categories once the exact (text + category) matches
        // are done, so clearing the search term finds the broader set already identified.
        public bool MatchesCategory(ItemListRow t) => IsCategoryVisible(t.SortCategory);

        // Category checkboxes act as a whitelist: with none ticked there's no category
        // filtering at all (everything shows); tick one or more to show only those.
        private bool AnyCategorySelected()
        {
            return Weapons || Armor || Clothing || Jewelry || Cloaks || Summons || Aetheria || Salvage || Other;
        }

        private bool IsCategoryVisible(ItemCategory sortCategory)
        {
            if (!AnyCategorySelected()) return true;

            switch (sortCategory)
            {
                case ItemCategory.Weapons: return Weapons;
                case ItemCategory.Armor: return Armor;
                case ItemCategory.Jewelry: return Jewelry;
                case ItemCategory.Cloaks: return Cloaks;
                case ItemCategory.Summons: return Summons;
                case ItemCategory.Aetheria: return Aetheria;
                case ItemCategory.Salvage: return Salvage;
                case ItemCategory.Clothing: return Clothing;
                default: return Other;
            }
        }

        // The item's searchable text: name plus every summary column, as one string.
        private static string Combined(ItemListRow t) => $"{t.DisplayName} {t.SummaryCol1} {t.SummaryCol2} {t.SummaryCol3} {t.SummaryCol4}";

        // "Doubles": items doubled up on their highest cantrip tier — two or more legendary, OR
        // two or more epic with no legendary, OR two or more major with no epic/legendary. Counts
        // the tier words in the row text (case-insensitive); a single higher-tier cantrip outranks
        // (disqualifies) a lower-tier double.
        private bool MatchesDoubles(ItemListRow t)
        {
            if (!Doubles) return true;

            if (t.SortCategory == ItemCategory.Weapons)
            {
                int weaponLegendary = 0, weaponEpic = 0;
                foreach (string entry in t.SummaryCol4.Split(','))
                {
                    string spell = entry.Trim();
                    bool legendaryTier = spell.StartsWith("Legendary ", StringComparison.OrdinalIgnoreCase);
                    bool epicTier = spell.StartsWith("Epic ", StringComparison.OrdinalIgnoreCase);
                    if (!legendaryTier && !epicTier) continue;
                    string name = spell.Substring(legendaryTier ? 10 : 5);
                    if (!name.Equals("Blood Thirst", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("Defender", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("Heart Seeker", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("Spirit Drinker", StringComparison.OrdinalIgnoreCase)) continue;
                    if (legendaryTier) weaponLegendary++;
                    else weaponEpic++;
                }
                return weaponLegendary > 0 ? weaponLegendary >= 2 : weaponEpic >= 2;
            }

            string combined = Combined(t);

            int legendary = CountOccurrences(combined, "legendary");
            if (legendary >= 2) return true;
            if (legendary > 0) return false;   // a single legendary outranks any epic/major double

            int epic = CountOccurrences(combined, "epic");
            if (epic >= 2) return true;
            if (epic > 0) return false;        // a single epic outranks any major double

            return CountOccurrences(combined, "major") >= 2;
        }

        private string parsedSearchText;
        private string textSearchError;
        public string SearchError
        {
            get
            {
                string current = Text ?? "";
                if (parsedSearchText != current) ParseTextSearch(current);
                return textSearchError;
            }
        }

        private readonly List<string> phrases = new List<string>();
        private readonly List<(string Word, int Count)> terms = new List<(string, int)>();
        private Regex pattern;


        private void ParseTextSearch(string text)
        {
            parsedSearchText = text;
            phrases.Clear();
            terms.Clear();
            pattern = null;
            textSearchError = null;
            var unquoted = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                // Keep escaped characters intact for the regex parser.
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    unquoted.Append(text[i]).Append(text[++i]);
                    continue;
                }
                if (text[i] != '"') { unquoted.Append(text[i]); continue; }
                int end = text.IndexOf('"', i + 1);
                if (end < 0) { textSearchError = "Close the quoted search phrase."; return; }
                string phrase = text.Substring(i + 1, end - i - 1);
                if (phrase.Length > 0) phrases.Add(phrase);
                unquoted.Append('\0'); // Keep explicit quotes as boundaries for automatic phrases.
                i = end;
            }

            string query = unquoted.ToString().Trim();
            // Preserve ordinary punctuation and the existing word*2 shorthand.
            bool regex = query.IndexOfAny(new[] { '\\', '|', '(', ')', '[', ']', '{', '}', '^', '$' }) >= 0 ||
                query.Contains(".*") || query.Contains(".+") || query.Contains(".?");
            if (regex)
            {
                try
                {
                    pattern = new Regex(query.Replace('\0', ' ').Trim(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(25));
                }
                catch (ArgumentException) { textSearchError = "Invalid search regex."; }
                return;
            }
            foreach (string segment in query.Split('\0'))
            {
                string[] words = segment.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    string term = words[i];
                    if (i + 1 < words.Length && (term.Equals("legendary", StringComparison.OrdinalIgnoreCase) ||
                        term.Equals("epic", StringComparison.OrdinalIgnoreCase) ||
                        term.Equals("major", StringComparison.OrdinalIgnoreCase) ||
                        term.Equals("minor", StringComparison.OrdinalIgnoreCase)))
                    {
                        phrases.Add(term + " " + words[++i]);
                        continue;
                    }
                    int star = term.LastIndexOf('*');
                    if (star > 0 && int.TryParse(term.Substring(star + 1), out int count) && count > 0)
                        terms.Add((term.Substring(0, star), count));
                    else terms.Add((term, 1));
                }
            }
        }

        private bool MatchesText(ItemListRow row)
        {
            if (SearchError != null) return false;
            if (phrases.Count == 0 && terms.Count == 0 && pattern == null) return true;
            var columns = new[] { row.DisplayName, row.SummaryCol1, row.SummaryCol4,
                row.SummaryCol2, row.SummaryCol3, row.Character };
            foreach (string phrase in phrases)
                if (!columns.Any(column => column != null && column.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;

            if (terms.Count == 0 && pattern == null) return true;
            string combined = string.Join(" ", columns);
            if (pattern != null)
            {
                try { return pattern.IsMatch(combined); }
                catch (RegexMatchTimeoutException)
                {
                    // Disable this query after one timeout, rather than stalling on every row.
                    textSearchError = "Search regex took too long; simplify the expression.";
                    return false;
                }
            }
            foreach (var term in terms)
            {
                int start = 0;
                for (int count = 0; count < term.Count; count++)
                {
                    int index = combined.IndexOf(term.Word, start, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) return false;
                    start = index + term.Word.Length;
                }
            }
            return true;
        }

        private static int CountOccurrences(string text, string term)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += term.Length;
            }
            return count;
        }
    }

    public static class ItemListRenderer
    {
        private static void SetText(HudList.HudListRowAccessor row, int column, string value)
        {
            var cell = (HudStaticText)row[column];
            if (cell.Text != value) cell.Text = value;
        }

        // Dim grey for rows still waiting on their appraisal details.
        private static readonly Color ColorLoading = Color.FromArgb(255, 150, 150, 150);

        // Paint the given items into the HudList: status/loading icon, item icon, name and
        // the four summary columns, with the id stashed in the (hidden) last column. Takes
        // the "not complete" icon for column 0 and the id of the selected row.
        public static void Render(HudList list, List<ItemListRow> items, int iconNotComplete, int selectedId, bool showCharacter = false, ItemListRow selectedItem = null)
        {
            for (int x = 0; x < items.Count; x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= list.RowCount) { row = list.AddRow(); } else { row = list[x]; }

                ItemListRow item = items[x];

                if (showCharacter) SetText(row, 0, item.Character);
                else AssignImage((HudPictureBox)row[0], iconNotComplete);
                AssignImage((HudPictureBox)row[1], item.Icon);
                SetText(row, 2, item.DisplayName);
                SetText(row, 3, item.SummaryCol1);
                SetText(row, 4, item.SummaryCol2);
                SetText(row, 5, item.SummaryCol3);
                SetText(row, 6, item.SummaryCol4);
                SetText(row, 7, item.Id.ToString());

                // Center the Effect and Info columns; the rest keep their default left alignment.
                ((HudStaticText)row[4]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                ((HudStaticText)row[5]).TextAlignment = VirindiViewService.WriteTextFormats.Center;

                bool selected = showCharacter ? ReferenceEquals(item, selectedItem) : item.Id == selectedId && selectedId != 0;
                SetRowColor(row, selected, loading: !item.IsComplete, showCharacter: showCharacter);
            }

            // Trim surplus rows. Nothing to clean up alongside them: AssignImage keeps its
            // state on the box, so a destroyed row takes it with it.
            while (list.RowCount > items.Count)
            {
                list.RemoveRow(list.RowCount - 1);
            }
        }

        // Tint the row's text columns (Name..Details): highlighted when selected, dim grey
        // while still loading its appraisal, otherwise the default colour.
        public static void SetRowColor(HudList.HudListRowAccessor row, bool selected, bool loading, bool showCharacter = false)
        {
            for (int col = 2; col <= 6; col++)
            {
                HudStaticText cell = (HudStaticText)row[col];
                if (selected) cell.TextColor = MainView.ColorSelected;
                else if (loading) cell.TextColor = ColorLoading;
                else cell.ResetTextColor();
            }
            if (showCharacter)
            {
                HudStaticText character = (HudStaticText)row[0];
                if (selected) character.TextColor = MainView.ColorSelected;
                else character.ResetTextColor();
            }
        }

        // Only swap the image when it actually changes; assigning is comparatively expensive.
        // The box is its own record of what it's showing — an int converts implicitly to
        // ACImage and PortalImageID reads that id back — so there's nothing to cache, and no
        // dead keys to clean up when a row is destroyed. Mirrors MainView.AssignImage.
        private static void AssignImage(HudPictureBox box, int icon)
        {
            // A cleared box reads back as null rather than 0, which is the same "no image".
            int current = box.Image == null ? 0 : box.Image.PortalImageID;
            if (current == icon) return;

            if (icon == 0)
            {
                box.Image = null;
            }
            else
            {
                box.Image = icon;
            }
        }

        // One consistent format regardless of filters: total, plus optional
        // "(X shown)" when a filter hides some and "(N identifying)" while ids load.
        public static string StatusText(string label, int total, int shownCount, int identifying)
        {
            var notes = new List<string>();
            if (shownCount != total) notes.Add($"{shownCount} shown");
            if (identifying > 0) notes.Add($"{identifying} identifying");

            string status = $"{label}: {total}";
            if (notes.Count > 0) status += " (" + string.Join(", ", notes) + ")";
            return status;
        }
    }
}
