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

namespace OracleOfDereth
{
    public class Target 
    {
        // Collection of Targets that have had spells cast on them
        public static Dictionary<int, Target> Targets = new Dictionary<int, Target>();
        public static int CurrentTargetId = 0;

        // Properties
        public int Id = 0;
        public Dictionary<int, DateTime> ActiveSpells = new Dictionary<int, DateTime>();

        public static void Init()
        {
            Targets.Clear();
            CurrentTargetId = 0;
        }

        public static void SetCurrentTarget(int id)
        {
            CurrentTargetId = id;
        }

        public static Target? GetCurrentTarget()
        {
            if (CurrentTargetId == 0) { return null; }

            // Find or create target
            Target.Targets.TryGetValue(CurrentTargetId, out Target target);

            if (target == null) {
                target = new Target() { Id = CurrentTargetId };
            }

            return target;
        }

        public static void SpellCast(int id, int spellId)
        {
            // Return false unless SpellId.VoidSpellIds include this spellId
            if(!SpellId.VoidSpellIds.Contains(spellId)) { return; }

            // Find or create target
            Target.Targets.TryGetValue(id, out Target target);

            if(target == null) { 
                target = new Target() { Id = id }; 

                // Save Target
                Targets[id] = target;
            }

            target.ActiveSpells[spellId] = DateTime.Now;

            //Util.Chat($"Active spells are now: {string.Join(", ", target.ActiveSpells.Keys)}", 1);
            //Util.Chat($"Active spells are now: {string.Join(", ", target.ActiveSpells.Values)}", 1);
        }

        // Instance methods

        public new string ToString()
        {
            return $"{Name()}";
        }

        public WorldObject Item()
        {
            return CoreManager.Current.WorldFilter[Id];
        }
        public string Name()
        {
            return Item().Name;
        }
    }
}

