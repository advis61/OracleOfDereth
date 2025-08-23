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

namespace OracleOfDereth
{
    public class Target 
    {
        // Collection of Targets that have had spells cast on them
        public static Dictionary<int, Target> Targets = new Dictionary<int, Target>();
        public static Target CurrentTarget = null;

        // Properties
        public int Id = 0;

        public static void Init()
        {
            Targets.Clear();
            CurrentTarget = null;
        }

        public static void SetCurrentTarget(int itemGuid)
        {
            Target target;
            Target.Targets.TryGetValue(itemGuid, out target);

            if(target == null)
            {
                target = new Target() { Id = itemGuid };
            }

            CurrentTarget = target;
        }

        // Instance methods

        public new string ToString()
        {
            return $"{Name()}";
        }

        public WorldObject Item()
        {
            return CoreManager.Current.WorldFilter[Id];
        }
        public string Name()
        {
            return Item().Name;
        }
    }
}

