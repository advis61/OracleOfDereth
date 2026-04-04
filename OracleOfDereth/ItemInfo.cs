using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;

namespace OracleOfDereth
{
    public class ItemInfo
    {
        // ============================================================
        // Constructor & Internal State
        // ============================================================

        public readonly WorldObject wo;

        private readonly List<int> activeSpells = new List<int>();
        private readonly List<int> innateSpells = new List<int>();
        private readonly Dictionary<int, int> intValues = new Dictionary<int, int>();
        private readonly Dictionary<int, double> doubleValues = new Dictionary<int, double>();

        public ItemInfo(WorldObject worldObject)
        {
            wo = worldObject;

            foreach (var key in wo.LongKeys)
                intValues[key] = wo.Values((LongValueKey)key);

            foreach (var key in wo.DoubleKeys)
                doubleValues[key] = wo.Values((DoubleValueKey)key);

            // Some quest weapons don't expose keys via LongKeys/DoubleKeys but the values are accessible via wo.Values() directly.
            EnsureKey(intValues, Key_MaxDamage, wo.Values(LongValueKey.MaxDamage, 0));
            EnsureKey(intValues, Key_ElementalDmgBonus, wo.Values(LongValueKey.ElementalDmgBonus, 0));
            EnsureKey(intValues, 353, wo.Values((LongValueKey)353, 0)); // Mastery
            EnsureKey(doubleValues, Key_DamageBonus, wo.Values(DoubleValueKey.DamageBonus, 0));
            EnsureKey(doubleValues, Key_AttackBonus, wo.Values(DoubleValueKey.AttackBonus, 0));
            EnsureKey(doubleValues, Key_MeleeDefenseBonus, wo.Values(DoubleValueKey.MeleeDefenseBonus, 0));
            EnsureKey(doubleValues, Key_ElementalDmgVsMonsters, wo.Values(DoubleValueKey.ElementalDamageVersusMonsters, 0));

            for (int i = 0; i < wo.ActiveSpellCount; i++)
                activeSpells.Add(wo.ActiveSpell(i));

            for (int i = 0; i < wo.SpellCount; i++)
                innateSpells.Add(wo.Spell(i));
        }

        private static void EnsureKey<T>(Dictionary<int, T> dict, int key, T value) where T : struct, IComparable
        {
            if (!dict.ContainsKey(key) && value.CompareTo(default(T)) != 0) dict[key] = value;
        }

        // ============================================================
        // Type Detection
        // ============================================================

        public bool IsWeapon => wo.ObjectClass == ObjectClass.MeleeWeapon || wo.ObjectClass == ObjectClass.MissileWeapon || wo.ObjectClass == ObjectClass.WandStaffOrb;
        public bool IsArmorClothing => (wo.ObjectClass == ObjectClass.Armor || wo.ObjectClass == ObjectClass.Clothing) && !IsCloak;
        public bool IsJewelry => wo.ObjectClass == ObjectClass.Jewelry;
        public bool IsCloak => wo.ObjectClass == ObjectClass.Clothing && wo.Values(LongValueKey.EquipableSlots, 0) == 0x8000000;
        public bool IsSummon => wo.ObjectClass == ObjectClass.Misc && wo.Values(LongValueKey.UsesTotal) == 50 && (wo.Name.EndsWith("Essence") || wo.Name.Contains("Essence ("));
        public bool IsAetheria => wo.Name == "Aetheria";
        public bool IsFoolproof => wo.Name.EndsWith(" Foolproof");
        public bool IsAmmo => (wo.ObjectClass == ObjectClass.MissileWeapon) && (wo.Values(LongValueKey.StackMax, 0) > 0);

        // ============================================================
        // Identity: Name, Material, Slot, ObjectClass
        // ============================================================

        public string GetObjectClassName() => wo.ObjectClass.ToString();

        public string GetMaterial()
        {
            if (wo.Values(LongValueKey.Material) <= 0) return "";
            if (MaterialInfo.TryGetValue(wo.Values(LongValueKey.Material), out string mat)) return mat;
            return "Unknown Material " + wo.Values(LongValueKey.Material);
        }

        public string GetName()
        {
            string material = GetMaterial();
            if (material.Length > 0) return material + " " + wo.Name;
            return wo.Name;
        }

        public string GetItemSlotName()
        {
            if (IsWeapon) return GetWeaponTypeName();
            if (IsCloak) return "Cloak";
            if (IsSummon) return "Summon";
            if (IsAetheria) return GetAetheriaColor();
            if (IsFoolproof) return "Foolproof";
            if (IsArmorClothing || IsJewelry) return GetSlotName();
            if (wo.ObjectClass == ObjectClass.SpellComponent) return "Component";
            if (wo.ObjectClass == ObjectClass.CraftedFletching) return "Fletching";
            if (wo.ObjectClass == ObjectClass.CraftedAlchemy) return "Alchemy";
            if (wo.ObjectClass == ObjectClass.CraftedCooking) return "Cooking";
            if (wo.ObjectClass == ObjectClass.BaseCooking) return "Cooking";
            return wo.ObjectClass.ToString();
        }

        public string GetWeaponTypeName()
        {
            int skill = GetWeaponSkill();
            switch (skill)
            {
                case 44: return "Heavy";
                case 45: return "Light";
                case 46: return "Finesse";
                case 41: return "Two Hand";
                case 47: return "Missile";
                case 34: return "War";
                case 43: return "Void";
                default:
                    if (wo.ObjectClass == ObjectClass.WandStaffOrb) return "Caster";
                    return "";
            }
        }

        public string GetSlotName()
        {
            int slots = wo.Values(LongValueKey.EquipableSlots, 0);
            int coverage = wo.Values(LongValueKey.Coverage, 0);

            // Underclothing (no armor level)
            if (wo.ObjectClass == ObjectClass.Clothing && wo.Values(LongValueKey.ArmorLevel, 0) == 0)
            {
                if ((slots & 0x02) != 0) return "Shirt";
                if ((slots & 0x40) != 0) return "Pants";
            }

            // Jewelry — try EquipableSlots, then fall back to name
            if (wo.ObjectClass == ObjectClass.Jewelry)
            {
                // EquipMask values from AC EquipMask enum
                if ((slots & 0xC0000) != 0) return "Ring";         // FingerWearLeft | FingerWearRight
                if ((slots & 0x30000) != 0) return "Bracelet";     // WristWearLeft | WristWearRight
                if ((slots & 0x8000) != 0) return "Necklace";      // NeckWear
                if ((slots & 0x4000000) != 0) return "Trinket";    // TrinketOne

                // Fallback to name
                string name = wo.Name.ToLower();
                if (name.Contains("ring") || name.Contains("band") || name.Contains("signet")) return "Ring";
                if (name.Contains("bracelet")) return "Bracelet";
                if (name.Contains("necklace") || name.Contains("gorget") || name.Contains("amulet") || name.Contains("pendant") || name.Contains("choker") || name.Contains("locket")) return "Necklace";
                if (name.Contains("trinket") || name.Contains("compass") || name.Contains("pocket") || name.Contains("goggles") || name.Contains("puzzle box") || name.Contains(" top") || name.Contains("scarab")) return "Trinket";
                return "Jewelry";
            }

            // Shield
            if ((slots & 0x200000) != 0) return "Shield";        // Shield

            // Armor slots (EquipMask values)
            if ((slots & 0x200) != 0) return "Chest";            // ChestArmor
            if ((slots & 0x400) != 0) return "Abdomen";          // AbdomenArmor
            if ((slots & 0x800) != 0) return "Upper Arms";       // UpperArmArmor
            if ((slots & 0x1000) != 0) return "Lower Arms";      // LowerArmArmor
            if ((slots & 0x2000) != 0) return "Upper Legs";      // UpperLegArmor
            if ((slots & 0x4000) != 0) return "Lower Legs";      // LowerLegArmor

            // Underwear / clothing wear slots
            if ((slots & 0x01) != 0) return "Head";              // HeadWear
            if ((slots & 0x02) != 0) return "Chest";             // ChestWear
            if ((slots & 0x04) != 0) return "Abdomen";           // AbdomenWear
            if ((slots & 0x08) != 0) return "Upper Arms";        // UpperArmWear
            if ((slots & 0x10) != 0) return "Lower Arms";        // LowerArmWear
            if ((slots & 0x20) != 0) return "Hands";             // HandWear
            if ((slots & 0x40) != 0) return "Legs";              // UpperLegWear
            if ((slots & 0x80) != 0) return "Lower Legs";        // LowerLegWear
            if ((slots & 0x100) != 0) return "Feet";             // FootWear

            // Fallback to Coverage (CoverageMask values)
            if ((coverage & 0x08) != 0 || (coverage & 0x400) != 0) return "Chest";       // UnderwearChest | OuterwearChest
            if ((coverage & 0x10) != 0 || (coverage & 0x800) != 0) return "Abdomen";      // UnderwearAbdomen | OuterwearAbdomen
            if ((coverage & 0x20) != 0 || (coverage & 0x1000) != 0) return "Upper Arms";  // UnderwearUpperArms | OuterwearUpperArms
            if ((coverage & 0x40) != 0 || (coverage & 0x2000) != 0) return "Lower Arms";  // UnderwearLowerArms | OuterwearLowerArms
            if ((coverage & 0x02) != 0 || (coverage & 0x100) != 0) return "Upper Legs";   // UnderwearUpperLegs | OuterwearUpperLegs
            if ((coverage & 0x04) != 0 || (coverage & 0x200) != 0) return "Lower Legs";   // UnderwearLowerLegs | OuterwearLowerLegs
            if ((coverage & 0x4000) != 0) return "Head";                                   // Head
            if ((coverage & 0x8000) != 0) return "Hands";                                  // Hands
            if ((coverage & 0x10000) != 0) return "Feet";                                  // Feet

            return "";
        }

        public string GetMasteryString()
        {
            if (wo.Values((LongValueKey)353) <= 0) return "";
            if (MasteryInfo.TryGetValue(wo.Values((LongValueKey)353), out string mastery)) return mastery;
            return "Unknown Mastery " + wo.Values((LongValueKey)353);
        }

        // ============================================================
        // Equipment Set
        // ============================================================

        /// <summary>Returns the full set name as stored (e.g. "Adept's Set").</summary>
        public string GetFullSetName()
        {
            int set = wo.Values((LongValueKey)265, 0);
            if (set == 0) return "";
            if (AttributeSetInfo.TryGetValue(set, out string setName)) return setName;
            return "Unknown Set";
        }

        /// <summary>Returns a shortened set name for display (e.g. "Adept").</summary>
        public string GetSetName()
        {
            int set = wo.Values((LongValueKey)265, 0);
            if (set != 0 && AttributeSetInfo.TryGetValue(set, out string setName))
                return setName.Replace("Sigil of ", "").Replace("'s", "").Replace(" Proof Set", "").Replace(" Set", "").Trim();
            return "";
        }

        // ============================================================
        // Armor
        // ============================================================

        public int GetArmorLevel() => wo.Values(LongValueKey.ArmorLevel, 0);

        public string GetArmorLevelString()
        {
            if (GetArmorLevel() <= 0) return "";
            return "AL " + GetArmorLevel();
        }

        public string GetProtectionsString()
        {
            if (wo.ObjectClass != ObjectClass.Armor || wo.Values(LongValueKey.Unenchantable, 0) == 0) return "";

            return "[" +
                wo.Values(DoubleValueKey.SlashProt).ToString("N1") + "/" +
                wo.Values(DoubleValueKey.PierceProt).ToString("N1") + "/" +
                wo.Values(DoubleValueKey.BludgeonProt).ToString("N1") + "/" +
                wo.Values(DoubleValueKey.ColdProt).ToString("N1") + "/" +
                wo.Values(DoubleValueKey.FireProt).ToString("N1") + "/" +
                wo.Values(DoubleValueKey.AcidProt).ToString("N1") + "/" +
                wo.Values(DoubleValueKey.LightningProt).ToString("N1") + "]";
        }

        // ============================================================
        // Ratings
        // ============================================================

        public int RatingDamage => wo.Values((LongValueKey)370);
        public int RatingDamageResist => wo.Values((LongValueKey)371);
        public int RatingCrit => wo.Values((LongValueKey)372);
        public int RatingCritResist => wo.Values((LongValueKey)373);
        public int RatingCritDamage => wo.Values((LongValueKey)374);
        public int RatingCritDamageResist => wo.Values((LongValueKey)375);
        public int RatingHealBoost => wo.Values((LongValueKey)376);
        public int RatingVitality => wo.Values((LongValueKey)379);

        public string GetRatingsString()
        {
            if (RatingDamage + RatingDamageResist + RatingCrit + RatingCritResist +
                RatingCritDamage + RatingCritDamageResist + RatingHealBoost + RatingVitality <= 0)
                return "";

            var parts = new List<string>();
            if (RatingDamage > 0) parts.Add("D" + RatingDamage);
            if (RatingDamageResist > 0) parts.Add("DR" + RatingDamageResist);
            if (RatingCrit > 0) parts.Add("C" + RatingCrit);
            if (RatingCritDamage > 0) parts.Add("CD" + RatingCritDamage);
            if (RatingCritResist > 0) parts.Add("CR" + RatingCritResist);
            if (RatingCritDamageResist > 0) parts.Add("CDR" + RatingCritDamageResist);
            if (RatingHealBoost > 0) parts.Add("HB" + RatingHealBoost);
            if (RatingVitality > 0) parts.Add("V" + RatingVitality);
            return string.Join(" ", parts);
        }

        // ============================================================
        // OD / OA / OM Values & Strings
        // ============================================================

        public int? GetODValue()
        {
            if (IsEquipped && !AssumeFullBuffs) return null;

            int? result = null;
            if (wo.ObjectClass == ObjectClass.MeleeWeapon) result = GetMeleeOD();
            else if (wo.ObjectClass == ObjectClass.MissileWeapon) result = GetMissileOD();
            else if (wo.ObjectClass == ObjectClass.WandStaffOrb) result = GetCasterOD();

            if (result != null && (result < -15 || result > 30)) return null;
            return result;
        }

        public int? GetOAValue()
        {
            if (IsEquipped && !AssumeFullBuffs) return null;
            if (wo.ObjectClass != ObjectClass.MeleeWeapon) return null;

            int? result = GetMeleeOA();
            if (result != null && (result < -15 || result > 30)) return null;
            return result;
        }

        public int? GetOMValue()
        {
            if (IsEquipped && !AssumeFullBuffs) return null;
            if (!IsWeapon) return null;

            double buffedDefBonus = GetBuffedDoubleValue(Key_MeleeDefenseBonus);
            if (buffedDefBonus <= 0) return null;

            int totalDef = (int)Math.Round((buffedDefBonus - 1) * 100);
            if (AssumeFullBuffs) totalDef -= 20;

            int maxDef = GetMaxMeleeDefense();
            if (maxDef <= 0) return null;

            int result = totalDef - maxDef;
            if (result < -15 || result > 30) return null;
            return result;
        }

        public string GetODString()
        {
            int? od = GetODValue();
            if (od == null) return null;
            return "OD " + FormatOValue((int)od);
        }

        public string GetOAString()
        {
            int? oa = GetOAValue();
            if (oa == null) return null;
            return "OA " + FormatOValue((int)oa);
        }

        public string GetOMString()
        {
            int? om = GetOMValue();
            if (om == null) return null;
            return "MD " + FormatOValue((int)om);
        }

        public static string FormatOValue(int val)
        {
            return val >= 0 ? "+" + val : "" + val;
        }

        // ============================================================
        // Damage
        // ============================================================

        public double GetWeaponDamageLow()
        {
            if (wo.Values(LongValueKey.MaxDamage) == 0) return 0;
            if (wo.Values(DoubleValueKey.Variance) == 0) return wo.Values(LongValueKey.MaxDamage);
            return wo.Values(LongValueKey.MaxDamage) - (wo.Values(LongValueKey.MaxDamage) * wo.Values(DoubleValueKey.Variance));
        }

        public int GetWeaponDamageHigh() => wo.Values(LongValueKey.MaxDamage, 0);
        public int GetElementalDamageBonus() => wo.Values(LongValueKey.ElementalDmgBonus, 0);
        public double GetDamageBonusPct() => Math.Round(((wo.Values(DoubleValueKey.DamageBonus, 1) - 1) * 100));
        public double GetElementalDamageVsMonsters() => Math.Round(((wo.Values(DoubleValueKey.ElementalDamageVersusMonsters, 1) - 1) * 100));

        public string GetDamageString()
        {
            var parts = new List<string>();

            int high = GetWeaponDamageHigh();
            if (high != 0 && wo.Values(DoubleValueKey.Variance) != 0)
                parts.Add(GetWeaponDamageLow().ToString("N2") + "-" + high);
            else if (high != 0)
                parts.Add(high.ToString());

            if (GetElementalDamageBonus() != 0)
                parts.Add("+" + GetElementalDamageBonus());
            if (GetDamageBonusPct() != 0)
                parts.Add("+" + GetDamageBonusPct() + "%");
            if (GetElementalDamageVsMonsters() != 0)
                parts.Add("+" + GetElementalDamageVsMonsters() + "%vs. Monsters");

            return string.Join(", ", parts);
        }

        // ============================================================
        // Bonuses (Attack, Defense, Mana Conversion)
        // ============================================================

        public double GetAttackBonus() => Math.Round(((wo.Values(DoubleValueKey.AttackBonus, 1) - 1) * 100));
        public double GetMeleeDefenseBonus() => Math.Round(((wo.Values(DoubleValueKey.MeleeDefenseBonus, 1) - 1) * 100));
        public double GetMagicDefenseBonus() => Math.Round(((wo.Values(DoubleValueKey.MagicDBonus, 1) - 1) * 100), 1);
        public double GetMissileDefenseBonus() => Math.Round(((wo.Values(DoubleValueKey.MissileDBonus, 1) - 1) * 100), 1);
        public double GetManaConversionBonus() => Math.Round((wo.Values(DoubleValueKey.ManaCBonus) * 100));

        public string GetBonusesString()
        {
            var parts = new List<string>();
            if (GetAttackBonus() != 0) parts.Add("+" + GetAttackBonus() + "%a");
            if (GetMeleeDefenseBonus() != 0) parts.Add(GetMeleeDefenseBonus() + "%md");
            if (GetMagicDefenseBonus() != 0) parts.Add(GetMagicDefenseBonus() + "%mgc.d");
            if (GetMissileDefenseBonus() != 0) parts.Add(GetMissileDefenseBonus() + "%msl.d");
            if (GetManaConversionBonus() != 0) parts.Add(GetManaConversionBonus() + "%mc");
            return string.Join(", ", parts);
        }

        // ============================================================
        // Imbues & Tinks
        // ============================================================

        public string GetImbueString()
        {
            int imbued = wo.Values(LongValueKey.Imbued);
            if (imbued <= 0) return "";

            var parts = new List<string>();
            if ((imbued & 1) == 1) parts.Add("CS");
            if ((imbued & 2) == 2) parts.Add("CB");
            if ((imbued & 4) == 4) parts.Add("AR");
            if ((imbued & 8) == 8) parts.Add("SlashRend");
            if ((imbued & 16) == 16) parts.Add("PierceRend");
            if ((imbued & 32) == 32) parts.Add("BludgeRend");
            if ((imbued & 64) == 64) parts.Add("AcidRend");
            if ((imbued & 128) == 128) parts.Add("FrostRend");
            if ((imbued & 256) == 256) parts.Add("LightRend");
            if ((imbued & 512) == 512) parts.Add("FireRend");
            if ((imbued & 1024) == 1024) parts.Add("MeleeImbue");
            if ((imbued & 4096) == 4096) parts.Add("MagicImbue");
            if ((imbued & 8192) == 8192) parts.Add("Hematited");
            if ((imbued & 536870912) == 536870912) parts.Add("MagicAbsorb");
            return string.Join(" ", parts);
        }

        public int GetTinksValue() => wo.Values(LongValueKey.NumberTimesTinkered, 0);

        public string GetTinksString()
        {
            if (GetTinksValue() <= 0) return "";
            return "Tinks " + GetTinksValue();
        }

        // ============================================================
        // Spells & Cantrips
        // ============================================================

        public string GetSpellsString()
        {
            if (innateSpells.Count == 0) return "";

            FileService service = CoreManager.Current.Filter<FileService>();
            List<int> sorted = new List<int>(innateSpells);
            sorted.Sort();
            sorted.Reverse();

            bool isLootGenerated = wo.LongKeys.Contains((int)LongValueKey.Material);
            bool isUnenchantable = wo.Values(LongValueKey.Unenchantable, 0) != 0;

            var parts = new List<string>();
            foreach (int spellId in sorted)
            {
                Decal.Filters.Spell spell = service.SpellTable.GetById(spellId);
                if (spell == null) continue;
                string name = spell.Name;

                if (isLootGenerated)
                {
                    bool isBaneImpen = name.Contains(" Bane") || name.Contains("Impen") || name.StartsWith("Brogard");

                    if (isBaneImpen && !isUnenchantable) continue;

                    bool isImpenCantrip = name.Contains("Minor Impenetrability") || name.Contains("Major Impenetrability")
                                       || name.Contains("Epic Impenetrability") || name.Contains("Legendary Impenetrability");

                    if (!isImpenCantrip && !isBaneImpen && !name.Contains("Augmented"))
                    {
                        if (name.EndsWith(" I") || name.EndsWith(" II") || name.EndsWith(" III")
                            || name.EndsWith(" IV") || name.EndsWith(" V") || name.EndsWith(" VI"))
                            continue;
                    }
                }

                parts.Add(name);
            }

            return string.Join(", ", parts);
        }

        public string GetCantripsString()
        {
            if (innateSpells.Count == 0) return "";

            FileService service = CoreManager.Current.Filter<FileService>();
            var parts = new List<string>();
            foreach (int spellId in innateSpells)
            {
                Decal.Filters.Spell spell = service.SpellTable.GetById(spellId);
                if (spell == null) continue;
                string name = spell.Name;
                if (name.EndsWith(" Bane")) continue;
                if (name.StartsWith("Legendary ") || name.StartsWith("Epic ") || name.StartsWith("Major ") || name.StartsWith("Minor "))
                    parts.Add(name);
            }
            parts.Sort((a, b) => CantripSortOrder(a).CompareTo(CantripSortOrder(b)));
            return string.Join(", ", parts);
        }

        private static int CantripSortOrder(string name)
        {
            if (name.StartsWith("Legendary ")) return 0;
            if (name.StartsWith("Epic ")) return 1;
            if (name.StartsWith("Major ")) return 2;
            if (name.StartsWith("Minor ")) return 3;
            return 4;
        }

        // ============================================================
        // Requirements (Wield, Lore, Summon, Workmanship)
        // ============================================================

        public string GetWieldReqName()
        {
            if (wo.Values(LongValueKey.WieldReqValue) <= 0) return "";
            if (wo.Values(LongValueKey.WieldReqType) == 7 && wo.Values(LongValueKey.WieldReqAttribute) == 1) return "Wield Lvl";
            if (SkillInfo.TryGetValue(wo.Values(LongValueKey.WieldReqAttribute), out string skillName)) return skillName;
            return "Unknown Skill " + wo.Values(LongValueKey.WieldReqAttribute);
        }

        public int GetWieldReqLevel() => wo.Values(LongValueKey.WieldReqValue, 0);

        public string GetWieldReqString()
        {
            string name = GetWieldReqName();
            if (name.Length == 0) return "";
            return name + " " + GetWieldReqLevel();
        }

        public int GetLoreValue() => wo.Values(LongValueKey.LoreRequirement, 0);

        public string GetLoreString()
        {
            if (GetLoreValue() <= 0) return "";
            return "Diff " + GetLoreValue();
        }

        public string GetWorkmanshipString()
        {
            if (wo.ObjectClass == ObjectClass.Salvage)
            {
                if (wo.Values(DoubleValueKey.SalvageWorkmanship) > 0)
                    return "Work " + wo.Values(DoubleValueKey.SalvageWorkmanship).ToString("N2");
            }
            else
            {
                if (wo.Values(LongValueKey.Workmanship) > 0 && GetTinksValue() != 10)
                    return "Craft " + wo.Values(LongValueKey.Workmanship);
            }
            return "";
        }

        public string GetSummonReqsString()
        {
            var parts = new List<string>();

            if (wo.Values((LongValueKey)369) > 0)
                parts.Add("Lvl " + wo.Values((LongValueKey)369));

            if (wo.Values(LongValueKey.SkillLevelReq) > 0 && (wo.Values(LongValueKey.WieldReqAttribute) != wo.Values(LongValueKey.ActivationReqSkillId) || wo.Values(LongValueKey.WieldReqValue) < wo.Values(LongValueKey.SkillLevelReq)))
            {
                if (SkillInfo.TryGetValue(wo.Values(LongValueKey.ActivationReqSkillId), out string skillName))
                    parts.Add(skillName + " " + wo.Values(LongValueKey.SkillLevelReq) + " to Activate");
                else
                    parts.Add("Unknown Skill " + wo.Values(LongValueKey.ActivationReqSkillId) + " " + wo.Values(LongValueKey.SkillLevelReq) + " to Activate");
            }

            if (wo.Values((LongValueKey)366) > 0 && wo.Values((LongValueKey)367) > 0)
            {
                if (SkillInfo.TryGetValue(wo.Values((LongValueKey)366), out string skillName))
                    parts.Add(skillName + " " + wo.Values((LongValueKey)367));
                else
                    parts.Add("Unknown Skill " + wo.Values((LongValueKey)366) + " " + wo.Values((LongValueKey)367));
            }

            if (wo.Values((LongValueKey)368) > 0 && wo.Values((LongValueKey)367) > 0)
            {
                if (SkillInfo.TryGetValue(wo.Values((LongValueKey)368), out string skillName))
                    parts.Add("Spec " + skillName + " " + wo.Values((LongValueKey)367));
                else
                    parts.Add("Unknown Skill Spec " + wo.Values((LongValueKey)368) + " " + wo.Values((LongValueKey)367));
            }

            return string.Join(", ", parts);
        }

        // ============================================================
        // Summon Scores
        // ============================================================

        public string GetSummonDamageString()
        {
            if (!IsSummon) return "";
            return $"{new Summon { Item = wo }.DamageScore()}%";
        }

        public string GetSummonDefenseString()
        {
            if (!IsSummon) return "";
            return $"{new Summon { Item = wo }.DefenseScore()}%";
        }

        // ============================================================
        // Cloak
        // ============================================================

        public string GetCloakWeave()
        {
            FileService service = CoreManager.Current.Filter<FileService>();
            foreach (int spellId in innateSpells)
            {
                Decal.Filters.Spell spell = service.SpellTable.GetById(spellId);
                if (spell == null) continue;
                if (spell.Name.Contains("Weave of"))
                    return spell.Name.Replace("Weave of ", "");
            }
            return "";
        }

        public string GetCloakProc()
        {
            FileService service = CoreManager.Current.Filter<FileService>();

            if (wo.SpellCount > 0)
            {
                Decal.Filters.Spell spell = service.SpellTable.GetById(wo.Spell(0));
                if (spell == null) return "";
                string name = spell.Name;

                if (name.Contains("Cloaked in Skill")) return "CiS";
                if (name.Contains("Shroud of Darkness (Melee)")) return "Melee Shroud";
                if (name.Contains("Shroud of Darkness (Missile)")) return "Missile Shroud";
                if (name.Contains("Shroud of Darkness (Magic)")) return "Magic Shroud";
                if (name.Contains("Shroud of Darkness")) return "Shroud";
                if (name.Contains("Horizon's Blades")) return "Blade Ring";
                if (name.Contains("Tectonic Rifts")) return "Bludgeon Ring";
                if (name.Contains("Nuhmudira's Spines")) return "Piercing Ring";
                if (name.Contains("Searing Disc")) return "Acid Ring";
                if (name.Contains("Cassius' Ring of Fire")) return "Fire Ring";
                if (name.Contains("Halo of Frost")) return "Frost Ring";
                if (name.Contains("Eye of the Storm")) return "Lightning Ring";
                if (name.Contains("Clouded Soul")) return "Void Ring";
                if (name.Contains("Major Melee")) return "Melee Ring";
                if (name.Contains("Major Magic")) return "Magic Ring";

                return name;
            }

            return "-200 Damage";
        }

        // ============================================================
        // Aetheria
        // ============================================================

        public string GetAetheriaColor()
        {
            if (wo.Name.Contains("Blue")) return "Blue";
            if (wo.Name.Contains("Yellow")) return "Yellow";
            if (wo.Name.Contains("Red")) return "Red";
            return "Aetheria";
        }

        public string GetAetheriaSurge()
        {
            FileService service = CoreManager.Current.Filter<FileService>();
            for (int i = 0; i < wo.SpellCount; i++)
            {
                Decal.Filters.Spell spell = service.SpellTable.GetById(wo.Spell(i));
                if (spell == null) continue;
                string name = spell.Name;
                if (name.Contains("Destruction")) return "Destruction";
                if (name.Contains("Protection")) return "Protection";
                if (name.Contains("Regeneration")) return "Regeneration";
                if (name.Contains("Mana")) return "Mana";
            }
            return "";
        }

        // ============================================================
        // Value & Burden
        // ============================================================

        public int GetValue() => wo.Values(LongValueKey.Value, 0);
        public int GetBurden() => wo.Values(LongValueKey.Burden, 0);

        public string GetValueBurdenString()
        {
            var parts = new List<string>();
            if (GetValue() > 0) parts.Add("Value " + String.Format("{0:n0}", GetValue()));
            if (GetBurden() > 0) parts.Add("BU " + GetBurden());
            return string.Join(", ", parts);
        }

        // ============================================================
        // Misc
        // ============================================================

        public string GetKeyringString()
        {
            if (wo.ObjectClass != ObjectClass.Misc || !wo.Name.Contains("Keyring")) return "";
            return "Keys: " + wo.Values(LongValueKey.KeysHeld) + ", Uses: " + wo.Values(LongValueKey.UsesRemaining);
        }

        // ============================================================
        // Weapon Identification (static entry point)
        // ============================================================

        public static bool WeaponIdentified(WorldObject item)
        {
            ItemInfo info = new ItemInfo(item);
            if (!info.IsWeapon) return false;

            string odString = info.ToODString();
            if (odString == null) return false;

            Util.Chat(info.GetName() + " " + odString, Util.ColorCyan, "");
            return true;
        }

        // ============================================================
        // ToString — Full item description
        // ============================================================

        public override string ToString()
        {
            var parts = new List<string>();

            // Name with mastery and OD/summon inline
            string header = GetName();

            string mastery = GetMasteryString();
            if (mastery.Length > 0) header += " (" + mastery + ")";

            string od = ToODString();
            if (od.Length > 0) header += " " + od;

            if (IsSummon) header += " [DMG " + GetSummonDamageString() + " | DEF " + GetSummonDefenseString() + "]";

            parts.Add(header);

            // Remaining sections — each only added if non-empty
            AddPart(parts, GetFullSetName());
            AddPart(parts, GetArmorLevelString());
            AddPart(parts, GetImbueString());
            AddPart(parts, GetTinksString());
            AddPart(parts, GetDamageString());
            AddPart(parts, GetBonusesString());
            AddPart(parts, GetBuffedValuesString());
            AddPart(parts, GetSpellsString());
            AddPart(parts, GetWieldReqString());
            AddPart(parts, GetSummonReqsString());
            AddPart(parts, GetLoreString());
            AddPart(parts, GetWorkmanshipString());
            AddPart(parts, GetProtectionsString());
            AddPart(parts, GetValueBurdenString());

            string ratings = GetRatingsString();
            if (ratings.Length > 0) parts.Add("[" + ratings + "]");

            AddPart(parts, GetKeyringString());

            return string.Join(", ", parts);
        }

        private static void AddPart(List<string> parts, string value)
        {
            if (value.Length > 0) parts.Add(value);
        }

        private string ToODString()
        {
            string od = GetODString();
            string oa = GetOAString();
            string om = GetOMString();

            if (AssumeFullBuffs && od == null && om == null) return "";
            if (od == null && oa == null && om == null) return "";

            var parts = new List<string>();
            if (od != null) parts.Add(od);
            if (oa != null) parts.Add(oa);
            if (om != null) parts.Add(om);

            return "[" + string.Join(" | ", parts) + "]";
        }

        // ============================================================
        // Buffed Value Calculations (private)
        // ============================================================

        #region Buffed Value Calculations

        private const int Key_MaxDamage = 218103842;
        private const int Key_ArmorLevel = 28;
        private const int Key_ElementalDmgBonus = 204;
        private const int Key_Imbued = 179;
        private const int Key_Tinks = 171;
        private const int Key_Material = 131;
        private const int Key_DamageBonus = 167772174;
        private const int Key_ElementalDmgVsMonsters = 152;
        private const int Key_AttackBonus = 167772172;
        private const int Key_MeleeDefenseBonus = 29;
        private const int Key_ManaCBonus = 144;

        private bool IsEquipped => intValues.ContainsKey(10) && intValues[10] > 0;

        private int GetBuffedIntValue(int key, int defaultValue = 0)
        {
            if (!intValues.ContainsKey(key)) return defaultValue;

            int value = intValues[key];
            foreach (int spell in activeSpells)
                if (IntSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key)
                    value -= effect.Change;
            foreach (int spell in innateSpells)
                if (IntSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key && effect.Bonus != 0)
                    value += effect.Bonus;
            return value;
        }

        private double GetBuffedDoubleValue(int key, double defaultValue = 0)
        {
            if (!doubleValues.ContainsKey(key)) return defaultValue;

            double value = doubleValues[key];
            foreach (int spell in activeSpells)
            {
                if (DoubleSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key)
                {
                    if (Math.Abs(effect.Change - 1) < Double.Epsilon)
                        value /= effect.Change;
                    else
                        value -= effect.Change;
                }
            }
            foreach (int spell in innateSpells)
            {
                if (DoubleSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key && Math.Abs(effect.Bonus) > Double.Epsilon)
                {
                    if (Math.Abs(effect.Change - 1) < Double.Epsilon)
                        value *= effect.Bonus;
                    else
                        value += effect.Bonus;
                }
            }
            return value;
        }

        private int GetHolderLevel()
        {
            if (!IsEquipped) return 0;
            try
            {
                WorldObject holder = CoreManager.Current.WorldFilter[wo.Container];
                if (holder != null) return holder.Values((LongValueKey)25, 0);
            }
            catch { }
            return 0;
        }

        private bool AssumeFullBuffs => GetHolderLevel() >= 200;

        public string GetBuffedValuesString()
        {
            if (!IsWeapon) return "";

            var sb = new StringBuilder("(");

            if (wo.ObjectClass == ObjectClass.MeleeWeapon)
                sb.Append(CalcBuffedTinkedDoT().ToString("N1") + "/" + GetBuffedIntValue(Key_MaxDamage));
            else if (wo.ObjectClass == ObjectClass.MissileWeapon)
                sb.Append(CalcBuffedMissileDamage().ToString("N1"));
            else
                sb.Append(((GetBuffedDoubleValue(Key_ElementalDmgVsMonsters) - 1) * 100).ToString("N1"));

            sb.Append(" ");

            if (wo.Values(DoubleValueKey.AttackBonus, 1) != 1)
                sb.Append(Math.Round(((GetBuffedDoubleValue(Key_AttackBonus) - 1) * 100)).ToString("N1") + "/");
            if (wo.Values(DoubleValueKey.MeleeDefenseBonus, 1) != 1)
                sb.Append(Math.Round(((GetBuffedDoubleValue(Key_MeleeDefenseBonus) - 1) * 100)).ToString("N1"));
            if (wo.Values(DoubleValueKey.ManaCBonus) != 0)
                sb.Append("/" + Math.Round(GetBuffedDoubleValue(Key_ManaCBonus) * 100));

            sb.Append(")");
            return sb.ToString();
        }

        private double CalcBuffedTinkedDoT()
        {
            if (!doubleValues.ContainsKey(167772171) || !intValues.ContainsKey(Key_MaxDamage))
                return -1;

            double variance = doubleValues[167772171];
            int maxDamage = GetBuffedIntValue(Key_MaxDamage);
            int tinks = intValues.ContainsKey(Key_Tinks) ? intValues[Key_Tinks] : 0;
            int numberOfTinksLeft = Math.Max(10 - Math.Max(tinks, 0), 0);

            if (!intValues.ContainsKey(Key_Imbued) || intValues[Key_Imbued] == 0)
                numberOfTinksLeft--;
            if (!intValues.ContainsKey(Key_Material) || intValues[Key_Material] == 0)
                numberOfTinksLeft = 0;

            for (int i = 1; i <= numberOfTinksLeft; i++)
            {
                double ironTinkDoT = CalculateDamageOverTime(maxDamage + 24 + 1, variance);
                double graniteTinkDoT = CalculateDamageOverTime(maxDamage + 24, variance * .8);
                if (ironTinkDoT >= graniteTinkDoT)
                    maxDamage++;
                else
                    variance *= .8;
            }

            return CalculateDamageOverTime(maxDamage + 24, variance);
        }

        private double CalcBuffedMissileDamage()
        {
            if (!intValues.ContainsKey(Key_MaxDamage) || !doubleValues.ContainsKey(Key_DamageBonus) || !intValues.ContainsKey(Key_ElementalDmgBonus))
                return -1;
            return GetBuffedIntValue(Key_MaxDamage) + (((GetBuffedDoubleValue(Key_DamageBonus) - 1) * 100) / 3) + GetBuffedIntValue(Key_ElementalDmgBonus);
        }

        private static double CalculateDamageOverTime(int maxDamage, double variance, double critChance = .1, double critMultiplier = 2)
        {
            return maxDamage * ((1 - critChance) * (2 - variance) / 2 + (critChance * critMultiplier));
        }

        #endregion

        // ============================================================
        // OD / OA / OM Calculation Internals (private)
        // ============================================================

        #region OD Calculations

        private const int Key_WeaponSkill = 159;
        private const int Key_CombatUse = 47;

        private int? GetMeleeOD()
        {
            if (!intValues.ContainsKey(Key_MaxDamage)) return null;

            int skill = GetWeaponSkill();
            int mastery = GetMastery();
            int multi = IsMultiStrike() ? 1 : 0;

            double maxDmg = LookupMaxProperty(skill, mastery, multi, e => e.MaxDmg);
            if (maxDmg <= 0) return null;

            int buffedDmg = GetBuffedIntValue(Key_MaxDamage);
            if (AssumeFullBuffs) buffedDmg -= 24;
            return (int)Math.Round(buffedDmg - maxDmg);
        }

        private int? GetMissileOD()
        {
            int mastery = GetMastery();

            int arrowMax;
            if (mastery == 8) arrowMax = 40;
            else if (mastery == 9) arrowMax = 53;
            else if (mastery == 10) arrowMax = 42;
            else return null;

            int skill = GetWeaponSkill();
            double maxDmgMod = LookupMaxProperty(skill, mastery, 0, e => e.MaxDmgMod);
            double maxElemBonus = LookupMaxProperty(skill, mastery, 0, e => e.MaxElemBonus);
            if (maxElemBonus <= 0) maxElemBonus = 22;

            double dmgMod = Math.Round((wo.Values(DoubleValueKey.DamageBonus, 1) - 1) * 100);
            int numTimesTinkered = wo.Values(LongValueKey.NumberTimesTinkered, 0);
            double remainingTinks = 10;

            if (wo.Values(DoubleValueKey.SalvageWorkmanship, -1) >= 0)
            {
                if (numTimesTinkered > 0)
                {
                    remainingTinks -= numTimesTinkered;
                    if (wo.Values(LongValueKey.Imbued, 0) == 0) remainingTinks--;
                }
                else
                {
                    remainingTinks--;
                }
            }
            else
            {
                remainingTinks = 0;
            }

            double maxTinkedMissileMod = (maxDmgMod + 100 + 4 * remainingTinks) / 100;
            if (maxTinkedMissileMod <= 0) return null;

            double buffedDmg = GetBuffedIntValue(Key_MaxDamage);
            if (AssumeFullBuffs) buffedDmg -= 24;
            if (buffedDmg <= 10) buffedDmg += 24;

            int elemBonus = intValues.ContainsKey(Key_ElementalDmgBonus) ? intValues[Key_ElementalDmgBonus] : 0;
            double calcMissileDmg = (1 + (dmgMod + (4 * remainingTinks)) / 100) * (elemBonus + buffedDmg + arrowMax) / maxTinkedMissileMod;

            return (int)Math.Round(calcMissileDmg - (maxElemBonus + 24 + arrowMax));
        }

        private int? GetCasterOD()
        {
            int skill = GetWeaponSkill();
            int mastery = GetMastery();

            double maxVsMon = LookupMaxProperty(skill, mastery, 0, e => e.MaxElemVsMon);
            if (maxVsMon <= 0) maxVsMon = 1.18;

            double buffedPct = (GetBuffedDoubleValue(Key_ElementalDmgVsMonsters) - 1) * 100;
            if (AssumeFullBuffs) buffedPct -= 8;

            return (int)Math.Round(buffedPct - (maxVsMon - 1) * 100);
        }

        private int? GetMeleeOA()
        {
            if (!doubleValues.ContainsKey(Key_AttackBonus)) return null;

            int maxAtk = GetMaxAttack();
            if (maxAtk <= 0) return null;

            double buffedPct = Math.Round((GetBuffedDoubleValue(Key_AttackBonus, 1) - 1) * 100);
            if (AssumeFullBuffs) buffedPct -= 20;

            return (int)Math.Round(buffedPct - maxAtk);
        }

        private int GetWeaponSkill()
        {
            if (intValues.ContainsKey(Key_WeaponSkill) && intValues[Key_WeaponSkill] != 0)
                return intValues[Key_WeaponSkill];

            int wieldAttr = wo.Values(LongValueKey.WieldReqAttribute, 0);
            if (wieldAttr >= 34) return wieldAttr;

            int mastery = intValues.ContainsKey(353) ? intValues[353] : 0;

            if (wo.ObjectClass == ObjectClass.MeleeWeapon)
            {
                if (mastery == 11) return 41;
                if (LookupMaxProperty(45, mastery, 0, e => e.MaxDmg) > 0) return 45;
                if (LookupMaxProperty(44, mastery, 0, e => e.MaxDmg) > 0) return 44;
            }
            if (wo.ObjectClass == ObjectClass.MissileWeapon)
            {
                if (LookupMaxProperty(47, mastery, 0, e => e.MaxDmgMod) > 0) return 47;
            }
            if (wo.ObjectClass == ObjectClass.WandStaffOrb)
            {
                if (LookupMaxProperty(34, mastery, 0, e => e.MaxElemVsMon) > 0) return 34;
                if (LookupMaxProperty(43, mastery, 0, e => e.MaxElemVsMon) > 0) return 43;
            }

            return 0;
        }

        private int GetMastery()
        {
            int mastery = intValues.ContainsKey(353) ? intValues[353] : 0;
            if (mastery != 0) return mastery;

            int skill = intValues.ContainsKey(Key_WeaponSkill) ? intValues[Key_WeaponSkill] : 0;
            if (skill == 0) skill = wo.Values(LongValueKey.WieldReqAttribute, 0);
            if (skill == 41 || skill == 0x29) return 11;
            return 0;
        }

        private bool IsMultiStrike()
        {
            int combatUse = intValues.ContainsKey(Key_CombatUse) ? intValues[Key_CombatUse] : 0;
            int mastery = intValues.ContainsKey(353) ? intValues[353] : 0;
            return combatUse == 160 || combatUse == 166 || combatUse == 486
                || (combatUse == 4 && mastery == 11 && !IsTwoHandedSpear());
        }

        private bool IsTwoHandedSpear()
        {
            string name = wo.Name.ToLower();
            return name.Contains("spear") || name.Contains("pike") || name.Contains("assagai") || name.Contains("yari") || name.Contains("naginata") || name.Contains("trident");
        }

        private int GetMaxMeleeDefense()
        {
            int mastery = GetMastery();

            if (wo.ObjectClass == ObjectClass.MissileWeapon || wo.ObjectClass == ObjectClass.WandStaffOrb)
                return 20;

            int skill = GetWeaponSkill();
            if (skill == 0x29 || mastery == 11)
                return IsTwoHandedSpear() ? 20 : 18;

            if ((skill == 0x2E) && mastery == 4 && wo.Name.IndexOf("Jitte", StringComparison.OrdinalIgnoreCase) >= 0)
                return 25;

            if (MaxMeleeDefenseByMastery.TryGetValue(mastery, out int maxDef))
                return maxDef;
            return 0;
        }

        private int GetMaxAttack()
        {
            int mastery = GetMastery();

            int skill = GetWeaponSkill();
            if (skill == 0x29 || mastery == 11)
                return IsTwoHandedSpear() ? 20 : 22;

            if ((skill == 0x2E) && mastery == 4 && wo.Name.IndexOf("Jitte", StringComparison.OrdinalIgnoreCase) >= 0)
                return 15;

            if (MaxAttackByMastery.TryGetValue(mastery, out int maxAtk))
                return maxAtk;
            return 0;
        }

        #endregion

        // ============================================================
        // Static Data Tables
        // ============================================================

        #region Weapon Max Values Lookup Table

        private struct WeaponMax
        {
            public readonly int Skill, Mastery, Multi;
            public readonly double MaxDmg, MaxDmgMod, MaxElemBonus, MaxElemVsMon;

            public WeaponMax(int skill, int mastery, int multi, double maxDmg = 0, double maxDmgMod = 0, double maxElemBonus = 0, double maxElemVsMon = 0)
            {
                Skill = skill; Mastery = mastery; Multi = multi;
                MaxDmg = maxDmg; MaxDmgMod = maxDmgMod;
                MaxElemBonus = maxElemBonus; MaxElemVsMon = maxElemVsMon;
            }
        }

        private static double LookupMaxProperty(int skill, int mastery, int multi, Func<WeaponMax, double> selector)
        {
            foreach (var e in WeaponMaxTable)
                if (e.Skill == skill && e.Mastery == mastery && e.Multi == multi)
                    return selector(e);
            return 0;
        }

        private static readonly WeaponMax[] WeaponMaxTable =
        {
            // Heavy Weaponry (skill 44)
            new WeaponMax(44, 3, 0, 74),   // Axe
            new WeaponMax(44, 6, 0, 71),   // Dagger
            new WeaponMax(44, 6, 1, 38),   // Multi-Strike Dagger
            new WeaponMax(44, 4, 0, 69),   // Mace
            new WeaponMax(44, 5, 0, 72),   // Spear
            new WeaponMax(44, 2, 0, 71),   // Sword
            new WeaponMax(44, 2, 1, 38),   // Multi-Strike Sword
            new WeaponMax(44, 7, 0, 70),   // Staff
            new WeaponMax(44, 1, 0, 59),   // UA

            // Light Weaponry (skill 45)
            new WeaponMax(45, 3, 0, 61),   // Axe
            new WeaponMax(45, 6, 0, 58),   // Dagger
            new WeaponMax(45, 6, 1, 28),   // Multi-Strike Dagger
            new WeaponMax(45, 4, 0, 57),   // Mace
            new WeaponMax(45, 5, 0, 60),   // Spear
            new WeaponMax(45, 2, 0, 58),   // Sword
            new WeaponMax(45, 2, 1, 28),   // Multi-Strike Sword
            new WeaponMax(45, 7, 0, 57),   // Staff
            new WeaponMax(45, 1, 0, 48),   // UA

            // Finesse Weaponry (skill 46)
            new WeaponMax(46, 3, 0, 61),   // Axe
            new WeaponMax(46, 6, 0, 58),   // Dagger
            new WeaponMax(46, 6, 1, 28),   // Multi-Strike Dagger
            new WeaponMax(46, 4, 0, 57),   // Mace
            new WeaponMax(46, 5, 0, 60),   // Spear
            new WeaponMax(46, 2, 0, 58),   // Sword
            new WeaponMax(46, 2, 1, 28),   // Multi-Strike Sword
            new WeaponMax(46, 7, 0, 57),   // Staff
            new WeaponMax(46, 1, 0, 48),   // UA

            // Two Handed Weaponry (skill 41)
            new WeaponMax(41, 11, 1, 45),  // Cleaver (multi-strike)
            new WeaponMax(41, 11, 0, 48),  // Two-Handed Spear

            // Missile Weaponry (skill 47)
            new WeaponMax(47,  8, 0, maxDmgMod: 140, maxElemBonus: 22),  // Bow
            new WeaponMax(47,  9, 0, maxDmgMod: 165, maxElemBonus: 22),  // Crossbow
            new WeaponMax(47, 10, 0, maxDmgMod: 160, maxElemBonus: 22),  // Thrown

            // Wands
            new WeaponMax(34,  0, 0, maxElemVsMon: 1.18),  // War Magic
            new WeaponMax(43,  0, 0, maxElemVsMon: 1.18),  // Void Magic
            new WeaponMax(43, 12, 0, maxElemVsMon: 1.18),  // Void VR wand
        };

        #endregion

        #region Max Defense/Attack Tables

        private static readonly Dictionary<int, int> MaxMeleeDefenseByMastery = new Dictionary<int, int>
        {
            { 1, 20 }, { 2, 20 }, { 3, 18 }, { 4, 22 }, { 5, 15 }, { 6, 20 }, { 7, 25 },
        };

        private static readonly Dictionary<int, int> MaxAttackByMastery = new Dictionary<int, int>
        {
            { 1, 20 }, { 2, 20 }, { 3, 22 }, { 4, 18 }, { 5, 25 }, { 6, 20 }, { 7, 15 },
        };

        #endregion

        #region Spell Effect Dictionaries

        private struct SpellEffect<T>
        {
            public readonly int Key;
            public readonly T Change;
            public readonly T Bonus;

            public SpellEffect(int key, T change, T bonus = default(T))
            {
                Key = key; Change = change; Bonus = bonus;
            }
        }

        private static readonly Dictionary<int, SpellEffect<int>> IntSpellEffects = new Dictionary<int, SpellEffect<int>>
        {
            { 1616, new SpellEffect<int>(Key_MaxDamage, 20) },          // Blood Drinker VI
            { 2096, new SpellEffect<int>(Key_MaxDamage, 22) },          // Infected Caress
            { 5183, new SpellEffect<int>(Key_MaxDamage, 24) },          // Incantation of Blood Drinker
            { 4395, new SpellEffect<int>(Key_MaxDamage, 24) },          // Incantation of Blood Drinker
            { 2598, new SpellEffect<int>(Key_MaxDamage, 2, 2) },        // Minor Blood Thirst
            { 2586, new SpellEffect<int>(Key_MaxDamage, 4, 4) },        // Major Blood Thirst
            { 4661, new SpellEffect<int>(Key_MaxDamage, 7, 7) },        // Epic Blood Thirst
            { 6089, new SpellEffect<int>(Key_MaxDamage, 10, 10) },      // Legendary Blood Thirst
            { 3688, new SpellEffect<int>(Key_MaxDamage, 300) },         // Prodigal Blood Drinker

            { 1486, new SpellEffect<int>(Key_ArmorLevel, 200) },        // Impenetrability VI
            { 2108, new SpellEffect<int>(Key_ArmorLevel, 220) },        // Brogard's Defiance
            { 4407, new SpellEffect<int>(Key_ArmorLevel, 240) },        // Incantation of Impenetrability
            { 2604, new SpellEffect<int>(Key_ArmorLevel, 20, 20) },     // Minor Impenetrability
            { 2592, new SpellEffect<int>(Key_ArmorLevel, 40, 40) },     // Major Impenetrability
            { 4667, new SpellEffect<int>(Key_ArmorLevel, 60, 60) },     // Epic Impenetrability
            { 6095, new SpellEffect<int>(Key_ArmorLevel, 80, 80) },     // Legendary Impenetrability
        };

        private static readonly Dictionary<int, SpellEffect<double>> DoubleSpellEffects = new Dictionary<int, SpellEffect<double>>
        {
            { 3258, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .06) },         // Spirit Drinker VI
            { 3259, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .07) },         // Infected Spirit Caress
            { 5182, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .08) },         // Incantation of Spirit Drinker
            { 4414, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .08) },         // Incantation of Spirit Drinker
            { 3251, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .01, .01) },    // Minor Spirit Thirst
            { 3250, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .03, .03) },    // Major Spirit Thirst
            { 4670, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .05, .05) },    // Epic Spirit Thirst
            { 6098, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .07, .07) },    // Legendary Spirit Thirst
            { 3735, new SpellEffect<double>(Key_ElementalDmgVsMonsters, .15) },         // Prodigal Spirit Drinker

            { 1592, new SpellEffect<double>(Key_AttackBonus, .15) },            // Heart Seeker VI
            { 2106, new SpellEffect<double>(Key_AttackBonus, .17) },            // Elysa's Sight
            { 4405, new SpellEffect<double>(Key_AttackBonus, .20) },            // Incantation of Heart Seeker
            { 2603, new SpellEffect<double>(Key_AttackBonus, .03, .03) },       // Minor Heart Thirst
            { 2591, new SpellEffect<double>(Key_AttackBonus, .05, .05) },       // Major Heart Thirst
            { 4666, new SpellEffect<double>(Key_AttackBonus, .07, .07) },       // Epic Heart Thirst
            { 6094, new SpellEffect<double>(Key_AttackBonus, .09, .09) },       // Legendary Heart Thirst

            { 1605, new SpellEffect<double>(Key_MeleeDefenseBonus, .15) },      // Defender VI
            { 2101, new SpellEffect<double>(Key_MeleeDefenseBonus, .17) },      // Cragstone's Will
            { 4400, new SpellEffect<double>(Key_MeleeDefenseBonus, .20) },      // Incantation of Defender
            { 2600, new SpellEffect<double>(Key_MeleeDefenseBonus, .03, .03) }, // Minor Defender
            { 3985, new SpellEffect<double>(Key_MeleeDefenseBonus, .04, .04) }, // Mukkir Sense
            { 2588, new SpellEffect<double>(Key_MeleeDefenseBonus, .05, .05) }, // Major Defender
            { 4663, new SpellEffect<double>(Key_MeleeDefenseBonus, .07, .07) }, // Epic Defender
            { 6091, new SpellEffect<double>(Key_MeleeDefenseBonus, .09, .09) }, // Legendary Defender
            { 3699, new SpellEffect<double>(Key_MeleeDefenseBonus, .25) },      // Prodigal Defender

            { 1480, new SpellEffect<double>(Key_ManaCBonus, 1.60) },            // Hermetic Link VI
            { 2117, new SpellEffect<double>(Key_ManaCBonus, 1.70) },            // Mystic's Blessing
            { 4418, new SpellEffect<double>(Key_ManaCBonus, 1.80) },            // Incantation of Hermetic Link
            { 3201, new SpellEffect<double>(Key_ManaCBonus, 1.05, 1.05) },      // Feeble Hermetic Link
            { 3199, new SpellEffect<double>(Key_ManaCBonus, 1.10, 1.10) },      // Minor Hermetic Link
            { 3202, new SpellEffect<double>(Key_ManaCBonus, 1.15, 1.15) },      // Moderate Hermetic Link
            { 3200, new SpellEffect<double>(Key_ManaCBonus, 1.20, 1.20) },      // Major Hermetic Link
            { 6086, new SpellEffect<double>(Key_ManaCBonus, 1.25, 1.25) },      // Epic Hermetic Link
            { 6087, new SpellEffect<double>(Key_ManaCBonus, 1.30, 1.30) },      // Legendary Hermetic Link
        };

        #endregion

        #region Name Dictionaries

        private static readonly Dictionary<int, string> SkillInfo = new Dictionary<int, string>
        {
            { 0x1, "Axe" }, { 0x2, "Bow" }, { 0x3, "Crossbow" }, { 0x4, "Dagger" },
            { 0x5, "Mace" }, { 0x6, "Melee Defense" }, { 0x7, "Missile Defense" },
            { 0x8, "Sling" }, { 0x9, "Spear" }, { 0xA, "Staff" }, { 0xB, "Sword" },
            { 0xC, "Thrown Weapons" }, { 0xD, "Unarmed Combat" }, { 0xE, "Arcane Lore" },
            { 0xF, "Magic Defense" }, { 0x10, "Mana Conversion" }, { 0x12, "Item Tinkering" },
            { 0x13, "Assess Person" }, { 0x14, "Deception" }, { 0x15, "Healing" },
            { 0x16, "Jump" }, { 0x17, "Lockpick" }, { 0x18, "Run" },
            { 0x1B, "Assess Creature" }, { 0x1C, "Weapon Tinkering" },
            { 0x1D, "Armor Tinkering" }, { 0x1E, "Magic Item Tinkering" },
            { 0x1F, "Creature Enchantment" }, { 0x20, "Item Enchantment" },
            { 0x21, "Life Magic" }, { 0x22, "War Magic" }, { 0x23, "Leadership" },
            { 0x24, "Loyalty" }, { 0x25, "Fletching" }, { 0x26, "Alchemy" },
            { 0x27, "Cooking" }, { 0x28, "Salvaging" }, { 0x29, "Two Handed Combat" },
            { 0x2A, "Gearcraft" }, { 0x2B, "Void" }, { 0x2C, "Heavy Weapons" },
            { 0x2D, "Light Weapons" }, { 0x2E, "Finesse Weapons" },
            { 0x2F, "Missile Weapons" }, { 0x30, "Shield" }, { 0x31, "Dual Wield" },
            { 0x32, "Recklessness" }, { 0x33, "Sneak Attack" },
            { 0x34, "Dirty Fighting" }, { 0x35, "Challenge" }, { 0x36, "Summoning" },
        };

        private static readonly Dictionary<int, string> MasteryInfo = new Dictionary<int, string>
        {
            { 1, "Unarmed Weapon" }, { 2, "Sword" }, { 3, "Axe" }, { 4, "Mace" },
            { 5, "Spear" }, { 6, "Dagger" }, { 7, "Staff" }, { 8, "Bow" },
            { 9, "Crossbow" }, { 10, "Thrown" }, { 11, "Two Handed Combat" },
        };

        public static readonly Dictionary<int, string> AttributeSetInfo = new Dictionary<int, string>
        {
            { 2, "Test" },
            { 4, "Carraida's Benediction" }, { 5, "Noble Relic Set" }, { 6, "Ancient Relic Set" },
            { 7, "Relic Alduressa Set" }, { 8, "Shou-jen Set" }, { 9, "Empyrean Rings Set" },
            { 10, "Arm, Mind, Heart Set" }, { 11, "Coat of the Perfect Light Set" },
            { 12, "Leggings of Perfect Light Set" }, { 13, "Soldier's Set" },
            { 14, "Adept's Set" }, { 15, "Archer's Set" }, { 16, "Defender's Set" },
            { 17, "Tinker's Set" }, { 18, "Crafter's Set" }, { 19, "Hearty Set" },
            { 20, "Dexterous Set" }, { 21, "Wise Set" }, { 22, "Swift Set" },
            { 23, "Hardened Set" }, { 24, "Reinforced Set" }, { 25, "Interlocking Set" },
            { 26, "Flame Proof Set" }, { 27, "Acid Proof Set" }, { 28, "Cold Proof Set" },
            { 29, "Lightning Proof Set" }, { 30, "Dedication Set" },
            { 31, "Gladiatorial Clothing Set" }, { 32, "Ceremonial Clothing" },
            { 33, "Protective Clothing" }, { 34, "Noobie Armor" },
            { 35, "Sigil of Defense" }, { 36, "Sigil of Destruction" },
            { 37, "Sigil of Fury" }, { 38, "Sigil of Growth" }, { 39, "Sigil of Vigor" },
            { 40, "Heroic Protector Set" }, { 41, "Heroic Destroyer Set" },
            { 42, "Olthoi Armor D Red" }, { 43, "Olthoi Armor C Rat" },
            { 44, "Olthoi Armor C Red" }, { 45, "Olthoi Armor D Rat" },
            { 46, "Upgraded Relic Alduressa Set" }, { 47, "Upgraded Ancient Relic Set" },
            { 48, "Upgraded Noble Relic Set" },
            { 49, "Weave of Alchemy" }, { 50, "Weave of Arcane Lore" },
            { 51, "Weave of Armor Tinkering" }, { 52, "Weave of Assess Person" },
            { 53, "Weave of Light Weapons" }, { 54, "Weave of Missile Weapons" },
            { 55, "Weave of Cooking" }, { 56, "Weave of Creature Enchantment" },
            { 57, "Weave of Missile Weapons" }, { 58, "Weave of Finesse" },
            { 59, "Weave of Deception" }, { 60, "Weave of Fletching" },
            { 61, "Weave of Healing" }, { 62, "Weave of Item Enchantment" },
            { 63, "Weave of Item Tinkering" }, { 64, "Weave of Leadership" },
            { 65, "Weave of Life Magic" }, { 66, "Weave of Loyalty" },
            { 67, "Weave of Light Weapons" }, { 68, "Weave of Magic Defense" },
            { 69, "Weave of Magic Item Tinkering" }, { 70, "Weave of Mana Conversion" },
            { 71, "Weave of Melee Defense" }, { 72, "Weave of Missile Defense" },
            { 73, "Weave of Salvaging" }, { 74, "Weave of Light Weapons" },
            { 75, "Weave of Light Weapons" }, { 76, "Weave of Heavy Weapons" },
            { 77, "Weave of Missile Weapons" }, { 78, "Weave of Two Handed Combat" },
            { 79, "Weave of Light Weapons" }, { 80, "Weave of Void Magic" },
            { 81, "Weave of War Magic" }, { 82, "Weave of Weapon Tinkering" },
            { 83, "Weave of Assess Creature" }, { 84, "Weave of Dirty Fighting" },
            { 85, "Weave of Dual Wield" }, { 86, "Weave of Recklessness" },
            { 87, "Weave of Shield" }, { 88, "Weave of Sneak Attack" },
            { 89, "Ninja_New" }, { 90, "Weave of Summoning" },
            { 91, "Shrouded Soul" }, { 92, "Darkened Mind" }, { 93, "Clouded Spirit" },
            { 94, "Minor Stinging Shrouded Soul" }, { 95, "Minor Sparking Shrouded Soul" },
            { 96, "Minor Smoldering Shrouded Soul" }, { 97, "Minor Shivering Shrouded Soul" },
            { 98, "Minor Stinging Darkened Mind" }, { 99, "Minor Sparking Darkened Mind" },
            { 100, "Minor Smoldering Darkened Mind" }, { 101, "Minor Shivering Darkened Mind" },
            { 102, "Minor Stinging Clouded Spirit" }, { 103, "Minor Sparking Clouded Spirit" },
            { 104, "Minor Smoldering Clouded Spirit" }, { 105, "Minor Shivering Clouded Spirit" },
            { 106, "Major Stinging Shrouded Soul" }, { 107, "Major Sparking Shrouded Soul" },
            { 108, "Major Smoldering Shrouded Soul" }, { 109, "Major Shivering Shrouded Soul" },
            { 110, "Major Stinging Darkened Mind" }, { 111, "Major Sparking Darkened Mind" },
            { 112, "Major Smoldering Darkened Mind" }, { 113, "Major Shivering Darkened Mind" },
            { 114, "Major Stinging Clouded Spirit" }, { 115, "Major Sparking Clouded Spirit" },
            { 116, "Major Smoldering Clouded Spirit" }, { 117, "Major Shivering Clouded Spirit" },
            { 118, "Blackfire Stinging Shrouded Soul" }, { 119, "Blackfire Sparking Shrouded Soul" },
            { 120, "Blackfire Smoldering Shrouded Soul" }, { 121, "Blackfire Shivering Shrouded Soul" },
            { 122, "Blackfire Stinging Darkened Mind" }, { 123, "Blackfire Sparking Darkened Mind" },
            { 124, "Blackfire Smoldering Darkened Mind" }, { 125, "Blackfire Shivering Darkened Mind" },
            { 126, "Blackfire Stinging Clouded Spirit" }, { 127, "Blackfire Sparking Clouded Spirit" },
            { 128, "Blackfire Smoldering Clouded Spirit" }, { 129, "Blackfire Shivering Clouded Spirit" },
            { 130, "Shimmering Shadows" }, { 131, "Brown Society Locket" },
            { 132, "Yellow Society Locket" }, { 133, "Red Society Band" },
            { 134, "Green Society Band" }, { 135, "Purple Society Band" },
            { 136, "Blue Society Band" }, { 137, "Gauntlet Garb" },
        };

        private static readonly Dictionary<int, string> MaterialInfo = new Dictionary<int, string>
        {
            { 1, "Ceramic" }, { 2, "Porcelain" }, { 4, "Linen" }, { 5, "Satin" },
            { 6, "Silk" }, { 7, "Velvet" }, { 8, "Wool" }, { 10, "Agate" },
            { 11, "Amber" }, { 12, "Amethyst" }, { 13, "Aquamarine" }, { 14, "Azurite" },
            { 15, "Black Garnet" }, { 16, "Black Opal" }, { 17, "Bloodstone" },
            { 18, "Carnelian" }, { 19, "Citrine" }, { 20, "Diamond" }, { 21, "Emerald" },
            { 22, "Fire Opal" }, { 23, "Green Garnet" }, { 24, "Green Jade" },
            { 25, "Hematite" }, { 26, "Imperial Topaz" }, { 27, "Jet" },
            { 28, "Lapis Lazuli" }, { 29, "Lavender Jade" }, { 30, "Malachite" },
            { 31, "Moonstone" }, { 32, "Onyx" }, { 33, "Opal" }, { 34, "Peridot" },
            { 35, "Red Garnet" }, { 36, "Red Jade" }, { 37, "Rose Quartz" },
            { 38, "Ruby" }, { 39, "Sapphire" }, { 40, "Smokey Quartz" },
            { 41, "Sunstone" }, { 42, "Tiger Eye" }, { 43, "Tourmaline" },
            { 44, "Turquoise" }, { 45, "White Jade" }, { 46, "White Quartz" },
            { 47, "White Sapphire" }, { 48, "Yellow Garnet" }, { 49, "Yellow Topaz" },
            { 50, "Zircon" }, { 51, "Ivory" }, { 52, "Leather" },
            { 53, "Armoredillo Hide" }, { 54, "Gromnie Hide" }, { 55, "Reed Shark Hide" },
            { 57, "Brass" }, { 58, "Bronze" }, { 59, "Copper" }, { 60, "Gold" },
            { 61, "Iron" }, { 62, "Pyreal" }, { 63, "Silver" }, { 64, "Steel" },
            { 66, "Alabaster" }, { 67, "Granite" }, { 68, "Marble" }, { 69, "Obsidian" },
            { 70, "Sandstone" }, { 71, "Serpentine" }, { 73, "Ebony" },
            { 74, "Mahogany" }, { 75, "Oak" }, { 76, "Pine" }, { 77, "Teak" },
        };

        #endregion
    }
}
