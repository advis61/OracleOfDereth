using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public static class Hud
    {
        public static string BuffsText()
        {
            FileService service = CoreManager.Current.Filter<FileService>();
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments
                .Where(x => x.Duration > 900)
                .Where(x => x.TimeRemaining > 0)
                .Where(x => !SpellId.BeerSpellIds.Contains(x.SpellId))
                .Where(x =>
                {
                    var spell = service.SpellTable.GetById(x.SpellId);
                    return spell != null && !spell.IsDebuff && spell.IsUntargetted;
                })
                .ToList();

            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            return string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
        }

        public static string BuffNowText()
        {
            string text = BuffsText();
            if(text == "-") { return "No buffs present"; }

            return $"{text} remaining on buffs";
        }

        public static string HouseText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments
                .Where(x => SpellId.HouseSpellIds.Contains(x.SpellId))
                .Where(x => x.TimeRemaining > 0)
                .ToList();

            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            return string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
        }

        public static string BeersText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => SpellId.BeerSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        public static string PagesText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => SpellId.PagesSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        public static string LockpickText()
        {
            Skill skill = new Skill(CharFilterSkillType.Lockpick);
            if(skill.IsUnKnown()) { return "-"; }

            int value = skill.Current();
            int vr1 = 0;
            int vr2 = 0;

            if (value >= 575) {
                vr1 = 3;
                vr2 = 10;
            } else if (value >= 570) {
                vr1 = 4;
                vr2 = 11;
            } else if (value >= 565) {
                vr1 = 5;
                vr2 = 12;
            } else if (value >= 550) { 
                vr1 = 6;
                vr2 = 13;
            } else if (value >= 525) {
                vr1 = 6;
                vr2 = 14;
            } else if (value >= 500) {
                vr1 = 7;
                vr2 = 15;
            } else {
                vr1 = 10;
                vr2 = 20;
            }

            if (value >= 500) {
                return skill.Current().ToString() + " (VR " + vr1 + "/" + vr2 + ")";
            } else {
                return skill.Current().ToString();
            }
        }
        public static string SummoningText()
        {
            Skill skill = new Skill(CharFilterSkillType.Summoning);
            if(skill.IsUnKnown()) { return "-"; }

            return skill.Current().ToString();
        }

        public static string LifeText()
        {
            Skill skill = new Skill(CharFilterSkillType.LifeMagic);
            if(skill.IsUnKnown()) { return "-"; }
            
            return skill.Current().ToString();
        }

        public static string MeleeDText()
        {
            Skill skill = new Skill(CharFilterSkillType.MeleeDefense);
            if(skill.IsUnKnown()) { return "-"; }
            
            return skill.Current().ToString();
        }
        public static string RareText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => SpellId.RareSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            double cooldown = 180 - (900 - enchantments.Max(x => x.TimeRemaining));

            if (cooldown > 0) {
                return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ") " + cooldown.ToString() + "s";
            } else {
                return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
            }
        }

        public static string DestructionText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => SpellId.DestructionSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);
            if(time.Seconds < 0) { return "-"; }

            return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        public static string RegenText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => SpellId.RegenSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);
            if(time.Seconds < 0) { return "-"; }

            return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        public static string ProtectionText()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => SpellId.ProtectionSpellIds.Contains(x.SpellId)).ToList();
            if (enchantments.Count == 0) { return "-"; }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);
            if(time.Seconds < 0) { return "-"; }

            return string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }
    }
}
