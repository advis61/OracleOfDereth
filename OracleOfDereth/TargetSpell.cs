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
        public string SpellName = "";

        public int TargetId = 0;
        public string TargetName = "";

        public bool Destruction = false;

        public DateTime CastOn = DateTime.MinValue;        // Got the SpellCast event
        public DateTime StartedOn = DateTime.MinValue;     // Got the Chat event
        public DateTime TickedOn = DateTime.MinValue;      // Got the period damage chat event

        public void SetStarted()
        {
            StartedOn = DateTime.Now;
        }
        public void SetTicked()
        {
            TickedOn = DateTime.Now;
        }
        public void SetDestruction()
        {
            Destruction = true;
        }

        public bool IsCasting()
        {
            return (!IsStarted() && SecondsSinceCast() < 3);
        }

        public bool IsStarted()
        {
            return StartedOn != DateTime.MinValue;
        }

        public bool IsTicked()
        {
            return TickedOn != DateTime.MinValue;
        }

        public bool IsActive()
        {
            return SecondsRemaining() > 0;
        }

        public bool IsExpired()
        {
            if(IsStarted() && !IsActive()) { return true; }
            if(!IsStarted() && SecondsSinceCast() > 6) { return true; }
            return false;
        }

        public int SecondsRemaining()
        {
            if(StartedOn == DateTime.MinValue) { return -1; }

            if (TickedOn == DateTime.MinValue)
            {
                return Duration() - (int)(DateTime.Now - StartedOn).TotalSeconds + 2;
            }

            return Duration() - (int)(DateTime.Now - TickedOn).TotalSeconds;
        }

        public int SecondsSinceCast()
        {
            if (CastOn == DateTime.MinValue) { return -1; }
            return (int)(DateTime.Now - CastOn).TotalSeconds;
        }

        public int Duration()
        {
            if(Spell.CorrosionSpellIds.Contains(SpellId)) { return 15; }
            if(Spell.CorruptionSpellIds.Contains(SpellId)) { return 30; }
            if(Spell.CurseSpellIds.Contains(SpellId)) { return 30; }

            return (int)Spell.GetSpell(SpellId).Duration;
        }
    }
}
