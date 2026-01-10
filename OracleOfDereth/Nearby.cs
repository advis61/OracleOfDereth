using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;


namespace OracleOfDereth
{
    public class Nearby
    {
        // Collection
        public static List<Nearby> Nearbys = new List<Nearby>();

        // Properties
        public WorldObject Item;
        public int Id = 0;
        public string Name = "";
        public DateTime LastSeenAt = DateTime.MinValue;

        public static void Init()
        {
            Nearbys.Clear();
        }

        public static void Update()
        {
        }
    }
}


