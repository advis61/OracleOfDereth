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
        public bool IsUntrained()
        {
            return CoreManager.Current.CharacterFilter.Skills[SkillType].Training == TrainingType.Untrained;
        }

            //public virtual int Current
            //{
            //    get
            //    {
            //        // logic from ACE
            //        var effectiveBase = (int)(InitLevel + PointsRaised);
            //        if (Training > SkillTrainingType.Unusable && Formula.UseFormula)
            //        {
            //            var attrBonus = _weenie.Attributes[Formula.Attribute1].Current;
            //            if (Formula.Attribute2 != 0)
            //            {
            //                attrBonus += _weenie.Attributes[Formula.Attribute2].Current;
            //            }

            //            effectiveBase += (int)Math.Round(((float)attrBonus / Formula.Divisor));
            //        }

            //        effectiveBase += _weenie.Value(IntId.LumAugAllSkills);

            //        if (MeleeSkills.Contains(Type))
            //            effectiveBase += _weenie.Value(IntId.AugmentationSkilledMelee) * 10;
            //        else if (MissileSkills.Contains(Type))
            //            effectiveBase += _weenie.Value(IntId.AugmentationSkilledMissile) * 10;
            //        else if (MagicSkills.Contains(Type))
            //            effectiveBase += _weenie.Value(IntId.AugmentationSkilledMagic) * 10;

            //        var multiplier = _characterState.GetEnchantmentsMultiplierModifier(Type);
            //        var fTotal = effectiveBase * multiplier;

            //        if (_characterState.Vitae < 1.0f)
            //        {
            //            fTotal *= _characterState.Vitae;
            //        }

            //        fTotal += _weenie.Value(IntId.AugmentationJackOfAllTrades) * 5;

            //        if (Training == SkillTrainingType.Specialized)
            //            fTotal += _weenie.Value(IntId.LumAugSkilledSpec) * 2;

            //        var additives = _characterState.GetEnchantmentsAdditiveModifier(Type);
            //        return (int)Math.Max(Math.Round(fTotal + additives), 0);
            //    }
            //}

        public int Current()
        {
            int value = CoreManager.Current.CharacterFilter.EffectiveSkill[SkillType];

            // TODO: Melee / Missile / Magic augs

            // Worlds
            value += CoreManager.Current.CharacterFilter.GetCharProperty((int)Augmentations.AuraWorld);

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

            // Vitae
            // value -= VitaeMissing();

            return (value);
        }
        public int Vitae()
        {
            return CoreManager.Current.CharacterFilter.Vitae;
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
