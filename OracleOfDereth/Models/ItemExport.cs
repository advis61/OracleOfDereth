using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Serializes a list of Items to disk (txt / csv / json) under My Documents and returns the
    // path written. Kept separate from ItemList, which is just the identify/sort pipeline.
    public static class ItemExport
    {
        public static string ToText(List<Item> items, string nameOverride = null)
        {
            string path = ExportPath("txt", nameOverride);
            File.WriteAllLines(path, items.Select(t => t.Description));
            return path;
        }

        public static string ToCsv(List<Item> items, string nameOverride = null)
        {
            string path = ExportPath("csv", nameOverride);

            var lines = new List<string> { string.Join(",", Headers.Select(CsvEscape)) };
            foreach (Item item in items)
                lines.Add(string.Join(",", Row(item).Select(CsvEscape)));

            File.WriteAllLines(path, lines);
            return path;
        }

        public static string ToJson(List<Item> items, string nameOverride = null)
        {
            string path = ExportPath("json", nameOverride);

            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < items.Count; i++)
            {
                string[] row = Row(items[i]);
                sb.AppendLine("  {");

                int colCount = Math.Min(Headers.Length, row.Length);
                for (int c = 0; c < colCount; c++)
                {
                    string comma = c < colCount - 1 ? "," : "";
                    sb.AppendLine($"    {JsonEscape(Headers[c])}: {JsonEscape(row[c])}{comma}");
                }

                sb.AppendLine("  }" + (i < items.Count - 1 ? "," : ""));
            }
            sb.AppendLine("]");

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static string ExportPath(string extension, string nameOverride = null)
        {
            // The Items view exports under the player's own name; the Trade view passes the trade
            // partner's name to use instead (so a partner's wares aren't filed under our name).
            string raw = string.IsNullOrEmpty(nameOverride) ? CoreManager.Current.CharacterFilter.Name : nameOverride;
            string name = Regex.Replace((raw ?? "").ToLower(), "[^a-z0-9]", "-");

            string filename = $"{DateTime.Now:yyyyMMdd-HHmmss}-{name}-items.{extension}";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
        }

        private static readonly string[] Headers =
        {
            "Character", "Server", "Name", "ObjectClass", "Type", "Set", "Armor Level", "Imbues", "Tinks",
            "OD", "OA", "OM", "Damage", "Dmg Low", "Dmg High", "Elem Bonus", "Missile %", "Caster %",
            "Attack", "Melee D", "Magic D", "Missile D", "Mana C",
            "Spells", "Wield Req", "Wield Req Level", "Activation Req",
            "Lore", "Craft", "Value", "Burden",
            "Summon DMG", "Summon DEF",
            "Item Level",
            "D", "DR", "C", "CR", "CD", "CDR", "HB", "V"
        };

        private static string[] Row(Item item)
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

        private static string JsonEscape(string value)
        {
            if (value == null) return "null";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
