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

        // Summoning
        // https://gitlab.com/utilitybelt/utilitybelt.scripting/-/blob/master/Interop/Skill.cs?ref_type=heads#L106
        //string stam = CoreManager.Current.CharacterFilter.Vitals[CharFilterVitalType.Stamina].Base.ToString();

        //string summoning = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.Summoning].ToString();
        //string summoning = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Buffed.ToString();
        //string summoning = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Current.ToString();
        //string summoning = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.Summoning].ToString();

        // "413"
        //string summoning = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Base.ToString();

        // "Specialized"
        //string training = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Training.ToString();

        // "10" - Not sure where this 10 is coming from
        //string bonus = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Bonus.ToString();

        // "546"
        //string buffed = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Buffed.ToString();

        // "546"
        //string effective = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.Summoning].ToString();

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
            if(CoreManager.Current.CharacterFilter.Enchantments.Count(x => (x.SpellId == 5753)) > 0)
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
