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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace OracleOfDereth
{
    public class Fellow
    {
        // Constants
        public static readonly int RemoveAfterSeconds = 10;
        public static readonly int RescanAfterSeconds = 2;

        // Collection
        public static List<Fellow> Fellows = new List<Fellow>();

        // Properties
        public WorldObject Item;
        public int Id = 0;
        public string Name = "";
        public string Fellowship = "";
        public DateTime LastSeenAt = DateTime.MinValue;

        public static void Init()
        {
            Fellows.Clear();
        }

        public static void Update()
        {
            Scan();
        }

        public unsafe bool InFellowship { get => ((*ClientFellowshipSystem.s_pFellowshipSystem)->m_pFellowship != null); }

        public unsafe static void Disband() { ((delegate* unmanaged[Cdecl]<int, byte>)6975808)(1); }
        public unsafe static void Quit() { ((delegate* unmanaged[Cdecl]<int, byte>)6975808)(0); }
        public unsafe static void Open() { ((delegate* unmanaged[Cdecl]<int, byte>)6975392)(1); }
        public unsafe static void Close() { ((delegate* unmanaged[Cdecl]<int, byte>)6975392)(0); }


        public static void Scan()
        {
            // Add all fellows
            WorldObjectCollection items = CoreManager.Current.WorldFilter.GetByObjectClass(ObjectClass.Player);

            foreach (var item in items) {
                Fellow fellow = Find(item);
                if(fellow != null) { continue; }

                fellow = Add(item);
                Request(fellow);
            }

            // Remove old fellows we can no longer track
            Fellows.RemoveAll(f => CoreManager.Current.WorldFilter[f.Id] == null && f.LastSeenAgo() >= RemoveAfterSeconds);

            // Update fellows we can see
            foreach (Fellow fellow in Fellows.OrderByDescending(f => f.LastSeenAgo()).ToList())
            {
                if (fellow.LastSeenAgo() >= RescanAfterSeconds) { Request(fellow); }
            }
        }

        public static List<IGrouping<string, Fellow>> Fellowships()
        {
            return Fellows.GroupBy(f => f.Fellowship).OrderBy(g => g.Key).ToList();
        }
        public unsafe static void Recruit(string name)
        {
            WorldObject closest = Util.GetClosestObject(name);

            if(closest == null) { 
                Util.Chat($"Could not find fellow: {name}"); 
                return;
            }

            Util.Chat($"Recruiting: {name}");

            try
            {
                ((delegate* unmanaged[Cdecl]<uint, byte>)6976016)((uint)closest.Id);
            }
            catch (Exception) { } // Eat the decal error
        }


        public static void Recruit()
        {
            //foreach (Fellow fellow in Fellows)
            //{
            //    if (fellow.Id == CoreManager.Current.CharacterFilter.Id) { continue; }
            //    if (fellow.Recruited == true) { continue; }

            //    if (fellow.Fellowship == "" || fellow.Fellowship == "(none)")
            //    {

            //        fellow.Recruited = true;

            //        try
            //        {
            //            WorldObject item = CoreManager.Current.WorldFilter[fellow.Id];
            //            CoreManager.Current.Actions.FellowshipRecruit(item.Id);
            //        }
            //        catch (AccessViolationException) { } // Eat the decal error

            //        //Util.Chat($"/ub fellow recruit {fellow.Name}", 1, "");
            //    }
            //}
        }

        // Once a player is identified, this packet is received with fellowship info
        public static void Parse(byte[] packet)
        {
            bool success = GetSuccess(packet);
            if(success == false) { return; }

            int id = GetObjectId(packet);
            if(id == 0) { return; }

            Fellow fellow = Add(CoreManager.Current.WorldFilter[id]);
            if(fellow == null) { return; }

            //Util.Chat("============");
            //Util.Chat($"Appraised {fellow.Item.Name}");
            //Util.Chat(BitConverter.ToString(packet));
            //Util.Chat("============");

            // Update
            fellow.Fellowship = GetFellowshipName(packet);
            fellow.LastSeenAt = DateTime.Now;
        }

        public static Fellow Find(int id) { return Fellows.Find(f => f.Item.Id == id); }
        public static Fellow Find(WorldObject item) { return Fellows.Find(f => f.Id == item.Id); }
        public static void Request(Fellow fellow) { CoreManager.Current.Actions.RequestId(fellow.Id); }

        public static Fellow Add(WorldObject item)
        {
            if (item == null) { return null; }
            if (item.Id == 0) { return null; }
            if (item.ObjectClass != ObjectClass.Player) { return null; }

            // Return existing
            Fellow existing = Find(item);
            if(existing != null) { return existing; }

            string fellowship = item.Values(StringValueKey.FellowshipName);
            if(fellowship == null || fellowship.Length == 0) { fellowship = "???"; }

            // Create new
            Fellow fellow = new() { 
                Item = item, 
                Id = item.Id, 
                Name = item.Name, 
                Fellowship = fellowship, 
                LastSeenAt = DateTime.Now 
            };

            Fellows.Add(fellow);

            //Util.Chat($"Adding {fellow.ToString()}");
            
            return fellow;
        }

        public new string ToString()
        {
            return $"{Name} Fellowship {Fellowship}";
        }

        public int LastSeenAgo()
        {
            if(LastSeenAt == DateTime.MinValue) { return -1; }
            return (int)(DateTime.Now - LastSeenAt).TotalSeconds;
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

        public static string GetFellowshipName(byte[] packet)
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


