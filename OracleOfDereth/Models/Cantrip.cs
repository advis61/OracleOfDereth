using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirindiViewService;

namespace OracleOfDereth
{
    public class Cantrip 
    {
        // Collection of Cantrips loaded from cantrips.csv
        public static List<Cantrip> Cantrips = new List<Cantrip>();

        // Properties
        public string Name = "";
        public int SkillId = 0;
        public int SpellId = 0;
        public int Minor = 0;
        public int Moderate = 0;
        public int Major = 0;
        public int Epic = 0;
        public int Legendary = 0;

        public static void Init()
        {
            Cantrips.Clear();
            GearSources.Clear();
            LoadCantripsCSV();
        }

        public static void LoadCantripsCSV()
        {
            var cantrips = new List<Cantrip>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("cantrips.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource cantrips.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: Name,BitMask,LegendaryQuestsFlag,QuestFlag,Url,Hint
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = Util.CsvParseLine(line);

                    cantrips.Add(new Cantrip
                    {
                        Name = fields[0].Trim(),
                        SkillId = int.Parse(fields[1].Trim()),
                        SpellId = int.Parse(fields[2].Trim()),
                        Minor = int.Parse(fields[3].Trim()),
                        Moderate = int.Parse(fields[4].Trim()),
                        Major = int.Parse(fields[5].Trim()),
                        Epic = int.Parse(fields[6].Trim()),
                        Legendary = int.Parse(fields[7].Trim()),
                    });
                }
            }

            Cantrips.AddRange(cantrips);

            //Util.Chat($"Loaded {Cantrips.Count} Cantrips from embedded CSV.", 1);
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        public bool SkillIsKnown()
        {
            if(SkillId <= 0) { return true; }
            return new Skill((CharFilterSkillType)SkillId).IsKnown();
        }

        // True only for rows backed by a real in-game skill. Everything else in cantrips.csv —
        // attributes, wards, sets, essences and the "Blank" spacers — carries 0 or a negative
        // sentinel, which is what keeps the sort below confined to its own section of the list.
        public bool IsSkill()
        {
            return SkillId > 0;
        }

        // Sort rank within the skill section: 0 specialized, 1 trained, 2 untrained, 3 unusable.
        // Non-skills get -1 and are never ranked. Unusable is its own tier rather than being folded
        // in with untrained — it means the skill can't be raised at all, which is a different thing
        // from simply not having spent credits on it.
        public int TrainingRank()
        {
            if (!IsSkill()) { return -1; }

            Skill skill = new Skill((CharFilterSkillType)SkillId);

            if (skill.IsSpecialized()) { return 0; }
            if (skill.IsTrained()) { return 1; }
            if (skill.IsUntrained()) { return 2; }
            return 3;
        }

        // Highest rank TrainingRank can return; the section rebuild walks 0..this.
        private const int MaxTrainingRank = 3;

        // Reorder ONLY the skill-backed stretch of the list — specialized, trained, untrained, then
        // unusable, with a spacer between groups. Rows before the first skill (attributes, armour
        // and the wards) and after the last (essences, sets, the vitals gains) are passed through
        // untouched, in their curated csv order.
        //
        // Within a group the rows are alphabetised. The csv's own weapon/magic/defense grouping
        // can't survive being split across three training tiers anyway, so a name sort is the only
        // ordering left that's predictable to scan.
        //
        // The csv's own spacer between the two skill blocks falls out here: the span is rebuilt from
        // its skill rows alone, so only the separators this method inserts survive. That also means
        // an empty group (no specialized skills, or untrained hidden by the filter) leaves no
        // stray blank behind.
        public static List<Cantrip> SortSkillSection(List<Cantrip> rows)
        {
            int first = rows.FindIndex(x => x.IsSkill());
            if (first < 0) { return rows; }

            int last = rows.FindLastIndex(x => x.IsSkill());

            // Rank once per row rather than per comparison — each call builds a Skill and reads the
            // character filter, and the list is redrawn every tick while the tab is open.
            var ranked = rows.GetRange(first, last - first + 1)
                             .Where(x => x.IsSkill())
                             .Select(x => new { Cantrip = x, Rank = x.TrainingRank() })
                             .ToList();

            var result = new List<Cantrip>();
            result.AddRange(rows.GetRange(0, first));

            bool wroteGroup = false;
            for (int rank = 0; rank <= MaxTrainingRank; rank++)
            {
                // Alphabetised here rather than in the csv. Sorting the file would work — groups are
                // filtered out of the csv sequence, which preserves relative order — but it would
                // rely on the file staying sorted, and appending one row later would break it
                // silently. Sorting at the point of use can't be undone by an edit to the data.
                var group = ranked.Where(x => x.Rank == rank)
                                  .Select(x => x.Cantrip)
                                  .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                                  .ToList();
                if (group.Count == 0) { continue; }

                if (wroteGroup) { result.Add(new Cantrip { Name = "Blank" }); }
                result.AddRange(group);
                wroteGroup = true;
            }

            result.AddRange(rows.GetRange(last + 1, rows.Count - last - 1));

            return result;
        }

        public bool IsSetBonus()
        {
            return (SkillId == -1);
        }
        public bool IsSetDedicationBonus()
        {
            return (SkillId == -2);
        }

        public bool IsEssence()
        {
            return (SkillId == -3);
        }

        public bool IsWarriorsVitality()
        { 
            return (SkillId == -4); 
        }

        public int ActiveTier(ISet<int> activeSpells)
        {
            if (Legendary > 0 && activeSpells.Contains(Legendary)) return 5;
            if (Epic > 0 && activeSpells.Contains(Epic)) return 4;
            if (Major > 0 && activeSpells.Contains(Major)) return 3;
            if (Moderate > 0 && activeSpells.Contains(Moderate)) return 2;
            if (Minor > 0 && activeSpells.Contains(Minor)) return 1;
            return 0;
        }

        private int SpellForTier(int tier) => tier switch
        {
            5 => Legendary, 4 => Epic, 3 => Major, 2 => Moderate, 1 => Minor, _ => 0
        };

        public int Icon()
        {
            FileService service = CoreManager.Current.Filter<FileService>();
            Decal.Filters.Spell spell = service.SpellTable.GetById(SpellId);
            if(spell == null) { return 0; }
            return spell.IconId;
        }

        public string Level(int tier)
        {
            if (IsSetDedicationBonus())
                return tier switch { 5 => "9 pieces", 4 => "8 pieces", 3 => "6 pieces", 2 => "4 pieces", 1 => "2 pieces", _ => "-" };
            if (IsSetBonus())
                return tier switch { 5 => "5 pieces", 4 => "4 pieces", 3 => "3 pieces", 2 => "2 pieces", _ => "-" };
            if (IsEssence())
                return tier switch { 5 => "+30 health", 4 => "+25 health", 3 => "+25 health", 2 => "+20 health", 1 => "+15 health", _ => "-" };
            if (IsWarriorsVitality())
                return tier switch { 5 => "+20 health", 4 => "+15 health", 3 => "+10 health", 2 => "+5 health", _ => "-" };

            string level = tier switch { 5 => "Legendary", 4 => "Epic", 3 => "Major", 2 => "Moderate", 1 => "Minor", _ => "-" };
            return level + GearSuffix(SpellForTier(tier));
        }

        // " (2)" when more than one equipped piece grants this exact cantrip — two items casting
        // Legendary Recklessness at you means one of them is contributing nothing, which is the
        // whole point of showing this. Silent at one source, because that's the normal case and
        // marking every row "(1)" would bury the signal.
        //
        // Counts sources of THIS tier only. A piece granting Epic Recklessness alongside a piece
        // granting Legendary is also redundant, but it doesn't show up here — the row reports the
        // level you actually have, and only the pieces feeding that level.
        private static string GearSuffix(int spellId)
        {
            if (spellId <= 0) { return ""; }

            GearSources.TryGetValue(spellId, out var source);

            return source.Count >= 2 ? $" ({source.Count})" : "";
        }

        public (int Id, string Name) EquippedSource(int tier)
        {
            int spellId = SpellForTier(tier);
            return spellId > 0 && GearSources.TryGetValue(spellId, out var source)
                ? (source.Id, source.Name) : (0, "");
        }

        // Spell id -> count and first equipped source. No live WorldObject references retained.
        private static Dictionary<int, (int Count, int Id, string Name)> GearSources =
            new Dictionary<int, (int Count, int Id, string Name)>();

        // Recount which spells the equipped gear is granting. ONE inventory walk, cached here and
        // reused by every row of a redraw — asking per cantrip would mean ~80 walks a tick.
        //
        // Reads INNATE spells (WorldObject.Spell(i)), not active ones: an item's innate list is what
        // it grants while worn, whereas its active list is what has been cast onto the item. Only
        // the former makes a piece of gear the source of a buff.
        public static void RefreshGearSources()
        {
            var counts = new Dictionary<int, (int Count, int Id, string Name)>();

            if (CoreManager.Current.CharacterFilter.LoginStatus < 1)
            {
                GearSources = counts;
                return;
            }

            using (var inventory = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject item in inventory)
                {
                    // Key 10 is EquippedSlots — nonzero only while worn or wielded, so spares in the
                    // pack don't count. Matches ItemInfo.IsEquipped.
                    if (item.Values((LongValueKey)10, 0) <= 0) { continue; }

                    var spellIds = new HashSet<int>();
                    for (int i = 0; i < item.SpellCount; i++)
                    {
                        spellIds.Add(item.Spell(i));
                    }

                    // Purple Society Band also grants these hidden legendary cantrips,
                    // which are missing from its innate spell list. Deduplicate in case
                    // a server includes them explicitly.
                    if (string.Equals(item.Name, "Purple Society Band", StringComparison.OrdinalIgnoreCase))
                    {
                        spellIds.Add(6063); // Legendary Magic Resistance (Magic Defense)
                        spellIds.Add(6054); // Legendary Impregnability (Missile Defense)
                    }

                    // These rings also grant hidden Legendary Endurance.
                    if (string.Equals(item.Name, "Red Society Band", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item.Name, "Legendary Black Ring", StringComparison.OrdinalIgnoreCase))
                    {
                        spellIds.Add(6104); // Legendary Endurance
                    }

                    string name = null;
                    foreach (int spellId in spellIds)
                    {
                        if (spellId <= 0) { continue; }

                        counts.TryGetValue(spellId, out var source);
                        counts[spellId] = source.Count == 0
                            ? (1, item.Id, name ?? (name = ItemInfo.GetName(item.Name, item.Values(LongValueKey.Material, 0))))
                            : (source.Count + 1, source.Id, source.Name);
                    }
                }
            }

            GearSources = counts;
        }

    }
}

