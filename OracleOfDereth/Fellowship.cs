using AcClient;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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
        private static readonly Random random = new Random();

        private static readonly List<string> FellowshipNames = new List<string> {
            "Aetherbound",
            "Aluvian Coast",
            "Ancient Dereth",
            "Arcanum Cabal",
            "Ashen Sigil",
            "Black Mana Rising",
            "Black Spawn Pact",
            "Blade of Ispar",
            "Crimson Order",
            "Crystal Exiles",
            "Dereth Forever",
            "Drudge Reavers",
            "Echoes of Ithaenc",
            "Eldrytch Council",
            "Empyrean Remnant",
            "Essence Ward",
            "Fallen Isparians",
            "Glyphbound",
            "Golemwatch",
            "Holtburg Ward",
            "Iron Fellowship",
            "Last Empyreans",
            "Leystone Keepers",
            "Mana Forged",
            "Mosswart Kin",
            "Nightbound",
            "Oblivion Tide",
            "Oathbound",
            "Olthoi Bane",
            "Plateau Watch",
            "Portal Storm",
            "Radiant Rebels",
            "Runic Accord",
            "Sentinels of Light",
            "Shadow Lugians",
            "Shoushi Circle",
            "Silver Dereth",
            "Spawn of Bael",
            "Spires of Dereth",
            "Spellweavers",
            "Stormwardens",
            "The Virindi Eye",
            "Tufa Stronghold",
            "Tumerok Claw",
            "Vanguard of Light",
            "Virindi Enclave",
            "Voidwalkers",
            "Wardens of Dereth",
            "Yaraq Vanguard"
        };

        public static int CurrentFellowId = 0; // Selected by clicking in the FellowsList
        public static bool AutoRecruitEnabled = false;

        public static void Init()
        {
            // Nothing to do
        }

        // If my current target is in the fellow, return that.
        // Otherwise last selected fellow on the FellowsList UI
        public static int SelectedFellowId()
        {
            int targetId = 0;
            int fellowId = Fellowship.CurrentFellowId;

            WorldObject target = Target.GetCurrent().Item();
            if (target != null && target.ObjectClass == ObjectClass.Player) { targetId = target.Id; }

            if (Fellowship.IsInFellowship(targetId)) { return targetId; }
            if (Fellowship.IsInFellowship(fellowId)) { return fellowId; }

            return 0;
        }

        public static void SelectFellow(int id)
        {
            CurrentFellowId = id;
        }

        public static bool NearbyLifestone()
        {
            WorldObjectCollection items = CoreManager.Current.WorldFilter.GetByObjectClass(ObjectClass.Lifestone);

            foreach (WorldObject item in items) {
                if(Util.GetDistanceFromPlayer(item) < 50.0) { return true; }
            }

            return false;
        }

        public unsafe static void AutoRecruit()
        {
            if(AutoRecruitEnabled == false) { return; }

            if (IsLeader() && !IsOpen()) { Open(); }
            if (CanRecruit() == false) { return; }
            if (NearbyLifestone()) { return; }

            foreach(Fellow player in Fellow.Players().Where(f => f.Identified).ToList()) { 
                if(IsInFellowship(player.Id) == false) { Recruit(player.Id); }
            }
        }

        public unsafe static void Create(string name = "")
        {
            if (name == "") { name = FellowshipNames[random.Next(FellowshipNames.Count)]; }

            AcClient.PStringBase<char> pStringBase = name.TrimEnd('\0') + '\0';
            ((delegate* unmanaged[Cdecl]<AcClient.PStringBase<char>*, int, byte>)6977280)(&pStringBase, 1);

            Open();
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
            status.Add("Leader", LeaderName());
            status.Add("Open", IsOpen().ToString());

            if (IsShareXp() && FellowCount() == 1) { status.Add("Experience", "Shared"); }
            else if (IsShareXp() && IsEvenXPSplit()) { status.Add("Experience", "Even split"); }
            else if (IsShareXp() && !IsEvenXPSplit()) { status.Add("Experience", "Uneven split"); }
            else { status.Add("Experience", "Not shared"); }

            status.Add("Fellows", FellowCount().ToString() + " / 9");

            if (AutoRecruitEnabled && IsFull()) {
                status.Add("Auto Recruit", "Fellowship full");
            }
            else if (AutoRecruitEnabled && NearbyLifestone()) {
                status.Add("Auto Recruit", "Paused near Life Stone");
            }
            else if (AutoRecruitEnabled && CanRecruit()) {
                status.Add("Auto Recruit", "Recruiting players");
            }
            else if (IsFull()) {
                status.Add("Recruit", "Fellowship full");
            }
            else if (CanRecruit()) {
                status.Add("Recruit", "Can recruit");
            }
            else {
                status.Add("Recruit", "Must be open or leader");
            }

            return status.ToList();
        }

        public unsafe static int FellowCount()
        {
            if (!IsInFellowship()) { return 0; }
            return (int)(*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._fellowship_table._currNum;
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
                var fellow = (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0._fellowship_table.GetByIndex(x);

                int id = (int)fellow->_key;
                string name = fellow->_data._name.ToString();

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
            return (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship != null;
        }

        public unsafe static bool IsInFellowship(int character_id)
        {
            return IsInFellowship() && (*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship->a0.IsFellow((uint)character_id) != 0;
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

        public unsafe static bool IsLeader(int id)
        {
            return IsInFellowship() && LeaderId() == (uint)id;
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



