using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public class TargetSpell
    {
        public int SpellId = 0;
        public string spellName = "";

        public int TargetId = 0;
        public string TargetName = "";

        public DateTime CastOn = DateTime.MinValue;        // Got the SpellCast event
        public DateTime StartedOn = DateTime.MinValue;     // Got the Chat event

        public int SecondsRemaining()
        {
            if(StartedOn == DateTime.MinValue) { return -1; }
            return Duration() - (int)(DateTime.Now - StartedOn).TotalSeconds;
        }

        public int SecondsSinceCast()
        {
            if (CastOn == DateTime.MinValue) { return -1; }
            return (int)(DateTime.Now - CastOn).TotalSeconds;
        }

        public int Duration()
        {
            if (SpellId == Spell.CorrosionSpellId) { return 15; }
            if (SpellId == Spell.CorruptionSpellId) { return 15; }
            if (SpellId == Spell.CurseSpellId) { return 30; }
            return 0;
        }

    }
}
