using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirindiViewService.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Drawing;

namespace OracleOfDereth
{
    public class Target
    {
        public static Color DestructionColor = Color.Gold;
        public static readonly Regex YouCastRegex = new Regex(@"^You cast (.+?) on (.+?)(?:,.*)?$");
        public static readonly Regex PeriodicNetherRegex = new Regex(@"^You scar (.+?) for (\d+) points of periodic nether damage.*$");

        // Target Spells I care to track
        private static readonly List<int> TargetSpellIds = new List<int> {}
            .Concat(Spell.CorrosionSpellIds)
            .Concat(Spell.CorruptionSpellIds)
            .Concat(Spell.CurseSpellIds)
            .ToList();

        // My current target
        public static int CurrentTargetId = 0;

        // Last time I got a Destruction proc
        public static DateTime CurrentDestructionProc = DateTime.MinValue;

        // Spells tracking
        public static List<TargetSpell> TargetSpells = new List<TargetSpell>();

        // Instance properties
        public int Id = 0;

        public static void Init()
        {
            TargetSpells.Clear();
            CurrentTargetId = 0;
        }

        public static void SetCurrentTarget(int id)
        {
            CurrentTargetId = id;

            //Util.Chat($"Targeting {GetCurrentTarget().ToString()}");
        }

        public static Target GetCurrentTarget()
        {
            return new() { Id = CurrentTargetId };
        }

        public static void RemoveAllExpired()
        {
            TargetSpells.RemoveAll(s => s.IsExpired());
        }

        public static void SpellCast(int id, int spellId)
        {
            if(TargetSpellIds.Contains(spellId) == false) { return; }

            Target target = new() { Id = id };

            TargetSpell targetSpell = new()
            {
                TargetId = target.Id,
                TargetName = target.Name(),
                SpellId = spellId,
                SpellName = Spell.GetSpellName(spellId),
                CastOn = DateTime.Now,
            };

            TargetSpells.Insert(0, targetSpell);
        }

        public static void SpellStarted(string text)
        {
            Match match = YouCastRegex.Match(text);
            if(!match.Success) { return; }

            string spellName = match.Groups[1].Value;
            string targetName = match.Groups[2].Value;

            // Find the correct previous cast
            TargetSpell targetSpell = TargetSpells.Where(s => s.TargetName == targetName && s.SpellName == spellName && !s.IsStarted() && !s.IsCasting()).FirstOrDefault();

            if(targetSpell == null) {
                targetSpell = TargetSpells.Where(s => s.TargetName == targetName && s.SpellName == spellName && !s.IsStarted()).FirstOrDefault();
            }

            if (targetSpell == null) { return; }

            // Determine if this is a destruction boosted spell or not
            Target target = new() { Id = targetSpell.TargetId };

            bool destruction = (target.DestructionText() != "");
            bool existingSpell = (target.GetSpellText(new List<int> { targetSpell.SpellId }) != "");
            bool existingDestruction = (target.GetSpellColor(new List<int> { targetSpell.SpellId }) == DestructionColor);

            if (existingDestruction || (destruction && !existingSpell)) { targetSpell.SetDestruction(); }

            targetSpell.SetStarted();

            //Util.Chat($"You cast '{spellName}' on '{targetName}' with '{targetSpell.Destruction}'");
        }

        public static void SpellTicked(string text)
        {
            Match match = PeriodicNetherRegex.Match(text);
            if(!match.Success) { return; }

            string targetName = match.Groups[1].Value;

            var targetSpells = TargetSpells.Where(s => s.TargetName == targetName && s.IsStarted() && !s.IsTicked());
            foreach(var targetSpell in targetSpells) { targetSpell.SetTicked(); }
        }

        // Instance methods
        public string Name()
        {
            if (Item() == null) return "";
            return Item().Name;
        }

        public bool IsMob()
        {
            if (Item() == null) return false;
            return ObjectClass() == "Monster";
        }

        private WorldObject? Item()
        {
            if(Id == 0) { return null; }

            try {
                return CoreManager.Current.WorldFilter[Id];
            } catch { 
                return null; 
            }
        }

        private string ObjectClass()
        {
            if (Item() == null) return "";
            return Item().ObjectClass.ToString();
        }
        public string DestructionText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => Spell.DestructionSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return ""; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            int seconds = time.Seconds;
            if (seconds < 0) { return ""; }

            return time.Seconds.ToString();
        }

        public string CorrosionText() { return GetSpellText(Spell.CorrosionSpellIds); }
        public string CorruptionText() { return GetSpellText(Spell.CorruptionSpellIds); }
        public string CurseText() { return GetSpellText(Spell.CurseSpellIds); }

        private string GetSpellText(List<int> spellIds)
        {
            if (Item() == null) return "";

            TargetSpell targetSpell = TargetSpells.Where(s => s.TargetId == Id && spellIds.Contains(s.SpellId) && s.IsActive()).FirstOrDefault();
            if(targetSpell == null) { return ""; }

            int seconds = targetSpell.SecondsRemaining();
            if(seconds < 0) { return ""; }

            return seconds.ToString();
        }

        public Color CorrosionColor() { return GetSpellColor(Spell.CorrosionSpellIds); }
        public Color CorruptionColor() { return GetSpellColor(Spell.CorruptionSpellIds); }
        public Color CurseColor() { return GetSpellColor(Spell.CurseSpellIds); }

        private Color GetSpellColor(List<int> spellIds)
        {
            if(Item() == null) { return Color.White; }

            TargetSpell targetSpell = TargetSpells.Where(s => s.TargetId == Id && spellIds.Contains(s.SpellId) && s.IsActive()).FirstOrDefault();
            if (targetSpell == null) { return Color.White; }

            if (targetSpell.Destruction) { return DestructionColor; }
            return Color.White;
        }
    }
}

