using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;

// claude --resume eaf48359-ba2c-4782-ad06-f48ca4b5f18d

namespace OracleOfDereth
{
    /// <summary>
    /// Formats a WorldObject's identification details into a readable string.
    /// Based on MagTools ItemInfo system.
    /// </summary>
    public class ItemInfo
    {
        private readonly WorldObject wo;
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

            for (int i = 0; i < wo.ActiveSpellCount; i++)
                activeSpells.Add(wo.ActiveSpell(i));

            for (int i = 0; i < wo.SpellCount; i++)
                innateSpells.Add(wo.Spell(i));
        }
        public static bool WeaponIdentified(WorldObject item)
        {
            if (item.ObjectClass != ObjectClass.MeleeWeapon &&
                item.ObjectClass != ObjectClass.MissileWeapon &&
                item.ObjectClass != ObjectClass.WandStaffOrb)
                return false;

            ItemInfo info = new ItemInfo(item);

            string odValue = info.GetODValue();
            string mdValue = info.GetMDValue();

            if (odValue == null && mdValue == null) return false;

            StringBuilder sb = new StringBuilder();
            sb.Append(item.Name);
            sb.Append(" [");
            if (odValue != null) sb.Append("OD: " + odValue);
            if (odValue != null && mdValue != null) sb.Append(" | ");
            if (mdValue != null) sb.Append("MD: " + mdValue);
            sb.Append("]");

            Util.Chat(sb.ToString(), Util.ColorCyan, "");
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // Material
            if (wo.Values(LongValueKey.Material) > 0)
            {
                if (MaterialInfo.TryGetValue(wo.Values(LongValueKey.Material), out string mat))
                    sb.Append(mat + " ");
                else
                    sb.Append("Unknown Material " + wo.Values(LongValueKey.Material) + " ");
            }

            // Name
            sb.Append(wo.Name);

            // Mastery
            if (wo.Values((LongValueKey)353) > 0)
            {
                if (MasteryInfo.TryGetValue(wo.Values((LongValueKey)353), out string mastery))
                    sb.Append(" (" + mastery + ")");
                else
                    sb.Append(" (Unknown Mastery " + wo.Values((LongValueKey)353) + ")");
            }

            // OD and MD Values (weapons only)
            string odValue = GetODValue();
            string mdValue = GetMDValue();

            if (odValue != null || mdValue != null)
            {
                sb.Append(" [");
                if (odValue != null) sb.Append("OD: " + odValue);
                if (odValue != null && mdValue != null) sb.Append(" | ");
                if (mdValue != null) sb.Append("MD: " + mdValue);
                sb.Append("]");
            }

            // Equipment Set
            int set = wo.Values((LongValueKey)265, 0);
            if (set != 0)
            {
                sb.Append(", ");
                if (AttributeSetInfo.TryGetValue(set, out string setName))
                    sb.Append(setName);
                else
                    sb.Append("Unknown Set");
            }

            // Armor Level
            if (wo.Values(LongValueKey.ArmorLevel) > 0)
                sb.Append(", AL " + wo.Values(LongValueKey.ArmorLevel));

            // Imbues
            if (wo.Values(LongValueKey.Imbued) > 0)
            {
                sb.Append(",");
                int imbued = wo.Values(LongValueKey.Imbued);
                if ((imbued & 1) == 1) sb.Append(" CS");
                if ((imbued & 2) == 2) sb.Append(" CB");
                if ((imbued & 4) == 4) sb.Append(" AR");
                if ((imbued & 8) == 8) sb.Append(" SlashRend");
                if ((imbued & 16) == 16) sb.Append(" PierceRend");
                if ((imbued & 32) == 32) sb.Append(" BludgeRend");
                if ((imbued & 64) == 64) sb.Append(" AcidRend");
                if ((imbued & 128) == 128) sb.Append(" FrostRend");
                if ((imbued & 256) == 256) sb.Append(" LightRend");
                if ((imbued & 512) == 512) sb.Append(" FireRend");
                if ((imbued & 1024) == 1024) sb.Append(" MeleeImbue");
                if ((imbued & 4096) == 4096) sb.Append(" MagicImbue");
                if ((imbued & 8192) == 8192) sb.Append(" Hematited");
                if ((imbued & 536870912) == 536870912) sb.Append(" MagicAbsorb");
            }

            // Tinks
            if (wo.Values(LongValueKey.NumberTimesTinkered) > 0)
                sb.Append(", Tinks " + wo.Values(LongValueKey.NumberTimesTinkered));

            // Damage (melee/missile)
            if (wo.Values(LongValueKey.MaxDamage) != 0 && wo.Values(DoubleValueKey.Variance) != 0)
                sb.Append(", " + (wo.Values(LongValueKey.MaxDamage) - (wo.Values(LongValueKey.MaxDamage) * wo.Values(DoubleValueKey.Variance))).ToString("N2") + "-" + wo.Values(LongValueKey.MaxDamage));
            else if (wo.Values(LongValueKey.MaxDamage) != 0)
                sb.Append(", " + wo.Values(LongValueKey.MaxDamage));

            // Elemental Damage Bonus
            if (wo.Values(LongValueKey.ElementalDmgBonus, 0) != 0)
                sb.Append(", +" + wo.Values(LongValueKey.ElementalDmgBonus));

            // Damage Bonus %
            if (wo.Values(DoubleValueKey.DamageBonus, 1) != 1)
                sb.Append(", +" + Math.Round(((wo.Values(DoubleValueKey.DamageBonus) - 1) * 100)) + "%");

            // Elemental Damage vs Monsters
            if (wo.Values(DoubleValueKey.ElementalDamageVersusMonsters, 1) != 1)
                sb.Append(", +" + Math.Round(((wo.Values(DoubleValueKey.ElementalDamageVersusMonsters) - 1) * 100)) + "%vs. Monsters");

            // Attack Bonus
            if (wo.Values(DoubleValueKey.AttackBonus, 1) != 1)
                sb.Append(", +" + Math.Round(((wo.Values(DoubleValueKey.AttackBonus) - 1) * 100)) + "%a");

            // Melee Defense Bonus
            if (wo.Values(DoubleValueKey.MeleeDefenseBonus, 1) != 1)
                sb.Append(", " + Math.Round(((wo.Values(DoubleValueKey.MeleeDefenseBonus) - 1) * 100)) + "%md");

            // Magic Defense Bonus
            if (wo.Values(DoubleValueKey.MagicDBonus, 1) != 1)
                sb.Append(", " + Math.Round(((wo.Values(DoubleValueKey.MagicDBonus) - 1) * 100), 1) + "%mgc.d");

            // Missile Defense Bonus
            if (wo.Values(DoubleValueKey.MissileDBonus, 1) != 1)
                sb.Append(", " + Math.Round(((wo.Values(DoubleValueKey.MissileDBonus) - 1) * 100), 1) + "%msl.d");

            // Mana Conversion Bonus
            if (wo.Values(DoubleValueKey.ManaCBonus) != 0)
                sb.Append(", " + Math.Round((wo.Values(DoubleValueKey.ManaCBonus) * 100)) + "%mc");

            // Buffed Values (weapons only)
            if (wo.ObjectClass == ObjectClass.MeleeWeapon || wo.ObjectClass == ObjectClass.MissileWeapon || wo.ObjectClass == ObjectClass.WandStaffOrb)
            {
                sb.Append(", (");

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
            }

            // Spells
            if (innateSpells.Count > 0)
            {
                FileService service = CoreManager.Current.Filter<FileService>();
                List<int> sorted = new List<int>(innateSpells);
                sorted.Sort();
                sorted.Reverse();

                bool isLootGenerated = wo.LongKeys.Contains((int)LongValueKey.Material);
                bool isUnenchantable = wo.Values(LongValueKey.Unenchantable, 0) != 0;

                foreach (int spellId in sorted)
                {
                    Decal.Filters.Spell spell = service.SpellTable.GetById(spellId);
                    if (spell == null) continue;

                    string name = spell.Name;

                    if (!isLootGenerated)
                        goto ShowSpell;

                    if (name.Contains("Minor Impenetrability") || name.Contains("Major Impenetrability") || name.Contains("Epic Impenetrability") || name.Contains("Legendary Impenetrability"))
                        goto ShowSpell;

                    if (name.Contains("Augmented"))
                        goto ShowSpell;

                    if (isUnenchantable)
                    {
                        if (name.Contains(" Bane") || name.Contains("Impen") || name.StartsWith("Brogard"))
                            goto ShowSpell;
                    }
                    else
                    {
                        if (name.Contains(" Bane") || name.Contains("Impen") || name.StartsWith("Brogard"))
                            continue;
                    }

                    if (name.EndsWith(" I") || name.EndsWith(" II") || name.EndsWith(" III") || name.EndsWith(" IV") || name.EndsWith(" V"))
                        continue;
                    if (name.EndsWith(" VI"))
                        continue;
                    //if (name.EndsWith(" VII"))
                    //    continue;
                    //if (name.Contains("Incantation"))
                    //    continue;

                    ShowSpell:
                    sb.Append(", " + name);
                }
            }

            // Wield Requirements
            if (wo.Values(LongValueKey.WieldReqValue) > 0)
            {
                if (wo.Values(LongValueKey.WieldReqType) == 7 && wo.Values(LongValueKey.WieldReqAttribute) == 1)
                    sb.Append(", Wield Lvl " + wo.Values(LongValueKey.WieldReqValue));
                else
                {
                    if (SkillInfo.TryGetValue(wo.Values(LongValueKey.WieldReqAttribute), out string skillName))
                        sb.Append(", " + skillName + " " + wo.Values(LongValueKey.WieldReqValue));
                    else
                        sb.Append(", Unknown Skill " + wo.Values(LongValueKey.WieldReqAttribute) + " " + wo.Values(LongValueKey.WieldReqValue));
                }
            }

            // Summoning Gem Level
            if (wo.Values((LongValueKey)369) > 0)
                sb.Append(", Lvl " + wo.Values((LongValueKey)369));

            // Activation Requirement
            if (wo.Values(LongValueKey.SkillLevelReq) > 0 && (wo.Values(LongValueKey.WieldReqAttribute) != wo.Values(LongValueKey.ActivationReqSkillId) || wo.Values(LongValueKey.WieldReqValue) < wo.Values(LongValueKey.SkillLevelReq)))
            {
                if (SkillInfo.TryGetValue(wo.Values(LongValueKey.ActivationReqSkillId), out string skillName))
                    sb.Append(", " + skillName + " " + wo.Values(LongValueKey.SkillLevelReq) + " to Activate");
                else
                    sb.Append(", Unknown Skill " + wo.Values(LongValueKey.ActivationReqSkillId) + " " + wo.Values(LongValueKey.SkillLevelReq) + " to Activate");
            }

            // Summoning Gem Skill Requirements
            if (wo.Values((LongValueKey)366) > 0 && wo.Values((LongValueKey)367) > 0)
            {
                if (SkillInfo.TryGetValue(wo.Values((LongValueKey)366), out string skillName))
                    sb.Append(", " + skillName + " " + wo.Values((LongValueKey)367));
                else
                    sb.Append(", Unknown Skill " + wo.Values((LongValueKey)366) + " " + wo.Values((LongValueKey)367));
            }

            // Skill
            if (wo.Values((LongValueKey)368) > 0 && wo.Values((LongValueKey)367) > 0)
            {
                if (SkillInfo.TryGetValue(wo.Values((LongValueKey)368), out string skillName))
                    sb.Append(", Spec " + skillName + " " + wo.Values((LongValueKey)367));
                else
                    sb.Append(", Unknown Skill Spec " + wo.Values((LongValueKey)368) + " " + wo.Values((LongValueKey)367));
            }

            // Lore Difficulty
            if (wo.Values(LongValueKey.LoreRequirement) > 0)
                sb.Append(", Diff " + wo.Values(LongValueKey.LoreRequirement));

            // Workmanship
            if (wo.ObjectClass == ObjectClass.Salvage)
            {
                if (wo.Values(DoubleValueKey.SalvageWorkmanship) > 0)
                    sb.Append(", Work " + wo.Values(DoubleValueKey.SalvageWorkmanship).ToString("N2"));
            }
            else
            {
                if (wo.Values(LongValueKey.Workmanship) > 0 && wo.Values(LongValueKey.NumberTimesTinkered) != 10)
                    sb.Append(", Craft " + wo.Values(LongValueKey.Workmanship));
            }

            // Armor Protections (unenchantable armor)
            if (wo.ObjectClass == ObjectClass.Armor && wo.Values(LongValueKey.Unenchantable, 0) != 0)
            {
                sb.Append(", [" +
                    wo.Values(DoubleValueKey.SlashProt).ToString("N1") + "/" +
                    wo.Values(DoubleValueKey.PierceProt).ToString("N1") + "/" +
                    wo.Values(DoubleValueKey.BludgeonProt).ToString("N1") + "/" +
                    wo.Values(DoubleValueKey.ColdProt).ToString("N1") + "/" +
                    wo.Values(DoubleValueKey.FireProt).ToString("N1") + "/" +
                    wo.Values(DoubleValueKey.AcidProt).ToString("N1") + "/" +
                    wo.Values(DoubleValueKey.LightningProt).ToString("N1") + "]");
            }

            // Value and Burden
            if (wo.Values(LongValueKey.Value) > 0)
                sb.Append(", Value " + String.Format("{0:n0}", wo.Values(LongValueKey.Value)));

            if (wo.Values(LongValueKey.Burden) > 0)
                sb.Append(", BU " + wo.Values(LongValueKey.Burden));

            // Ratings
            AppendRatings(sb);

            // Keyring
            if (wo.ObjectClass == ObjectClass.Misc && wo.Name.Contains("Keyring"))
                sb.Append(", Keys: " + wo.Values(LongValueKey.KeysHeld) + ", Uses: " + wo.Values(LongValueKey.UsesRemaining));

            return sb.ToString();
        }

        private void AppendRatings(StringBuilder sb)
        {
            int d = wo.Values((LongValueKey)370);
            int dr = wo.Values((LongValueKey)371);
            int c = wo.Values((LongValueKey)372);
            int cr = wo.Values((LongValueKey)373);
            int cd = wo.Values((LongValueKey)374);
            int cdr = wo.Values((LongValueKey)375);
            int hb = wo.Values((LongValueKey)376);
            int v = wo.Values((LongValueKey)379);

            if (d + dr + c + cr + cd + cdr + hb + v <= 0) return;

            sb.Append(", [");
            bool first = true;
            void Add(string label, int val) { if (val > 0) { if (!first) sb.Append(", "); sb.Append(label + " " + val); first = false; } }
            Add("D", d); Add("DR", dr); Add("C", c); Add("CD", cd); Add("CR", cr); Add("CDR", cdr); Add("HB", hb); Add("V", v);
            sb.Append("]");
        }

        #region Buffed Value Calculations

        private const int Key_MaxDamage = 218103842;
        private const int Key_ArmorLevel = 28;
        private const int Key_ElementalDmgBonus = 204;
        private const int Key_Imbued = 179;
        private const int Key_Tinks = 171;
        private const int Key_Material = 131;
        private const int Key_Variance = 167772171;
        private const int Key_DamageBonus = 167772174;
        private const int Key_ElementalDmgVsMonsters = 152;
        private const int Key_AttackBonus = 167772172;
        private const int Key_MeleeDefenseBonus = 29;
        private const int Key_ManaCBonus = 144;

        private int GetBuffedIntValue(int key, int defaultValue = 0)
        {
            if (!intValues.ContainsKey(key))
                return defaultValue;

            int value = intValues[key];

            foreach (int spell in activeSpells)
            {
                if (IntSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key)
                    value -= effect.Change;
            }

            foreach (int spell in innateSpells)
            {
                if (IntSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key)
                    value += effect.Bonus;
            }

            return value;
        }

        private double GetBuffedDoubleValue(int key, double defaultValue = 0)
        {
            if (!doubleValues.ContainsKey(key))
                return defaultValue;

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
                if (DoubleSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key && Math.Abs(effect.Bonus - 0) > Double.Epsilon)
                {
                    if (Math.Abs(effect.Change - 1) < Double.Epsilon)
                        value *= effect.Bonus;
                    else
                        value += effect.Bonus;
                }
            }

            return value;
        }

        private double CalcBuffedTinkedDoT()
        {
            if (!doubleValues.ContainsKey(Key_Variance) || !intValues.ContainsKey(Key_MaxDamage))
                return -1;

            double variance = doubleValues[Key_Variance];
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

        #region OD (Over Damage) Calculations

        // OD+0 = top roll for that weapon type. OD+N = N above top roll. OD-N = N below.
        // Not shown when equipped (character buffs inflate stats) or below OD-10.

        private const int Key_EquipSkill = 218103840;
        private const int Key_CurrentWieldedLocation = 10;

        private string GetODValue()
        {
            if (wo.Values((LongValueKey)Key_CurrentWieldedLocation) > 0) return null;

            if (wo.ObjectClass == ObjectClass.MeleeWeapon) return GetMeleeOD();
            if (wo.ObjectClass == ObjectClass.MissileWeapon) return GetMissileOD();
            if (wo.ObjectClass == ObjectClass.WandStaffOrb) return GetCasterOD();

            return null;
        }

        private string GetMeleeOD()
        {
            int equipSkill = intValues.ContainsKey(Key_EquipSkill) ? intValues[Key_EquipSkill] : 0;
            int wieldSkill = wo.Values(LongValueKey.WieldReqAttribute);
            int skill = equipSkill > 0 ? equipSkill : wieldSkill;

            int mastery = intValues.ContainsKey(353) ? intValues[353] : 0;
            if (mastery == 0) return null;

            int rawDmg = wo.Values(LongValueKey.MaxDamage);
            int tableMax = 0;

            if (skill == 0x29 || mastery == 11) // Two Handed
                tableMax = IsTwoHandedSpear() ? TwoHandedSpearMax : TwoHandedCleaverMax;
            else if (skill == 0x2C) // Heavy
            {
                if (HeavyMultiMax.ContainsKey(mastery) && rawDmg <= HeavyMultiMax[mastery] + 15)
                    tableMax = HeavyMultiMax[mastery];
                else if (HeavyMax.ContainsKey(mastery))
                    tableMax = HeavyMax[mastery];
            }
            else if (skill == 0x2D || skill == 0x2E) // Light / Finesse
            {
                if (mastery == 4 && wo.Name.IndexOf("Jitte", StringComparison.OrdinalIgnoreCase) >= 0)
                    tableMax = LightJitteMax;
                else if (LightMultiMax.ContainsKey(mastery) && rawDmg <= LightMultiMax[mastery] + 15)
                    tableMax = LightMultiMax[mastery];
                else if (LightMax.ContainsKey(mastery))
                    tableMax = LightMax[mastery];
            }
            else return null;

            if (tableMax <= 0) return null;
            return FormatOD(GetBuffedIntValue(Key_MaxDamage) - tableMax);
        }

        private string GetMissileOD()
        {
            int mastery = intValues.ContainsKey(353) ? intValues[353] : 0;

            // Each missile type has its own dmgPct-to-elem conversion factor (K) and reference (refC).
            // K represents how many % of damage modifier equals 1 elemental damage point.
            double conversionK, refC;
            if (mastery == 8)       { conversionK = 3.5; refC = 423.0 / 7.0; } // Bow
            else if (mastery == 9)  { conversionK = 3.5; refC = 473.0 / 7.0; } // Crossbow (estimated)
            else if (mastery == 10) { conversionK = 2.5; refC = 86.0; }        // Thrown
            else return null;

            int elemBonus = wo.Values(LongValueKey.ElementalDmgBonus, 0);
            int cantripBonus = GetCantripIntBonus(Key_MaxDamage);
            double dmgPct = (wo.Values(DoubleValueKey.DamageBonus, 1) - 1) * 100;

            double score = elemBonus + cantripBonus + dmgPct / conversionK;
            int od = (int)Math.Floor(score - refC);
            return FormatOD(od);
        }

        private string GetCasterOD()
        {
            int maxPct = CasterMax;
            double buffedPctValue = GetBuffedDoubleValue(Key_ElementalDmgVsMonsters);
            int buffedPct = (int)Math.Round((buffedPctValue - 1) * 100);

            return FormatOD(buffedPct - maxPct);
        }

        private static string FormatOD(int od)
        {
            if (od < -10) return null;
            return od >= 0 ? "+" + od : "" + od;
        }

        private int GetCantripIntBonus(int key)
        {
            int bonus = 0;
            foreach (int spell in innateSpells)
            {
                if (IntSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key)
                    bonus += effect.Bonus;
            }
            return bonus;
        }

        private bool IsTwoHandedSpear()
        {
            string name = wo.Name.ToLower();
            return name.Contains("spear") || name.Contains("pike") || name.Contains("assagai") || name.Contains("yari") || name.Contains("naginata") || name.Contains("trident");
        }

        #endregion

        #region MD (Melee Defense) Calculations

        // MD+0 = max defense roll for that weapon type. MD+N = N% above max.
        // Not shown when equipped or below MD-10.

        private string GetMDValue()
        {
            if (wo.Values((LongValueKey)Key_CurrentWieldedLocation) > 0)
                return null;

            if (wo.ObjectClass != ObjectClass.MeleeWeapon && wo.ObjectClass != ObjectClass.MissileWeapon && wo.ObjectClass != ObjectClass.WandStaffOrb)
                return null;

            // Raw defense % from the item
            double rawDefPct = Math.Round((wo.Values(DoubleValueKey.MeleeDefenseBonus, 1) - 1) * 100);
            if (rawDefPct <= 0) return null;

            // Add Defender cantrip bonus
            double cantripDefPct = GetCantripDoubleBonus(Key_MeleeDefenseBonus) * 100;
            int totalDef = (int)Math.Round(rawDefPct + cantripDefPct);

            // Determine max defense for this weapon type
            int maxDef = GetMaxDefense();
            if (maxDef <= 0) return null;

            return FormatMD(totalDef - maxDef);
        }

        private int GetMaxDefense()
        {
            int mastery = intValues.ContainsKey(353) ? intValues[353] : 0;

            // Missile and Caster weapons all have 20% max defense
            if (wo.ObjectClass == ObjectClass.MissileWeapon || wo.ObjectClass == ObjectClass.WandStaffOrb)
                return 20;

            // Two-Handed weapons
            int equipSkill = intValues.ContainsKey(Key_EquipSkill) ? intValues[Key_EquipSkill] : 0;
            int wieldSkill = wo.Values(LongValueKey.WieldReqAttribute);
            int skill = equipSkill > 0 ? equipSkill : wieldSkill;

            if (skill == 0x29 || mastery == 11)
                return IsTwoHandedSpear() ? 20 : 18;

            // Finesse Jitte (special case: 25% instead of normal Mace 22%)
            if ((skill == 0x2E) && mastery == 4 && wo.Name.IndexOf("Jitte", StringComparison.OrdinalIgnoreCase) >= 0)
                return 25;

            // Melee weapons by mastery (same across Heavy/Light/Finesse)
            if (MaxDefenseByMastery.TryGetValue(mastery, out int maxDef))
                return maxDef;

            return 0;
        }

        private double GetCantripDoubleBonus(int key)
        {
            double bonus = 0;
            foreach (int spell in innateSpells)
            {
                if (DoubleSpellEffects.TryGetValue(spell, out var effect) && effect.Key == key && effect.Bonus > 0)
                    bonus += effect.Bonus;
            }
            return bonus;
        }

        private static string FormatMD(int md)
        {
            if (md < -10) return null;
            return md >= 0 ? "+" + md : "" + md;
        }

        // Max melee defense bonus % by mastery (same for Heavy/Light/Finesse)
        private static readonly Dictionary<int, int> MaxDefenseByMastery = new Dictionary<int, int>
        {
            { 1, 20 },  // UA
            { 2, 20 },  // Sword
            { 3, 18 },  // Axe
            { 4, 22 },  // Mace
            { 5, 15 },  // Spear
            { 6, 20 },  // Dagger
            { 7, 25 },  // Staff
        };

        #region OD Max Damage Tables

        private static readonly Dictionary<int, int> HeavyMax = new Dictionary<int, int>
        {
            { 3, 74 },  // Axe
            { 6, 71 },  // Dagger
            { 4, 69 },  // Mace
            { 5, 72 },  // Spear
            { 2, 71 },  // Sword
            { 7, 70 },  // Staff
            { 1, 59 },  // UA
        };
        private static readonly Dictionary<int, int> HeavyMultiMax = new Dictionary<int, int>
        {
            { 6, 38 },  // Dagger Multi
            { 2, 38 },  // Sword Multi
        };

        private static readonly Dictionary<int, int> LightMax = new Dictionary<int, int>
        {
            { 3, 61 },  // Axe
            { 6, 58 },  // Dagger
            { 4, 57 },  // Mace
            { 5, 60 },  // Spear
            { 2, 58 },  // Sword
            { 7, 57 },  // Staff
            { 1, 48 },  // UA
        };
        private static readonly Dictionary<int, int> LightMultiMax = new Dictionary<int, int>
        {
            { 6, 28 },  // Dagger Multi
            { 2, 28 },  // Sword Multi
        };

        private const int LightJitteMax = 57;
        private const int TwoHandedCleaverMax = 45;
        private const int TwoHandedSpearMax = 48;
        private const int CasterMax = 18;

        #endregion

        #endregion

        #region Spell Effect Dictionaries

        private struct SpellEffect<T>
        {
            public readonly int Key;
            public readonly T Change;
            public readonly T Bonus;

            public SpellEffect(int key, T change, T bonus = default(T))
            {
                Key = key;
                Change = change;
                Bonus = bonus;
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

        #region Dictionaries

        private static readonly Dictionary<int, string> SkillInfo = new Dictionary<int, string>
        {
            { 0x1, "Axe" },
            { 0x2, "Bow" },
            { 0x3, "Crossbow" },
            { 0x4, "Dagger" },
            { 0x5, "Mace" },
            { 0x6, "Melee Defense" },
            { 0x7, "Missile Defense" },
            { 0x8, "Sling" },
            { 0x9, "Spear" },
            { 0xA, "Staff" },
            { 0xB, "Sword" },
            { 0xC, "Thrown Weapons" },
            { 0xD, "Unarmed Combat" },
            { 0xE, "Arcane Lore" },
            { 0xF, "Magic Defense" },
            { 0x10, "Mana Conversion" },
            { 0x12, "Item Tinkering" },
            { 0x13, "Assess Person" },
            { 0x14, "Deception" },
            { 0x15, "Healing" },
            { 0x16, "Jump" },
            { 0x17, "Lockpick" },
            { 0x18, "Run" },
            { 0x1B, "Assess Creature" },
            { 0x1C, "Weapon Tinkering" },
            { 0x1D, "Armor Tinkering" },
            { 0x1E, "Magic Item Tinkering" },
            { 0x1F, "Creature Enchantment" },
            { 0x20, "Item Enchantment" },
            { 0x21, "Life Magic" },
            { 0x22, "War Magic" },
            { 0x23, "Leadership" },
            { 0x24, "Loyalty" },
            { 0x25, "Fletching" },
            { 0x26, "Alchemy" },
            { 0x27, "Cooking" },
            { 0x28, "Salvaging" },
            { 0x29, "Two Handed Combat" },
            { 0x2A, "Gearcraft" },
            { 0x2B, "Void" },
            { 0x2C, "Heavy Weapons" },
            { 0x2D, "Light Weapons" },
            { 0x2E, "Finesse Weapons" },
            { 0x2F, "Missile Weapons" },
            { 0x30, "Shield" },
            { 0x31, "Dual Wield" },
            { 0x32, "Recklessness" },
            { 0x33, "Sneak Attack" },
            { 0x34, "Dirty Fighting" },
            { 0x35, "Challenge" },
            { 0x36, "Summoning" },
        };

        private static readonly Dictionary<int, string> MasteryInfo = new Dictionary<int, string>
        {
            { 1, "Unarmed Weapon" },
            { 2, "Sword" },
            { 3, "Axe" },
            { 4, "Mace" },
            { 5, "Spear" },
            { 6, "Dagger" },
            { 7, "Staff" },
            { 8, "Bow" },
            { 9, "Crossbow" },
            { 10, "Thrown" },
            { 11, "Two Handed Combat" },
        };

        private static readonly Dictionary<int, string> AttributeSetInfo = new Dictionary<int, string>
        {
            { 2, "Test" },
            { 4, "Carraida's Benediction" },
            { 5, "Noble Relic Set" },
            { 6, "Ancient Relic Set" },
            { 7, "Relic Alduressa Set" },
            { 8, "Shou-jen Set" },
            { 9, "Empyrean Rings Set" },
            { 10, "Arm, Mind, Heart Set" },
            { 11, "Coat of the Perfect Light Set" },
            { 12, "Leggings of Perfect Light Set" },
            { 13, "Soldier's Set" },
            { 14, "Adept's Set" },
            { 15, "Archer's Set" },
            { 16, "Defender's Set" },
            { 17, "Tinker's Set" },
            { 18, "Crafter's Set" },
            { 19, "Hearty Set" },
            { 20, "Dexterous Set" },
            { 21, "Wise Set" },
            { 22, "Swift Set" },
            { 23, "Hardened Set" },
            { 24, "Reinforced Set" },
            { 25, "Interlocking Set" },
            { 26, "Flame Proof Set" },
            { 27, "Acid Proof Set" },
            { 28, "Cold Proof Set" },
            { 29, "Lightning Proof Set" },
            { 30, "Dedication Set" },
            { 31, "Gladiatorial Clothing Set" },
            { 32, "Ceremonial Clothing" },
            { 33, "Protective Clothing" },
            { 34, "Noobie Armor" },
            { 35, "Sigil of Defense" },
            { 36, "Sigil of Destruction" },
            { 37, "Sigil of Fury" },
            { 38, "Sigil of Growth" },
            { 39, "Sigil of Vigor" },
            { 40, "Heroic Protector Set" },
            { 41, "Heroic Destroyer Set" },
            { 42, "Olthoi Armor D Red" },
            { 43, "Olthoi Armor C Rat" },
            { 44, "Olthoi Armor C Red" },
            { 45, "Olthoi Armor D Rat" },
            { 46, "Upgraded Relic Alduressa Set" },
            { 47, "Upgraded Ancient Relic Set" },
            { 48, "Upgraded Noble Relic Set" },
            { 49, "Weave of Alchemy" },
            { 50, "Weave of Arcane Lore" },
            { 51, "Weave of Armor Tinkering" },
            { 52, "Weave of Assess Person" },
            { 53, "Weave of Light Weapons" },
            { 54, "Weave of Missile Weapons" },
            { 55, "Weave of Cooking" },
            { 56, "Weave of Creature Enchantment" },
            { 57, "Weave of Missile Weapons" },
            { 58, "Weave of Finesse" },
            { 59, "Weave of Deception" },
            { 60, "Weave of Fletching" },
            { 61, "Weave of Healing" },
            { 62, "Weave of Item Enchantment" },
            { 63, "Weave of Item Tinkering" },
            { 64, "Weave of Leadership" },
            { 65, "Weave of Life Magic" },
            { 66, "Weave of Loyalty" },
            { 67, "Weave of Light Weapons" },
            { 68, "Weave of Magic Defense" },
            { 69, "Weave of Magic Item Tinkering" },
            { 70, "Weave of Mana Conversion" },
            { 71, "Weave of Melee Defense" },
            { 72, "Weave of Missile Defense" },
            { 73, "Weave of Salvaging" },
            { 74, "Weave of Light Weapons" },
            { 75, "Weave of Light Weapons" },
            { 76, "Weave of Heavy Weapons" },
            { 77, "Weave of Missile Weapons" },
            { 78, "Weave of Two Handed Combat" },
            { 79, "Weave of Light Weapons" },
            { 80, "Weave of Void Magic" },
            { 81, "Weave of War Magic" },
            { 82, "Weave of Weapon Tinkering" },
            { 83, "Weave of Assess Creature" },
            { 84, "Weave of Dirty Fighting" },
            { 85, "Weave of Dual Wield" },
            { 86, "Weave of Recklessness" },
            { 87, "Weave of Shield" },
            { 88, "Weave of Sneak Attack" },
            { 89, "Ninja_New" },
            { 90, "Weave of Summoning" },
            { 91, "Shrouded Soul" },
            { 92, "Darkened Mind" },
            { 93, "Clouded Spirit" },
            { 94, "Minor Stinging Shrouded Soul" },
            { 95, "Minor Sparking Shrouded Soul" },
            { 96, "Minor Smoldering Shrouded Soul" },
            { 97, "Minor Shivering Shrouded Soul" },
            { 98, "Minor Stinging Darkened Mind" },
            { 99, "Minor Sparking Darkened Mind" },
            { 100, "Minor Smoldering Darkened Mind" },
            { 101, "Minor Shivering Darkened Mind" },
            { 102, "Minor Stinging Clouded Spirit" },
            { 103, "Minor Sparking Clouded Spirit" },
            { 104, "Minor Smoldering Clouded Spirit" },
            { 105, "Minor Shivering Clouded Spirit" },
            { 106, "Major Stinging Shrouded Soul" },
            { 107, "Major Sparking Shrouded Soul" },
            { 108, "Major Smoldering Shrouded Soul" },
            { 109, "Major Shivering Shrouded Soul" },
            { 110, "Major Stinging Darkened Mind" },
            { 111, "Major Sparking Darkened Mind" },
            { 112, "Major Smoldering Darkened Mind" },
            { 113, "Major Shivering Darkened Mind" },
            { 114, "Major Stinging Clouded Spirit" },
            { 115, "Major Sparking Clouded Spirit" },
            { 116, "Major Smoldering Clouded Spirit" },
            { 117, "Major Shivering Clouded Spirit" },
            { 118, "Blackfire Stinging Shrouded Soul" },
            { 119, "Blackfire Sparking Shrouded Soul" },
            { 120, "Blackfire Smoldering Shrouded Soul" },
            { 121, "Blackfire Shivering Shrouded Soul" },
            { 122, "Blackfire Stinging Darkened Mind" },
            { 123, "Blackfire Sparking Darkened Mind" },
            { 124, "Blackfire Smoldering Darkened Mind" },
            { 125, "Blackfire Shivering Darkened Mind" },
            { 126, "Blackfire Stinging Clouded Spirit" },
            { 127, "Blackfire Sparking Clouded Spirit" },
            { 128, "Blackfire Smoldering Clouded Spirit" },
            { 129, "Blackfire Shivering Clouded Spirit" },
            { 130, "Shimmering Shadows" },
            { 131, "Brown Society Locket" },
            { 132, "Yellow Society Locket" },
            { 133, "Red Society Band" },
            { 134, "Green Society Band" },
            { 135, "Purple Society Band" },
            { 136, "Blue Society Band" },
            { 137, "Gauntlet Garb" },
        };

        private static readonly Dictionary<int, string> MaterialInfo = new Dictionary<int, string>
        {
            { 1, "Ceramic" },
            { 2, "Porcelain" },
            { 4, "Linen" },
            { 5, "Satin" },
            { 6, "Silk" },
            { 7, "Velvet" },
            { 8, "Wool" },
            { 10, "Agate" },
            { 11, "Amber" },
            { 12, "Amethyst" },
            { 13, "Aquamarine" },
            { 14, "Azurite" },
            { 15, "Black Garnet" },
            { 16, "Black Opal" },
            { 17, "Bloodstone" },
            { 18, "Carnelian" },
            { 19, "Citrine" },
            { 20, "Diamond" },
            { 21, "Emerald" },
            { 22, "Fire Opal" },
            { 23, "Green Garnet" },
            { 24, "Green Jade" },
            { 25, "Hematite" },
            { 26, "Imperial Topaz" },
            { 27, "Jet" },
            { 28, "Lapis Lazuli" },
            { 29, "Lavender Jade" },
            { 30, "Malachite" },
            { 31, "Moonstone" },
            { 32, "Onyx" },
            { 33, "Opal" },
            { 34, "Peridot" },
            { 35, "Red Garnet" },
            { 36, "Red Jade" },
            { 37, "Rose Quartz" },
            { 38, "Ruby" },
            { 39, "Sapphire" },
            { 40, "Smokey Quartz" },
            { 41, "Sunstone" },
            { 42, "Tiger Eye" },
            { 43, "Tourmaline" },
            { 44, "Turquoise" },
            { 45, "White Jade" },
            { 46, "White Quartz" },
            { 47, "White Sapphire" },
            { 48, "Yellow Garnet" },
            { 49, "Yellow Topaz" },
            { 50, "Zircon" },
            { 51, "Ivory" },
            { 52, "Leather" },
            { 53, "Armoredillo Hide" },
            { 54, "Gromnie Hide" },
            { 55, "Reed Shark Hide" },
            { 57, "Brass" },
            { 58, "Bronze" },
            { 59, "Copper" },
            { 60, "Gold" },
            { 61, "Iron" },
            { 62, "Pyreal" },
            { 63, "Silver" },
            { 64, "Steel" },
            { 66, "Alabaster" },
            { 67, "Granite" },
            { 68, "Marble" },
            { 69, "Obsidian" },
            { 70, "Sandstone" },
            { 71, "Serpentine" },
            { 73, "Ebony" },
            { 74, "Mahogany" },
            { 75, "Oak" },
            { 76, "Pine" },
            { 77, "Teak" },
        };

        #endregion
    }
}
