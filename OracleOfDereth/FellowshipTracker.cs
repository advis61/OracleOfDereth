using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OracleOfDereth
{
    public static class FellowshipTracker
    {
        private static readonly int IdRequestsPerTick = 4;
        private static readonly int ReIdentifyIntervalSeconds = 3;
        public static List<Fellow> Fellows = new List<Fellow>();

        public static bool Debug = true;

        public static void Init()
        {
            Fellows.Clear();
        }

        public static Fellow Find(int id)
        {
            return Fellows.Find(f => f.Id == id);
        }

        public static List<Fellow> UnaffiliatedPlayers()
        {
            int myId = CoreManager.Current.CharacterFilter.Id;
            return Fellows.Where(f => string.IsNullOrEmpty(f.FellowshipName) && f.Id != myId).OrderBy(f => f.Name).ToList();
        }

        public static List<IGrouping<string, Fellow>> Fellowships()
        {
            return Fellows.Where(f => !string.IsNullOrEmpty(f.FellowshipName)).GroupBy(f => f.FellowshipName).OrderBy(g => g.Key).ToList();
        }

        public static void Update()
        {
            RemoveGonePlayers();
            UpdateKnownFellows();
            RequestIdentifications();
        }

        public static Fellow Add(WorldObject item)
        {
            if (item == null) return null;
            if (item.Id == 0) return null;
            if (item.ObjectClass != ObjectClass.Player) return null;

            Fellow existing = Find(item.Id);
            if (existing != null) return existing;

            string fellowshipName = item.Values(StringValueKey.FellowshipName);
            if (string.IsNullOrEmpty(fellowshipName)) fellowshipName = "";

            Fellow fellow = new Fellow
            {
                Item = item,
                Id = item.Id,
                Name = item.Name,
                FellowshipName = fellowshipName
            };

            Fellows.Add(fellow);

            if (!item.HasIdData)
            {
                fellow.LastRequestedAt = DateTime.Now;
                CoreManager.Current.Actions.RequestId(fellow.Id);
                Log($"Requesting ident for new player: {fellow.Name}");
            }
            else
            {
                Fellowship.AutoRecruit(fellow);
            }

            return fellow;
        }

        public static void Parse(byte[] packet)
        {
            if (!GetSuccess(packet)) return;

            int id = GetObjectId(packet);
            if (id == 0) return;

            Fellow fellow = Add(CoreManager.Current.WorldFilter[id]);
            if (fellow == null) return;

            string newName = GetFellowshipName(packet);
            string previous = fellow.FellowshipName;
            bool wasIdentified = fellow.Identified;

            fellow.FellowshipName = newName;
            fellow.LastIdentifiedAt = DateTime.Now;
            fellow.Identified = true;

            if (!wasIdentified)
            {
                Log($"Identified: {fellow.Name}, fellowship=\"{newName}\"");
            }
            else if (previous != newName)
            {
                Log($"Fellowship changed: {fellow.Name}, \"{previous}\" -> \"{newName}\"");
            }

            Fellowship.AutoRecruit(fellow);
        }

        private static void RemoveGonePlayers()
        {
            var removed = Fellows.Where(f => CoreManager.Current.WorldFilter[f.Id] == null).ToList();
            foreach (var fellow in removed) { Fellows.Remove(fellow); }
        }

        private static void UpdateKnownFellows()
        {
            int myId = CoreManager.Current.CharacterFilter.Id;
            string myFellowName = Fellowship.Name();

            foreach (Fellow fellow in Fellows)
            {
                if (fellow.Id == myId || Fellowship.IsInFellowship(fellow.Id))
                {
                    if (fellow.FellowshipName != myFellowName) { fellow.FellowshipName = myFellowName; }
                    if (fellow.Identified != true) { fellow.Identified = true; }

                    //fellow.LastIdentifiedAt = DateTime.Now;
                }
            }
        }

        private static void RequestIdentifications()
        {
            int myId = CoreManager.Current.CharacterFilter.Id;

            var requestable = Fellows.Where(f =>
                !(f.Id == myId) &&
                !Fellowship.IsInFellowship(f.Id) &&
                (!f.Identified || f.LastIdentifiedAgo() > ReIdentifyIntervalSeconds) &&
                (f.LastRequestedAgo() == -1 || f.LastRequestedAgo() > ReIdentifyIntervalSeconds)
            ).OrderBy(f => f.LastRequestedAt).Take(IdRequestsPerTick).ToList();

            foreach (var fellow in requestable)
            {
                fellow.LastRequestedAt = DateTime.Now;
                CoreManager.Current.Actions.RequestId(fellow.Id);
                Log($"Re-requesting ident: {fellow.Name} (last identified {fellow.LastIdentifiedAgo()}s ago)");
            }
        }

        private static void Log(string message)
        {
            if (Debug) Util.Chat($"[FT] {message}");
        }

        #region Packet Parsing

        private static bool GetSuccess(byte[] packet)
        {
            if (packet == null || packet.Length < 9) return false;

            using (var br = new BinaryReader(new MemoryStream(packet)))
            {
                br.ReadUInt32(); // skip ObjectID
                br.ReadUInt32(); // skip Flags
                return br.ReadBoolean(); // success
            }
        }

        private static int GetObjectId(byte[] packet)
        {
            if (packet == null || packet.Length < 20) return 0;

            ushort orderHdr = (ushort)(packet[0] | (packet[1] << 8));
            if (orderHdr != 0xF7B0) return 0;

            uint messageType = (uint)(packet[12] | (packet[13] << 8) | (packet[14] << 16) | (packet[15] << 24));
            if (messageType != 0x000000C9) return 0;

            uint objectId = (uint)(packet[16] | (packet[17] << 8) | (packet[18] << 16) | (packet[19] << 24));
            return (int)objectId;
        }

        private static string GetFellowshipName(byte[] packet)
        {
            if (packet.Length < 32) return "";

            using (var ms = new MemoryStream(packet))
            using (var br = new BinaryReader(ms))
            {
                br.BaseStream.Position = 12;

                uint opCode = br.ReadUInt32();
                uint objectID = br.ReadUInt32();
                uint flags = br.ReadUInt32();
                uint success = br.ReadUInt32();

                if (success == 0) return "";

                if ((flags & 0x00000001) != 0) SafeSkip(br, 4, 4);
                if ((flags & 0x00002000) != 0) SafeSkip(br, 4, 8);
                if ((flags & 0x00000002) != 0) SafeSkip(br, 4, 4);
                if ((flags & 0x00000004) != 0) SafeSkip(br, 4, 8);

                if ((flags & 0x00000008) != 0)
                {
                    if (br.BaseStream.Position + 4 <= br.BaseStream.Length)
                    {
                        ushort count = br.ReadUInt16();
                        br.ReadUInt16();

                        for (int i = 0; i < count; i++)
                        {
                            if (br.BaseStream.Position + 6 > br.BaseStream.Length) break;

                            uint key = br.ReadUInt32();
                            ushort len = br.ReadUInt16();

                            if (br.BaseStream.Position + len > br.BaseStream.Length) break;

                            byte[] strBytes = br.ReadBytes(len);

                            int pad = (4 - (len % 4)) % 4;
                            if (br.BaseStream.Position + pad <= br.BaseStream.Length)
                                br.BaseStream.Position += pad;

                            if (key == 0x000A)
                                return Encoding.UTF8.GetString(strBytes).TrimEnd('\0');
                        }
                    }
                }

                return "";
            }
        }

        private static void SafeSkip(BinaryReader br, int keySize, int valSize)
        {
            if (br.BaseStream.Position + 4 > br.BaseStream.Length) return;
            ushort count = br.ReadUInt16();
            br.ReadUInt16();
            long totalToSkip = (long)count * (keySize + valSize);
            br.BaseStream.Position = Math.Min(br.BaseStream.Position + totalToSkip, br.BaseStream.Length);
        }

        #endregion
    }
}
