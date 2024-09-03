using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
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
        public bool IsSpecialized()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Specialized;
        }

        public bool IsTrained()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Trained;
        }

        public int Current()
        {
            int value = CoreManager.Current.CharacterFilter.EffectiveSkill[SkillType];

            // Jack of All Trades
            value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.JackOfAllTrades) * 5;

            // Worlds
            value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.AuraWorld);

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

            // Vitae
            if(Vitae() > 0)
            {
                double percentage = 1 - (Vitae() / 100.0);
                value = (int)Math.Round(value * percentage);
            }

            return (value);

        }
        public int Vitae()
        {
            return CoreManager.Current.CharacterFilter.Vitae;
        }
    }
}
