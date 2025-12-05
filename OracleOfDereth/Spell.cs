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
using VirindiViewService;

namespace OracleOfDereth
{
    public class Spell
    {
        public static readonly List<int> RareSpellIds = new List<int> {
            3679,  // Prodigal Acid Bane
            3680,  // Prodigal Acid Protection
            3681,  // Prodigal Alchemy Mastery
            3682,  // Prodigal Arcane Enlightenment
            3683,  // Prodigal Armor Expertise
            3684,  // Prodigal Armor
            3685,  // Prodigal Light Weapon Mastery
            3686,  // Prodigal Blade Bane
            3687,  // Prodigal Blade Protection
            3688,  // Prodigal Blood Drinker
            3689,  // Prodigal Bludgeon Bane
            3690,  // Prodigal Bludgeon Protection
            3691,  // Prodigal Missile Weapon Mastery
            3692,  // Prodigal Cold Protection
            3693,  // Prodigal Cooking Mastery
            3694,  // Prodigal Coordination
            3695,  // Prodigal Creature Enchantment Mastery
            3696,  // Prodigal Missile Weapon Mastery
            3697,  // Prodigal Finesse Weapon Mastery
            3698,  // Prodigal Deception Mastery
            3699,  // Prodigal Defender
            3700,  // Prodigal Endurance
            3701,  // Prodigal Fealty
            3702,  // Prodigal Fire Protection
            3703,  // Prodigal Flame Bane
            3704,  // Prodigal Fletching Mastery
            3705,  // Prodigal Focus
            3706,  // Prodigal Frost Bane
            3707,  // Prodigal Healing Mastery
            3708,  // Prodigal Heart Seeker
            3709,  // Prodigal Hermetic Link
            3710,  // Prodigal Impenetrability
            3711,  // Prodigal Impregnability
            3712,  // Prodigal Invulnerability
            3713,  // Prodigal Item Enchantment Mastery
            3714,  // Prodigal Item Expertise
            3715,  // Prodigal Jumping Mastery
            3716,  // Prodigal Leadership Mastery
            3717,  // Prodigal Life Magic Mastery
            3718,  // Prodigal Lightning Bane
            3719,  // Prodigal Lightning Protection
            3720,  // Prodigal Lockpick Mastery
            3721,  // Prodigal Light Weapon Mastery
            3722,  // Prodigal Magic Item Expertise
            3723,  // Prodigal Magic Resistance
            3732,  // Prodigal Mana Conversion Mastery
            3725,  // Prodigal Mana Renewal
            3726,  // Prodigal Monster Attunement
            3727,  // Prodigal Person Attunement
            3728,  // Prodigal Piercing Bane
            3729,  // Prodigal Piercing Protection
            3730,  // Prodigal Quickness
            3731,  // Prodigal Regeneration
            3732,  // Prodigal Rejuvenation
            3733,  // Prodigal Willpower
            3734,  // Prodigal Light Weapon Mastery
            3735,  // Prodigal Spirit Drinker
            3736,  // Prodigal Sprint
            3737,  // Prodigal Light Weapon Mastery
            3738,  // Prodigal Strength
            3739,  // Prodigal Swift Killer
            3740,  // Prodigal Heavy Weapon Mastery
            3741,  // Prodigal Missile Weapon Mastery
            3742,  // Prodigal Light Weapon Mastery
            3743,  // Prodigal War Magic Mastery
            3744,  // Prodigal Weapon Expertise
            5025,  // Prodigal Item Expertise
            5026,  // Prodigal Two Handed Combat Mastery
            5436,  // Prodigal Void Magic Mastery
            5903,  // Prodigal Dual Wield Mastery
            5905,  // Prodigal Recklessness Mastery
            5907,  // Prodigal Shield Mastery
            5909,  // Prodigal Sneak Attack Mastery
            5911,  // Prodigal Dirty Fighting Mastery
            4131, // Spectral Light Weapon Mastery
            4132, // Spectral Blood Drinker
            4133, // Spectral Missile Weapon Mastery
            4134, // Spectral Missile Weapon Mastery
            4135, // Spectral Finesse Weapon Mastery
            4136, // Spectral Light Weapon Mastery
            4137, // Spectral Light Weapon Mastery
            4138, // Spectral Light Weapon Mastery
            4139, // Spectral Heavy Weapon Mastery
            4140, // Spectral Missile Weapon Mastery
            4141, // Spectral Light Weapon Mastery
            4142, // Spectral War Magic Mastery
            4208, // Spectral Flame
            4221, // Spectral Life Magic Mastery
            5023, // Spectral Two Handed Combat Mastery
            5032, // Spectral Item Expertise
            5168, // a spectacular view of the Mhoire lands
            5169, // a descent into the Mhoire catacombs
            5170, // a descent into the Mhoire catacombs
            5171, // Spectral Fountain Sip (Feeling good)
            5172, // Spectral Fountain Sip (Blood poisoned)
            5173, // Spectral Fountain Sip (Wounds poisoned)
            5435, // Spectral Void Magic Mastery
            5904, // Spectral Dual Wield Mastery
            5906, // Spectral Recklessness Mastery
            5908, // Spectral Shield Mastery
            5910, // Spectral Sneak Attack Mastery
            5912, // Spectral Dirty Fighting Mastery
        };

        public static readonly List<int> HouseSpellIds = new List<int>
        {
            3896, // Dark Equilibrium
            3894, // Dark Persistence
            3897, // Dark Purpose
            3895, // Dark Reflexes
            6146, // Ride the Lightning
            4099, // Strength of Diemos
            4025, // Iron Cast Stomach
            3323, // Consencration
            3324, // Divine Manipulation
            3325, // Sacrosanct Touch
            3237, // Fanaticism
            3831, // Blessing of the Pitcher Plant
            3830, // Blessing of the Fly Trap
            2995, // Power of the Dragon
            2993, // Grace of the Unicorn
            2997, // Splendor of the Firebird
            3829, // Blessing of the Sundew
            3977, // Coordination Other Incantation
            3978, // Focus Other Incantation
            3979, // Strength Other Incantation
        };

        public static readonly List<int> NotBuffSpellIds = new List<int> {
            3204, // Blazing Heart
            5127, // Answer of Loyalty (Mana)
            5131, // Answer of Loyalty (Stam)
            5132, // Answer of Loyalty (Stam)
            5978, // Rare Armor Damage
            5192, // Rare Damage Reduction
            6170, // Life Mead
            5966, // Vigor Mead
            5122, // Call of Leadership V
        };

        public static readonly List<int> BeerSpellIds = new List<int> {
            3531, 3533, 3862, 3864, 3530, 3863
        };

        public static readonly List<int> PagesSpellIds = new List<int> {
            3869, // Incantation of the Black Book (Pages of Salt and Ash)
         };

        public static readonly List<int> DestructionSpellIds = new List<int> {
            5204, // Surge of Destruction
         };

        public static readonly List<int> RegenSpellIds = new List<int> {
            5208, // Surge of Regen
         };

        public static readonly List<int> ProtectionSpellIds = new List<int> {
            5206, // Surge of Protection
         };

        public static readonly List<int> CisSpellIds = new List<int> {
            5753, // Cloaked in Skill
         };

        public static readonly List<int> CorrosionSpellIds = new List<int> {
            5387, 5388, 5389, 5390, 5391, 5392, 5393, 5394 // Corrosion
        };

        public static readonly List<int> CorruptionSpellIds = new List<int> {
            5395, 5396, 5397, 5398, 5399, 5400, 5401, 5402 // Corruption
        };

        public static readonly List<int> CurseSpellIds = new List<int> {
            5339, 5340, 5341, 5342, 5343, 5344, 5337, 5338 // Destructive Curse
        };

        public static Decal.Filters.Spell GetSpell(int id)
        {
            FileService service = CoreManager.Current.Filter<FileService>();

            Decal.Filters.Spell spell = service.SpellTable.GetById(id);
            return spell;
        }

        public static string GetSpellName(int id)
        {
            Decal.Filters.Spell spell = GetSpell(id);
            if(spell == null) { return ""; }

            return spell.Name;
        }
    }
}
