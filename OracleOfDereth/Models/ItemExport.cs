using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static string ToText(List<ItemListRow> items, string nameOverride = null)
        {
            string path = ExportPath("txt", nameOverride);
            File.WriteAllLines(path, items.Select(t => t.DescriptionWithOwner));
            return path;
        }

        public static string ToCsv(List<ItemListRow> items, string nameOverride = null)
        {
            string path = ExportPath("csv", nameOverride);

            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false))) WriteCsv(writer, items);
            return path;
        }

        private static void WriteCsv(TextWriter writer, List<ItemListRow> items)
        {
            writer.WriteLine(string.Join(",", Headers.Select(CsvEscape)));
            foreach (ItemListRow item in items)
                writer.WriteLine(string.Join(",", Row(item).Select(value => CsvEscape(Convert.ToString(value, CultureInfo.InvariantCulture)))));
        }

        public static string ToJson(List<ItemListRow> items, string nameOverride = null)
        {
            string path = ExportPath("json", nameOverride);

            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false))) WriteJson(writer, items);
            return path;
        }

        private static void WriteJson(TextWriter writer, List<ItemListRow> items)
        {
            writer.WriteLine("[");
            for (int i = 0; i < items.Count; i++)
            {
                object[] row = Row(items[i]);
                writer.WriteLine("  {");

                int colCount = Math.Min(Headers.Length, row.Length);
                for (int c = 0; c < colCount; c++)
                {
                    string comma = c < colCount - 1 ? "," : "";
                    string value = row[c] == null || row[c] is string
                        ? Util.JsonString(row[c] as string) : Convert.ToString(row[c], CultureInfo.InvariantCulture);
                    writer.WriteLine($"    {Util.JsonString(Headers[c])}: {value}{comma}");
                }

                writer.WriteLine("  }" + (i < items.Count - 1 ? "," : ""));
            }
            writer.WriteLine("]");
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
            "Lore", "Workmanship", "Value", "Burden",
            "Summon DMG", "Summon DEF",
            "Item Level",
            "D", "DR", "C", "CR", "CD", "CDR", "HB", "V",
            "Item ID", "Material", "Element", "Quantity", "Uses Remaining", "Keys Held"
        };

        private static object[] Row(ItemListRow item)
        {
            ItemInfo info = new ItemInfo(item.Item);
            var details = new object[]
            {
                unchecked((uint)item.Id), info.GetMaterial(), info.GetElementName(),
                item.Item.TryGetValue(LongValueKey.StackCount, out int quantity) ? quantity : item.IsComplete ? 1 : (int?)null,
                item.Item.TryGetValue(LongValueKey.UsesRemaining, out int uses) ? uses : (int?)null,
                item.Item.TryGetValue(LongValueKey.KeysHeld, out int keys) ? keys : (int?)null
            };
            if (!item.IsComplete)
            {
                var row = new object[Headers.Length - details.Length];
                row[0] = item.Character;
                row[1] = item.Server;
                row[2] = item.DisplayName;
                row[3] = info.GetObjectClassName();
                return row.Concat(details).ToArray();
            }

            return new object[] {
                item.Character,
                item.Server,
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
                info.IsSalvage
                    ? (item.Item.TryGetValue(DoubleValueKey.SalvageWorkmanship, out double work) ? work : (double?)null)
                    : (item.Item.TryGetValue(LongValueKey.Workmanship, out int craft) ? craft : (double?)null),
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
            }.Concat(details).ToArray();
        }

        // See the note in QuestExport: one escaping rule for every csv the plugin writes.
        private static string CsvEscape(string value) => Util.CsvEscape(value);

    }
}
