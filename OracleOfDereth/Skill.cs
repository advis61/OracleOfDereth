using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    internal class Skill
    {
        public CharFilterSkillType SkillType { get; private set; }

        public Skill(CharFilterSkillType skillType)
        {
            SkillType = skillType;
        }

        public bool IsKnown()
        {
            return IsSpecialized() || IsTrained();
        }

        public bool IsSpecialized()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Specialized;
        }

        public bool IsTrained()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Trained;
        }
        public bool IsUntrained()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Untrained;
        }

        public bool IsUnusable()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Unusable;
        }

        public int Current()
        {
            int value = CoreManager.Current.CharacterFilter.EffectiveSkill[SkillType];

            // TODO: Melee / Missile / Magic augs
            if (SkillType == CharFilterSkillType.LifeMagic)
            {
                value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.MasterFiveFoldPath) * 10;
            }

            // Worlds
            value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.AuraWorld);

            // Vitae - This isn't quite correct but it's close enough
            int vitae = CoreManager.Current.CharacterFilter.Vitae;

            if (vitae > 0)
            {
                value -= (int)Math.Round(value * (vitae / 100.0f)) - ((int)Math.Ceiling(vitae / 2.0f) + 1);
            }

            // Jack of All Trades
            value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.JackOfAllTrades) * 5;

            // Aura of Specialization
            if (IsSpecialized())
            {
                value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.AuraSpecialization) * 2;
            }

            // Enlightens
            if (IsTrained() || IsSpecialized())
            {
                value += CoreManager.Current.CharacterFilter.GetCharProperty(390);
            }

            // CiS
            if (CoreManager.Current.CharacterFilter.Enchantments.Count(x => (x.SpellId == 5753)) > 0)
            {
                value += 20;
            }

            return (value);
        }

        // No Vitae:
        // Life 536
        // MeleeD 563
        // Summon 576

        // 5% Vitae
        // Life 514
        // MeleeD 540 (
        // Summoning 552 (24 less. ingame 21)

        // CIS 5753

        public int Vitae()
        {
            return CoreManager.Current.CharacterFilter.Vitae;
        }

        public int VitaeValue()
        {
            return (int)Math.Round(CoreManager.Current.CharacterFilter.Vitae / 100.0f);
        }

        public int VitaeMissing()
        {
            if (Vitae() > 0)
            {
                int value = CoreManager.Current.CharacterFilter.Skills[SkillType].Base;
                value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.AuraWorld);

                return (int)Math.Ceiling(value * (Vitae() / 100.0f));
            }

            return 0;
        }
    }
}
