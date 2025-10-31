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

        // My current target
        public static int CurrentTargetId = 0;

        // Instance properties
        public int Id = 0;

        public static void Init()
        {
            CurrentTargetId = 0;
        }

        public static void ItemIdentified(int id)
        {
            CurrentTargetId = id;

            Summon summon = GetCurrent();
            if (summon == null || summon.IsSummon() == false) { return; }

            // Show damage and defense score
            if (summon.IsRated()) {
                Util.Chat($"{summon.Name()} [DMG {Math.Round(summon.DamageScore())} | DEF {Math.Round(summon.DefenseScore())}]", Util.ColorCyan, "");
            } else {
                Util.Chat($"{summon.Name()}", Util.ColorCyan, "");
            }
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

        public int D()
        {
            if (Item() == null) return 0;
            return Item().Values((LongValueKey)370);
        }
        public int C()
        {
            if (Item() == null) return 0;
            return Item().Values((LongValueKey)372);
        }

        public int CD()
        {
            if (Item() == null) return 0;
            return Item().Values((LongValueKey)374);
        }

        public int DR()
        {
            if (Item() == null) return 0;
            return Item().Values((LongValueKey)371);
        }

        public int CR()
        {
            if (Item() == null) return 0;
            return Item().Values((LongValueKey)373);
        }

        public int CDR()
        {
            if (Item() == null) return 0;
            return Item().Values((LongValueKey)375);
        }

        public bool IsRated() {
            return (D() > 0 || C() > 0 || CD() > 0 || DR() > 0 || CR() > 0 || CDR() > 0);
        }

        public static Summon GetCurrent()
        {
            return new() { Id = CurrentTargetId };
        }
        public new string ToString()
        {
            return $"[{Id}] {Name()} {Item().HasIdData}";
        }

        // Instance methods

        public bool IsSummon()
        {
            if (Item() == null) return false;
            if(ObjectClass() != "Misc") { return false; }

            if(Name().EndsWith("Essence")) { return true; }
            if(Name().Contains("Essence (")) { return true; }

            return false;
        }

        private WorldObject? Item()
        {
            if (Id == 0) { return null; }

            try {
                return CoreManager.Current.WorldFilter[Id];
            } catch {
                return null;
            }
        }
        public string Name()
        {
            if (Item() == null) return "";
            return Item().Name;
        }

        private string ObjectClass()
        {
            if (Item() == null) return "";
            return Item().ObjectClass.ToString();
        }    
    }
}

