using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static readonly int RescanAfterSeconds = 3;

        // Collection
        public static List<Fellow> Fellows = new List<Fellow>();

        // Properties
        public WorldObject Item;
        public int Id = 0;
        public string Name = "";
        public string Fellowship = "";
        public DateTime LastSeenAt = DateTime.MinValue;
        public bool Recruited = false;

        public static void Init()
        {
            Fellows.Clear();
        }

        public static void Update()
        {
            Scan();
        }
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
            foreach (Fellow fellow in Fellows)
            {
                if(fellow.Name != "Igmo Baggins" && fellow.Name != "C-mule" && fellow.Name != "Locke Eveldan") { continue; }
                if(fellow.Name != "Igmo Baggins") { continue; }
                if (fellow.LastSeenAgo() >= RescanAfterSeconds) { Request(fellow); }
            }
        }

        public static List<IGrouping<string, Fellow>> Fellowships()
        {
            return Fellows.GroupBy(f => f.Fellowship).OrderBy(g => g.Key).ToList();
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

            // Update
            fellow.Fellowship = GetFellowshipName(packet);
            fellow.LastSeenAt = DateTime.Now;

            Util.Chat("============");
            Util.Chat($"Updating {fellow.ToString()}");
            Util.Chat(BitConverter.ToString(packet));
            Util.Chat("============");
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

            // Create new
            Fellow fellow = new() { Item = item, Id = item.Id, Name = item.Name, Fellowship = "???", LastSeenAt = DateTime.Now };
            Fellows.Add(fellow);

            Util.Chat($"Adding {fellow.ToString()}");
            
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

        private static string GetFellowshipName(byte[] packet)
        {
            uint targetKey = (uint)StringValueKey.FellowshipName;

            using (var br = new BinaryReader(new MemoryStream(packet)))
            {
                while (br.BaseStream.Position < br.BaseStream.Length - 6)
                {
                    long start = br.BaseStream.Position;

                    ushort count = br.ReadUInt16();
                    ushort unknown = br.ReadUInt16();

                    if (count == 0 || count >= 500 || unknown >= 0x0100)
                    {
                        br.BaseStream.Position = start + 1;
                        continue;
                    }

                    bool valid = true;

                    for (int i = 0; i < count; i++)
                    {
                        if (br.BaseStream.Position + 6 > br.BaseStream.Length)
                        {
                            valid = false;
                            break;
                        }

                        uint key = br.ReadUInt32();
                        ushort len = br.ReadUInt16();

                        if (len > 512 || br.BaseStream.Position + len > br.BaseStream.Length)
                        {
                            valid = false;
                            break;
                        }

                        string value = Encoding.UTF8.GetString(br.ReadBytes(len)).TrimEnd('\0');
                        if (key == targetKey) return value;
                    }

                    br.BaseStream.Position = valid ? br.BaseStream.Position : start + 1;
                }
            }

            return "";
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
    }
}


