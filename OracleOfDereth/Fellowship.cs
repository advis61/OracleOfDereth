using AcClient;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OracleOfDereth
{
    public static class Fellowship
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

        public static int CurrentFellowId = 0;
        public static bool AutoRecruitEnabled = false;

        // If my current target is in the fellow, return that.
        // Otherwise last selected fellow on the FellowsList UI
        public static int SelectedFellowId()
        {
            int targetId = 0;
            int fellowId = CurrentFellowId;

            WorldObject target = Target.GetCurrent().Item();
            if (target != null && target.ObjectClass == ObjectClass.Player) { targetId = target.Id; }

            if (IsInFellowship(targetId)) { return targetId; }
            if (IsInFellowship(fellowId)) { return fellowId; }

            return 0;
        }

        public static void SelectFellow(int id)
        {
            CurrentFellowId = id;
        }

        public static void AutoOpenFellow()
        {
            if (AutoRecruitEnabled && IsLeader() && !IsOpen()) Open();
        }

        public static void AutoRecruit(Fellow fellow, bool force = false)
        {
            if (!AutoRecruitEnabled) return;
            if (fellow.Id == CoreManager.Current.CharacterFilter.Id) return;

            if (!IsInFellowship()) return;
            if (IsInFellowship(fellow.Id)) return;

            if (!fellow.Identified) return;
            if (!fellow.FellowshipNameBlank()) return;

            if (!CanRecruit()) return;
            if (NearbyLifestone() || NearbyBindstone()) return;

            if (fellow.WasRecruited() && !force) return;

            fellow.LastRecruitedAt = DateTime.Now;
            Recruit(fellow.Id);
        }

        public static void RecruitAll()
        {
            foreach (var fellow in FellowshipTracker.Fellows) { AutoRecruit(fellow, true); }
        }

        public static bool NearbyLifestone()
        {
            WorldObjectCollection items = CoreManager.Current.WorldFilter.GetByObjectClass(ObjectClass.Lifestone);

            foreach (WorldObject item in items) {
                if(Util.GetDistanceFromPlayer(item) < 50.0) { return true; }
            }

            return false;
        }

        public static bool NearbyBindstone()
        {
            WorldObjectCollection items = CoreManager.Current.WorldFilter.GetByName("Bind Stone");

            foreach (WorldObject item in items)
            {
                if (Util.GetDistanceFromPlayer(item) < 50.0) { return true; }
            }

            return false;
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

            if (IsInFellowship() == false) { return status.ToList(); }

            status.Add("Leader", LeaderName());
            status.Add("Open", IsOpen().ToString());

            // Experience
            if (IsShareXp() && FellowCount() == 1) {
                status.Add("Experience", "Shared");
            }
            else if (IsShareXp() && IsEvenXPSplit()) {
                status.Add("Experience", "Even split");
            }
            else if (IsShareXp() && !IsEvenXPSplit()) {
                status.Add("Experience", "Uneven split");
            }
            else {
                status.Add("Experience", "Not shared");
            }

            // Recruit or Auto-Recruit
            if (AutoRecruitEnabled && IsFull()) {
                status.Add("Auto Recruit", "Fellowship full");
            }
            else if (AutoRecruitEnabled && NearbyLifestone()) {
                status.Add("Auto Recruit", "Paused by Life Stone");
            }
            else if (AutoRecruitEnabled && NearbyBindstone()) {
                status.Add("Auto Recruit", "Paused by Bind Stone");
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
            if (IsInFellowship() == false) { return fellows; }

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
