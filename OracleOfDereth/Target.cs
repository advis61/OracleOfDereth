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

namespace OracleOfDereth
{
    public class Target
    {
        public static readonly Regex YouCastRegex = new Regex(@"^You cast (.+?) on (.+?)(?:,.*)?$");

        // Collection of Targets that have had spells cast on them
        public static Dictionary<int, Target> Targets = new Dictionary<int, Target>();

        // My current target
        public static int CurrentTargetId = 0;

        // Spells tracking
        public static List<TargetSpell> TargetSpells = new List<TargetSpell>();

        // Properties
        public int Id = 0;
        //public Dictionary<int, DateTime> ActiveSpells = new Dictionary<int, DateTime>();

        public static void Init()
        {
            Targets.Clear();
            TargetSpells.Clear();
            CurrentTargetId = 0;
        }

        public static void SetCurrentTarget(int id)
        {
            CurrentTargetId = id;
            Util.Chat($"Targeting {GetCurrentTarget().ToString()}", Util.ColorOrange);
        }

        public static Target? GetCurrentTarget()
        {
            if (CurrentTargetId == 0) { return null; }

            // Find or create target
            Target.Targets.TryGetValue(CurrentTargetId, out Target target);

            if (target == null)
            {
                target = new Target() { Id = CurrentTargetId };
            }

            return target;
        }

        //public static void SpellCastStarted(int id, int spellId)
        //{
        //    if (!Spell.VoidSpellIds.Contains(spellId)) { return; }

        //    // Find or create target
        //    Target.Targets.TryGetValue(id, out Target target);

        //    if (target == null)
        //    {
        //        target = new Target() { Id = id };

        //        // Save Target
        //        Targets[id] = target;
        //    }

        //    target.ActiveSpells[spellId] = DateTime.Now;

        //    //Util.Chat($"Active spells are now: {string.Join(", ", target.ActiveSpells.Keys)}", 1);
        //    //Util.Chat($"Active spells are now: {string.Join(", ", target.ActiveSpells.Values)}", 1);
        //}

        public static void SpellCast(int id, int spellId)
        {
            if (!Spell.VoidSpellIds.Contains(spellId)) { return; }

            // Find or create target
            Target.Targets.TryGetValue(id, out Target target);

            if (target == null)
            {
                target = new Target() { Id = id };

                // Save Target
                Targets[id] = target;
            }

            TargetSpell targetSpell = new TargetSpell()
            {
                TargetId = id,
                TargetName = target.Name(),
                SpellId = spellId,
                spellName = Spell.GetSpellName(spellId),
                CastOn = DateTime.Now
            };

            TargetSpells.Insert(0, targetSpell);
        }

        public static void SpellStarted(string text)
        {
            Match match = YouCastRegex.Match(text);
            if(!match.Success) { return; }

            string spell = match.Groups[1].Value;
            string target = match.Groups[2].Value;

            Util.Chat($"You cast '{spell}' on '{target}' bro.", 1);
        }

        // Instance methods

        public new string ToString()
        {
            return $"{Name()} {ObjectClass()}";
        }

        public WorldObject Item()
        {
            return CoreManager.Current.WorldFilter[Id];
        }
        public string Name()
        {
            return Item().Name;
        }

        public bool IsMob() {
            return ObjectClass() == "Monster";
        }

        public string ObjectClass()
        {
            return Item().ObjectClass.ToString();
        }

        public string CorrosionText() { return GetSpellText(Spell.CorrosionSpellId); }
        public string CorruptionText() { return GetSpellText(Spell.CorruptionSpellId); }
        public string CurseText() { return GetSpellText(Spell.CurseSpellId); }

        private int GetSpellDuration(int spellId)
        {
            if (spellId == Spell.CorrosionSpellId) { return 15; }
            if (spellId == Spell.CorruptionSpellId) { return 15; }
            if (spellId == Spell.CurseSpellId) { return 30; }
            return 0;
        }

        private string GetSpellText(int spellId)
        {
            Target target = GetCurrentTarget();
            if (target == null) { return ""; }

            Target.GetCurrentTarget().ActiveSpells.TryGetValue(spellId, out DateTime spellTime);
            if (spellTime == null || spellTime == DateTime.MinValue) { return ""; }

            int seconds = GetSpellDuration(spellId) - (int)(DateTime.Now - spellTime).TotalSeconds;
            if(seconds < 0) { return ""; }

            return seconds.ToString();
        }
    }
}

