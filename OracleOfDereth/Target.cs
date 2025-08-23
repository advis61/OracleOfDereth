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
        public static Target CurrentTarget = null;

        // Properties
        public int Id = 0;
        public Dictionary<int, DateTime> ActiveSpells = new Dictionary<int, DateTime>();

        public static void Init()
        {
            Targets.Clear();
            CurrentTarget = null;
        }

        public static void SetCurrentTarget(int id)
        {
            Target.Targets.TryGetValue(id, out Target target);
            if(target == null) { target = new Target() { Id = id }; }

            CurrentTarget = target;
        }

        public static void SpellCast(int id, int spellId)
        {
            // Find or create target
            Target.Targets.TryGetValue(id, out Target target);

            if(target == null) { 
                target = new Target() { Id = id }; 

                // Save Targets
                Targets[id] = target;
            }

            // Find or create spell
            target.ActiveSpells.TryGetValue(spellId, out DateTime spellTime);

            if(spellTime == null) {
                spellTime = DateTime.Now;
                target.ActiveSpells[spellId] = spellTime;
            } else if(DateTime.Now - spellTime < TimeSpan.FromSeconds(1)) {
                return; // Don't update if the spell was cast less than 1 second ago
            } else {
                target.ActiveSpells[spellId] = spellTime;
            }

            Util.Chat($"Active spells are now: {string.Join(", ", target.ActiveSpells.Keys)}", 1);
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

