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
        // Instance properties
        public static WorldObject CurrentSelection;

        // Instance variables
        public WorldObject Current;

        public static void SetCurrent(WorldObject item)
        {
            CurrentSelection = item;
            if (item == null || item.Id == 0) { return; }

            Summon summon = GetCurrent();
            if (summon.IsSummon() == false) { return; }

            Util.Chat(summon.ToString(), Util.ColorCyan, "");
        }
        public static Summon GetCurrent()
        {
            return new() { Current = CurrentSelection };
        }

        public new string ToString()
        {
            if (IsRated()) {
                return $"{Current.Name} [DMG {Math.Round(DamageScore())} | DEF {Math.Round(DefenseScore())}]";
            } else {
                return Current.Name;
            }
        }

        public bool IsSummon()
        {
            if (Current == null) return false;
            if (Current.ObjectClass != Decal.Adapter.Wrappers.ObjectClass.Misc) { return false; }

            if (Current.Name.EndsWith("Essence")) { return true; }
            if (Current.Name.Contains("Essence (")) { return true; }

            return false;
        }

        // From an old /mb exec passed around on Discord
        // $D = wobjectgetintprop[wobjectgetselection[], 370];
        // $C = wobjectgetintprop[wobjectgetselection[], 372];
        // $CD = wobjectgetintprop[wobjectgetselection[], 374];
        // (0.625 * (1 +$D / 100)*(.9 -$C / 100)+2 * (1 + ($D +$CD)/ 100)*(.1 +$C / 100))/ 0.01365
        public float DamageScore()
        {
            return (float)(0.625 * (1 + D() / 100.0f) * (0.9 - C() / 100.0f) + 2 * (1 + (D() + CD()) / 100.0f) * (0.1 + C() / 100.0f)) / 0.01365f;
        }
        public float DefenseScore()
        {
            return (float)(0.625 * (1 + DR() / 100.0f) * (0.9 - CR() / 100.0f) + 2 * (1 + (DR() + CDR()) / 100.0f) * (0.1 + CR() / 100.0f)) / 0.01365f;
        }

        public bool IsRated() { 
            return (D() > 0 || C() > 0 || CD() > 0 || DR() > 0 || CR() > 0 || CDR() > 0); 
        }

        public int D() { return Current.Values((LongValueKey)370); }
        public int C() { return Current.Values((LongValueKey)372); }
        public int CD() { return Current.Values((LongValueKey)374); }
        public int DR() { return Current.Values((LongValueKey)371); }
        public int CR() { return Current.Values((LongValueKey)373); }
        public int CDR() { return Current.Values((LongValueKey)375); }
    }
}

