using Decal.Adapter;
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

            if (TickedOn == DateTime.MinValue) {
                return Duration() - (int)(DateTime.Now - StartedOn).TotalSeconds + 2;
            }

            return Duration() - (int)(DateTime.Now - TickedOn).TotalSeconds;
        }

        public int SecondsSinceCast()
        {
            if (CastOn == DateTime.MinValue) { return -1; }
            return (int)(DateTime.Now - CastOn).TotalSeconds;
        }

        // Client property id for the "Archmage's Endurance" augmentation (+20% spell
        // duration each, max 5). Matches AugmentationIncreasedSpellDuration server-side
        // (PropertyInt 238, [SendOnLogin] so the client receives it).
        private const int ArchmagesEndurancePropId = 238;

        public int Duration()
        {
            double duration;

            if (Spell.CorrosionSpellIds.Contains(SpellId)) { duration = 15; }
            else if (Spell.CorruptionSpellIds.Contains(SpellId)) { duration = 30; }
            else if (Spell.CurseSpellIds.Contains(SpellId)) { duration = 30; }
            else { duration = Spell.GetSpell(SpellId).Duration; }

            // Conquest scales void DoT duration by the caster's spell-duration augments:
            //   duration *= 1 + (Archmage's Endurance * 0.2) + (Lum spell duration * 0.05)
            // Archmage's Endurance is a normal client property; the luminance count isn't
            // networked to the client, so it's scraped from "/augs" (see SpellDurationAug).
            if (Server.IsConquest)
            {
                int arch = CoreManager.Current.CharacterFilter.GetCharProperty(ArchmagesEndurancePropId);
                int lum = ConquestAugmentation.DurationCount;

                duration *= 1.0 + (arch * 0.2) + (lum * 0.05);
            }

            return (int)duration;
        }
    }
}
