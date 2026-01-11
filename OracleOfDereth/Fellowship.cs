using AcClient;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace OracleOfDereth
{
    public class Fellowship
    {
        public static void Init()
        {
            // Nothing to do
        }

        public unsafe static void Create(string name = "")
        {
            if (name == "") { name = "eveldan"; }

            AcClient.PStringBase<char> pStringBase = name.TrimEnd('\0') + '\0';
            ((delegate* unmanaged[Cdecl]<AcClient.PStringBase<char>*, int, byte>)6977280)(&pStringBase, 1);
        }

        public unsafe static void Recruit(string name)
        {
            WorldObject closest = Util.GetClosestObject(name);

            if (closest == null) {
                Util.Chat($"Could not find fellow: {name}");
                return;
            }

            Recruit(closest.Id);
        }

        public unsafe static void Recruit(int id) { ((delegate* unmanaged[Cdecl]<uint, byte>)6976016)((uint)id); }
        public unsafe static void Dismiss(int id) { ((delegate* unmanaged[Cdecl]<uint, byte>)6975600)((uint)id); }
        public unsafe static void Leader(int id) { ((delegate* unmanaged[Cdecl]<uint, byte>)6975184)((uint)id); }
        public unsafe static void Disband() { ((delegate* unmanaged[Cdecl]<int, byte>)6975808)(1); }
        public unsafe static void Quit() { ((delegate* unmanaged[Cdecl]<int, byte>)6975808)(0); }
        public unsafe static void Open() { ((delegate* unmanaged[Cdecl]<int, byte>)6975392)(1); }
        public unsafe static void Close() { ((delegate* unmanaged[Cdecl]<int, byte>)6975392)(0); }

        public static List<KeyValuePair<string, string>> Status()
        {
            var status = new Dictionary<string, string>();

            if (IsInFellowship() == false)
            {
                status.Add("None", "");
                return status.ToList();
            }

            status.Add("Name", Name());
            status.Add("Fellows", FellowCount().ToString());
            status.Add("Leader", LeaderName());
            status.Add("Open", IsOpen().ToString());

            if (IsShareXp() && FellowCount() == 1) { status.Add("XP Sharing", "Shared"); }
            else if (IsShareXp() && IsEvenXPSplit()) { status.Add("XP Sharing", "Even split"); }
            else if (IsShareXp() && !IsEvenXPSplit()) { status.Add("XP Sharing", "Uneven split"); }
            else { status.Add("XP Sharing", "Not shared"); }

            status.Add("Status", (CanRecruit() ? "Can recruit" : "Cannot recruit"));

            return status.ToList();
        }

        public unsafe static uint FellowCount()
        {
            if (!IsInFellowship()) { return 0; }
            return (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._fellowship_table._currNum;
        }

        public unsafe static Dictionary<int, string> Fellows()
        {
            var fellows = new Dictionary<int, string>();

            if (IsInFellowship() == false)
            {
                fellows.Add(0, "None");
                return fellows;
            }

            for (int x = 0; x < FellowCount(); x++)
            {
                int id = (int)(*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._fellowship_table.GetByIndex(x)->_key;
                string name = (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._fellowship_table.GetByIndex(x)->_data._name.ToString();
                fellows.Add(id, name);
            }

            return fellows;
        }

        public unsafe static string Name()
        {
            if (IsInFellowship() == false) { return ""; }
            return (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._name.ToString();
        }

        public unsafe static bool IsInFellowship()
        {
            return ((*AcClient.ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship != null);
        }

        public unsafe static bool IsInFellowship(uint character_id)
        {
            return IsInFellowship() && (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0.IsFellow(character_id) != 0;
        }

        public unsafe static uint LeaderId()
        {
            if (!IsInFellowship()) { return 0; }
            return (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._leader;
        }

        public unsafe static string LeaderName()
        {
            if (IsInFellowship() == false) { return ""; }

            for (int x = 0; x < FellowCount(); x++)
            {
                var fellow = (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._fellowship_table.GetByIndex(x);
                if (fellow->_key == LeaderId()) { return fellow->_data._name.ToString(); }
            }

            return "";
        }

        public unsafe static bool IsLeader()
        {
            return IsInFellowship() && LeaderId() == CoreManager.Current.CharacterFilter.Id;
        }

        public unsafe static bool IsLeader(uint id)
        {
            return IsInFellowship() && LeaderId() == id;
        }

        public unsafe static bool IsOpen()
        {
            return IsInFellowship() && (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._open_fellow == 1;
        }

        public unsafe static bool IsEvenXPSplit()
        {
            return IsInFellowship() && (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._even_xp_split == 1;
        }

        public unsafe static bool IsShareXp()
        {
            return IsInFellowship() && (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._share_xp == 1;
        }

        public unsafe static bool IsFull()
        {
            return FellowCount() >= 9;
        }

        public unsafe static bool CanRecruit()
        {
            return IsInFellowship() && (IsLeader() || IsOpen()) && !IsFull();
        }
    }
}



