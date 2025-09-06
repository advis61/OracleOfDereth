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

namespace OracleOfDereth
{
    public class Target
    {
        public static readonly Regex YouCastRegex = new Regex(@"^You cast (.+?) on (.+?)(?:,.*)?$");

        // My current target
        public static int CurrentTargetId = 0;

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
            if (!Spell.VoidSpellIds.Contains(spellId)) { return; }

            Target target = new() { Id = id };

            TargetSpell targetSpell = new()
            {
                TargetId = target.Id,
                TargetName = target.Name(),
                SpellId = spellId,
                spellName = Spell.GetSpellName(spellId),
                CastOn = DateTime.Now
            };

            TargetSpells.Insert(0, targetSpell);
            // Util.Chat($"You casting '{spellId}' on '{id}'");
        }

        public static void SpellStarted(string text)
        {
            Match match = YouCastRegex.Match(text);
            if(!match.Success) { return; }

            string spellName = match.Groups[1].Value;
            string targetName = match.Groups[2].Value;

            TargetSpell targetSpell = TargetSpells.Where(s => s.TargetName == targetName && s.spellName == spellName && !s.IsStarted() && !s.IsCasting()).FirstOrDefault();

            if(targetSpell == null) {
                targetSpell = TargetSpells.Where(s => s.TargetName == targetName && s.spellName == spellName && !s.IsStarted()).FirstOrDefault();
            }

            if (targetSpell == null) { return; }

            targetSpell.SetStarted();
            //Util.Chat($"You cast '{spellName}' on '{targetName}'", 1);
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

        public string CorrosionText() { return GetSpellText(Spell.CorrosionSpellId); }
        public string CorruptionText() { return GetSpellText(Spell.CorruptionSpellId); }
        public string CurseText() { return GetSpellText(Spell.CurseSpellId); }

        private string GetSpellText(int spellId)
        {
            if (Id == 0) return "";

            TargetSpell targetSpell = TargetSpells.Where(s => s.TargetId == Id && s.SpellId == spellId && s.IsActive()).FirstOrDefault();
            if(targetSpell == null) { return ""; }

            int seconds = targetSpell.SecondsRemaining();
            if(seconds < 0) { return ""; }

            return seconds.ToString();
        }
    }
}

