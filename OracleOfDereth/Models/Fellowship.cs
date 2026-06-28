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

        // Auto Recruit pausing 
        private static bool _autoRecruitEnabled = false;
        private static DateTime _lastPauseAt = DateTime.MinValue;
        private static string _lastPauseReason = "";
        private static readonly double PauseGraceSeconds = 3.0;

        public static bool AutoRecruitEnabled
        {
            get { return _autoRecruitEnabled; }
            set
            {
                // Turning it on adopts the current spot as the baseline and clears any stale pause
                // hold, so recruiting starts immediately — unless a station is actually nearby right
                // now. Without this, re-enabling after moving to a new landblock would read as a
                // fresh zone-in and pause for the grace window.
                if (value && !_autoRecruitEnabled)
                {
                    _lastLandblock = -1;
                    _lastPauseAt = DateTime.MinValue;
                    _lastPauseReason = "";
                }

                _autoRecruitEnabled = value;
            }
        }

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
            if (AutoRecruitPauseReason() != "") return;

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
                if(Util.GetDistanceFromPlayer(item) < 150.0) { return true; }
            }

            return false;
        }

        public static bool NearbyBindstone()
        {
            WorldObjectCollection items = CoreManager.Current.WorldFilter.GetByName("Bind Stone");

            foreach (WorldObject item in items)
            {
                if (Util.GetDistanceFromPlayer(item) < 150.0) { return true; }
            }

            return false;
        }

        public static bool NearbyTownNetworkPortal()
        {
            WorldObjectCollection items = CoreManager.Current.WorldFilter.GetByName("Portal to Town Network");

            foreach (WorldObject item in items)
            {
                if (Util.GetDistanceFromPlayer(item) < 150.0) { return true; }
            }

            return false;
        }


        // Returns the active pause reason ("Zoning In", "Life Stone", "Bind Stone", "Town Portal"), or "" if auto-recruit is clear to fire.
        public static string AutoRecruitPauseReason()
        {
            string reason =
                RecentlyZoned() ? "Zoning In" :
                NearbyLifestone() ? "Life Stone" :
                NearbyBindstone() ? "Bind Stone" :
                NearbyTownNetworkPortal() ? "Town Portal" : "";

            if (reason != "")
            {
                _lastPauseAt = DateTime.Now;
                _lastPauseReason = reason;
                return reason;
            }

            if ((DateTime.Now - _lastPauseAt).TotalSeconds < PauseGraceSeconds) return _lastPauseReason;

            return "";
        }

        private static int _lastLandblock = -1;

        // A landblock jump of this many cells or more counts as a zone (portal/recall/dungeon).
        // Smaller steps are ordinary outdoor movement into an adjacent landblock and are ignored.
        private static readonly int ZoneJumpThreshold = 2;

        private static bool RecentlyZoned()
        {
            // Edge detector: true only on the first poll after a zone-sized landblock jump. The shared
            // PauseGraceSeconds hold in AutoRecruitPauseReason supplies the actual pause window, so this
            // just needs to flag the moment we arrive somewhere new.
            //
            // Compares the landblock (high 16 bits) only -- the low 16 bits are the cell within the
            // landblock and flip as you walk around. The landblock's X (high byte) and Y (low byte) are
            // world-grid coordinates; adjacent landblocks differ by 1. We re-baseline on every change,
            // so running across outdoor boundaries is a series of distance-1 steps that never trip,
            // while a teleport is a single large-distance jump that does.
            int current = Util.CurrentLandblock();

            // The very first poll only establishes a baseline. RecentlyZoned() isn't called until
            // auto-recruit is enabled, so without this the act of turning it on would look like a zone-in.
            if (_lastLandblock == -1) { _lastLandblock = current; return false; }
            if (current == _lastLandblock) return false;

            int dx = Math.Abs(((current >> 8) & 0xFF) - ((_lastLandblock >> 8) & 0xFF));
            int dy = Math.Abs((current & 0xFF) - (_lastLandblock & 0xFF));
            int distance = Math.Max(dx, dy);

            _lastLandblock = current;

            return distance >= ZoneJumpThreshold;
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
            // Poll the pause hold every tick (when enabled) so its sticky timestamp stays fresh,
            // even while the fellowship is full. The held reason carries through brief WorldFilter gaps.
            string pauseReason = AutoRecruitEnabled ? AutoRecruitPauseReason() : "";

            if (AutoRecruitEnabled && IsFull()) {
                status.Add("Auto Recruit", "Fellowship full");
            }
            else if (AutoRecruitEnabled && pauseReason != "") {
                status.Add("Auto Recruit", $"Paused by {pauseReason}");
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

        public static int MaxFellowCount()
        {
            return Server.IsConquest ? 14 : 9;
        }

        public unsafe static bool IsFull()
        {
            return FellowCount() >= MaxFellowCount();
        }

        public unsafe static bool CanRecruit()
        {
            return IsInFellowship() && (IsLeader() || IsOpen()) && !IsFull();
        }
    }
}
