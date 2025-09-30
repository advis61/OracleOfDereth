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
        private readonly CharacterFilter CharacterFilter;

        public Skill(CharFilterSkillType skillType)
        {
            SkillType = skillType;
            CharacterFilter = CoreManager.Current.CharacterFilter;
        }

        public bool IsKnown()
        {
            return IsSpecialized() || IsTrained();
        }

        public bool IsUnKnown()
        {
            return !IsKnown();
        }

        public bool IsSpecialized()
        {
            return CharacterFilter.Skills[SkillType].Training == TrainingType.Specialized;
        }

        public bool IsTrained()
        {
            return CharacterFilter.Skills[SkillType].Training == TrainingType.Trained;
        }
        public bool IsUntrained()
        {
            return CharacterFilter.Skills[SkillType].Training == TrainingType.Untrained;
        }

        public bool IsUnusable()
        {
            return CharacterFilter.Skills[SkillType].Training == TrainingType.Unusable;
        }

        public bool IsCloakedInSkill()
        {
            return CharacterFilter.Enchantments.Count(x => (x.SpellId == 5753)) > 0;
        }

        public int Current()
        {
            int value = CharacterFilter.EffectiveSkill[SkillType];

            // TODO: Melee / Missile / Magic augs
            if (SkillType == CharFilterSkillType.LifeMagic) {
                value += CharacterFilter.GetCharProperty((int)Augmentations.MasterFiveFoldPath) * 10;
            }

            // Worlds
            value += CharacterFilter.GetCharProperty((int)Augmentations.AuraWorld);

            // Vitae - This isn't quite correct but it's close enough
            int vitae = CharacterFilter.Vitae;

            if (vitae > 0) {
                value -= (int)Math.Round(value * (vitae / 100.0f)) - ((int)Math.Ceiling(vitae / 2.0f) + 1);
            }

            // Jack of All Trades
            value += CharacterFilter.GetCharProperty((int)Augmentations.JackOfAllTrades) * 5;

            // Aura of Specialization
            if (IsSpecialized()) {
                value += CharacterFilter.GetCharProperty((int)Augmentations.AuraSpecialization) * 2;
            }

            // Enlightens
            if (IsTrained() || IsSpecialized()) {
                value += CharacterFilter.GetCharProperty(390);
            }

            // CiS
            if (IsCloakedInSkill()) {
                value += 20;
            }

            return value;
        }
    }
}
