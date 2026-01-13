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
    public class Fellow
    {
        // Constants
        public static readonly int PerTick = 4;
        public static readonly int Cooldown = 3;

        // Collection
        public static List<Fellow> Fellows = new List<Fellow>();

        // Properties
        public WorldObject Item;
        public int Id = 0;
        public string Name = "";
        public string FellowshipName = "";
        public bool Requestable = true;
        public bool Identified = false;
        public DateTime LastRequestedAt = DateTime.MinValue;
        public DateTime LastUpdatedAt = DateTime.MinValue;

        public static void Init()
        {
            Fellows.Clear();
        }

        // Fellows without a Fellowship
        public static List<Fellow> Players()
        {
            int myId = CoreManager.Current.CharacterFilter.Id;
            return Fellows.Where(f => string.IsNullOrEmpty(f.FellowshipName) && f.Id != myId).OrderBy(f => f.Name).ToList();
        }

        public static List<IGrouping<string, Fellow>> Fellowships()
        {
            return Fellows.Where(f => !string.IsNullOrEmpty(f.FellowshipName)).GroupBy(f => f.FellowshipName).OrderBy(g => g.Key).ToList();
        }

        public static Fellow Find(int id) { return Fellows.Find(f => f.Item.Id == id); }
        public static Fellow Find(WorldObject item) { return Fellows.Find(f => f.Id == item.Id); }

        public static List<Fellow> RequestableFellows()
        {
            return Fellows
                .Where(f => f.Requestable && (f.LastRequestedAgo() == -1 || f.LastRequestedAgo() > Cooldown))
                .OrderBy(f => f.LastRequestedAt).ToList();
        }

        // We only need this for Nearby fellows. Not everyone in our fellowship which is handled by Fellowship class
        public static void Update()
        {
            // Remove old fellows we can no longer track
            Fellows.RemoveAll(f => CoreManager.Current.WorldFilter[f.Id] == null);

            // Update all fellows
            foreach (Fellow fellow in Fellows) { Update(fellow); }

            // Request ident for oldest players outside my fellowship
            foreach (Fellow fellow in RequestableFellows().Take(PerTick)) { Request(fellow); }
        }

        private static void Update(Fellow fellow)
        {
            if (fellow.Id == CoreManager.Current.CharacterFilter.Id)
            {
                UpdateFellowshipName(fellow, Fellowship.Name());
                fellow.Requestable = false;
            }
            else if (Fellowship.IsInFellowship(fellow.Id))
            {
                UpdateFellowshipName(fellow, Fellowship.Name());
                fellow.Requestable = true; 
            }
            else
            {
                fellow.Requestable = true;
            }
        }

        private static void UpdateFellowshipName(Fellow fellow, string fellowshipName)
        {
            fellow.FellowshipName = fellowshipName;
            fellow.LastUpdatedAt = DateTime.Now;
            fellow.Identified = true;
        }

        public static Fellow Add(WorldObject item)
        {
            if (item == null) { return null; }
            if (item.Id == 0) { return null; }
            if (item.ObjectClass != ObjectClass.Player) { return null; }

            // Return existing
            Fellow existing = Find(item);
            if(existing != null) { return existing; }

            string fellowshipName = item.Values(StringValueKey.FellowshipName);
            if(fellowshipName == null || fellowshipName.Length == 0) { fellowshipName = ""; }

            // Create new
            Fellow fellow = new() { 
                Item = item, 
                Id = item.Id, 
                Name = item.Name, 
                FellowshipName = fellowshipName
            };

            Fellows.Add(fellow);
            Update(fellow);
            if(item.HasIdData == false) { Request(fellow); }

            //Util.Chat($"Adding {fellow.ToString()}");
            
            return fellow;
        }

        public static void Request(Fellow fellow)
        {
            fellow.LastRequestedAt = DateTime.Now;
            Util.Chat($"Requesting: {fellow.Name}");
            CoreManager.Current.Actions.RequestId(fellow.Id);
        }

        // Once a player is identified, this packet is received with fellowship info
        public static void Parse(byte[] packet)
        {
            bool success = GetSuccess(packet);
            if (success == false) { return; }

            int id = GetObjectId(packet);
            if (id == 0) { return; }

            Fellow fellow = Add(CoreManager.Current.WorldFilter[id]);
            if (fellow == null) { return; }

            //Util.Chat("============");
            //Util.Chat($"Appraised {fellow.Item.Name}");
            //Util.Chat(BitConverter.ToString(packet));
            //Util.Chat("============");

            UpdateFellowshipName(fellow, GetFellowshipName(packet));
        }

        public new string ToString()
        {
            return $"{Name} Fellowship {FellowshipName}";
        }

        public int LastRequestedAgo()
        {
            if (LastRequestedAt == DateTime.MinValue) { return -1; }
            return (int)(DateTime.Now - LastRequestedAt).TotalSeconds;
        }

        public int LastUpdatedAgo()
        {
            if(LastUpdatedAt == DateTime.MinValue) { return -1; }
            return (int)(DateTime.Now - LastUpdatedAt).TotalSeconds;
        }

        private static bool GetSuccess(byte[] packet)
        {
            if (packet == null || packet.Length < 9) { return false; } // ObjectID(4) + Flags(4) + Success(1)

            using (var br = new BinaryReader(new MemoryStream(packet)))
            {
                br.ReadUInt32(); // skip ObjectID
                br.ReadUInt32(); // skip Flags
                return br.ReadBoolean(); // success
            }
        }

        private static int GetObjectId(byte[] packet)
        {
            if (packet == null) { return 0; }
            if (packet.Length < 20) { return 0; }

            // validate order header 0xF7B0
            ushort orderHdr = (ushort)(packet[0] | (packet[1] << 8));
            if (orderHdr != 0xF7B0) { return 0; }

            // validate message type 0x00C9
            uint messageType = (uint)(packet[12] | (packet[13] << 8) | (packet[14] << 16) | (packet[15] << 24));
            if (messageType != 0x000000C9) { return 0; }

            // objectID is at offset 16, little-endian uint32
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

                uint opCode = br.ReadUInt32(); // 0x00C9
                uint objectID = br.ReadUInt32();
                uint flags = br.ReadUInt32();
                uint success = br.ReadUInt32();

                if (success == 0) return "";

                // 2. Table Navigation
                if ((flags & 0x00000001) != 0) SafeSkip(br, 4, 4);
                if ((flags & 0x00002000) != 0) SafeSkip(br, 4, 8);
                if ((flags & 0x00000002) != 0) SafeSkip(br, 4, 4);
                if ((flags & 0x00000004) != 0) SafeSkip(br, 4, 8);

                // 0x08: String Table (OUR TARGET)
                if ((flags & 0x00000008) != 0)
                {
                    if (br.BaseStream.Position + 4 <= br.BaseStream.Length)
                    {
                        ushort count = br.ReadUInt16();
                        br.ReadUInt16(); // Padding

                        for (int i = 0; i < count; i++)
                        {
                            if (br.BaseStream.Position + 6 > br.BaseStream.Length) break;

                            uint key = br.ReadUInt32();
                            ushort len = br.ReadUInt16();

                            if (br.BaseStream.Position + len > br.BaseStream.Length) break;

                            byte[] strBytes = br.ReadBytes(len);

                            // AC String Alignment (4-byte)
                            int pad = (4 - (len % 4)) % 4;
                            if (br.BaseStream.Position + pad <= br.BaseStream.Length)
                                br.BaseStream.Position += pad;

                            if (key == 0x000A) // Fellowship Property
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
    }
}


