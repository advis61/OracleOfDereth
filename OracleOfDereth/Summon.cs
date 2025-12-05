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
using VirindiViewService.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Drawing;

namespace OracleOfDereth
{
    public class Summon
    {
        // Instance variables
        public WorldObject Item;

        public static void SetCurrent(WorldObject item)
        {
            Summon summon = new() { Item = item };
            if (summon.IsSummon() == false) { return; }

            Util.Chat(summon.ToString(), Util.ColorCyan, "");
        }

        public new string ToString()
        {
            if (IsRated()) {
                return $"{Item.Name} [DMG {DamageScore()}% | DEF {DefenseScore()}%]";
            } else {
                return Item.Name;
            }
        }

        public bool IsSummon()
        {
            if (Item == null) return false;
            if (Item.Id == 0) return false;
            if (Item.ObjectClass != ObjectClass.Misc) { return false; }

            if (Item.Name.EndsWith("Essence")) { return true; }
            if (Item.Name.Contains("Essence (")) { return true; }

            return false;
        }

        // From an old /mb exec passed around on Discord
        // $D = wobjectgetintprop[wobjectgetselection[], 370];
        // $C = wobjectgetintprop[wobjectgetselection[], 372];
        // $CD = wobjectgetintprop[wobjectgetselection[], 374];
        // (0.625 * (1 +$D / 100)*(.9 -$C / 100)+2 * (1 + ($D +$CD)/ 100)*(.1 +$C / 100))/ 0.01365
        public double DamageScore()
        {
            return Math.Round((float)((0.625f * (1 + D() / 100.0f) * (0.9 - C() / 100.0f) + 2 * (1 + (D() + CD()) / 100.0f) * (0.1 + C() / 100.0f)) - 0.7625f) * 165.9751f);
        }
        public double DefenseScore()
        {
            return Math.Round((float)((0.7625f - (0.625f * (1 - DR() / 100.0f) * (0.9f + CR() / 1000.0f) + 2 * (1 - (DR() + CDR()) / 100.0f) * (0.1f - CR() / 1000.0f))) / 0.002065f));
        }

        public bool IsRated() { 
            return (D() > 0 || C() > 0 || CD() > 0 || DR() > 0 || CR() > 0 || CDR() > 0); 
        }

        public int D() { return Item.Values((LongValueKey)370); }
        public int C() { return Item.Values((LongValueKey)372); }
        public int CD() { return Item.Values((LongValueKey)374); }
        public int DR() { return Item.Values((LongValueKey)371); }
        public int CR() { return Item.Values((LongValueKey)373); }
        public int CDR() { return Item.Values((LongValueKey)375); }
    }
}

