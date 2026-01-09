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
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Runtime.InteropServices;

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


        public unsafe static void Disband() { ((delegate* unmanaged[Cdecl]<int, byte>)6975808)(1); }
        public unsafe static void Quit() { ((delegate* unmanaged[Cdecl]<int, byte>)6975808)(0); }
        public unsafe static void Open() { ((delegate* unmanaged[Cdecl]<int, byte>)6975392)(1); }
        public unsafe static void Close() { ((delegate* unmanaged[Cdecl]<int, byte>)6975392)(0); }

        public unsafe static void Create(string name)
        {
            PStringBase<char> pStringBase = name.TrimEnd('\0') + '\0';
            ((delegate* unmanaged[Cdecl]<PStringBase<char>*, int, byte>)6977280)(&pStringBase, 1);
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

        public struct PStringBase<T> where T : unmanaged
        {
            public unsafe PSRefBuffer<T>* m_buffer;

            public unsafe static delegate* unmanaged[Thiscall]<PStringBase<ushort>*, ushort[], void> __Ctor_16 = (delegate* unmanaged[Thiscall]<PStringBase<ushort>*, ushort[], void>)5522640;

            public unsafe static delegate* unmanaged[Thiscall]<PStringBase<char>*, char[], void> __Ctor_ = (delegate* unmanaged[Thiscall]<PStringBase<char>*, char[], void>)4768736;

            public unsafe static delegate* unmanaged[Thiscall]<PStringBase<char>*, int, void> __Ctor = (delegate* unmanaged[Thiscall]<PStringBase<char>*, int, void>)4905888;

            public unsafe static PSRefBuffer<T>** s_NullBuffer = (PSRefBuffer<T>**)9367836;

            public unsafe static PStringBase<char>* null_string = (PStringBase<char>*)9367840;

            public unsafe static PStringBase<char>* whitespace_string = (PStringBase<char>*)9367844;

            public unsafe static PSRefBuffer<T>** s_NullBuffer_w = (PSRefBuffer<T>**)9367852;

            public unsafe static PStringBase<ushort>* null_string_w = (PStringBase<ushort>*)9367856;

            public unsafe static PStringBase<ushort>* whitespace_string_w = (PStringBase<ushort>*)9367860;

            public unsafe override string ToString()
            {
                if (m_buffer == null || m_buffer->m_len == 0)
                {
                    return "null";
                }

                if (typeof(T) == typeof(ushort))
                {
                    return new string((char*)(&m_buffer->m_data), 0, m_buffer->m_len - 1);
                }

                return new string((sbyte*)(&m_buffer->m_data), 0, m_buffer->m_len - 1);
            }

            public unsafe static implicit operator PStringBase<T>(string inStr)
            {
                PStringBase<T> result = default(PStringBase<T>);
                if (typeof(T) == typeof(ushort))
                {
                    result.m_buffer = *s_NullBuffer_w;
                    ushort[] array = new ushort[inStr.Length];
                    for (int i = 0; i < inStr.Length; i++)
                    {
                        array[i] = inStr[i];
                    }

                    __Ctor_16((PStringBase<ushort>*)(&result), array);
                    return result;
                }

                result.m_buffer = *s_NullBuffer;
                __Ctor_((PStringBase<char>*)(&result), inStr.ToCharArray());
                return result;
            }

            public unsafe static implicit operator PStringBase<T>(int v)
            {
                if (typeof(T) == typeof(ushort))
                {
                    throw new NotImplementedException();
                }

                PStringBase<T> result = default(PStringBase<T>);
                result.m_buffer = *s_NullBuffer;
                __Ctor((PStringBase<char>*)(&result), v);
                return result;
            }

            public unsafe PStringBase<char>* operator_equals(int i_int32)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, int, PStringBase<char>*>)4905408)(ref this, i_int32);
            }

            public unsafe PStringBase<ushort>* operator_equals(PStringBase<ushort>* rhs)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<ushort>*, PStringBase<ushort>*>)4759712)(ref this, rhs);
            }

            public unsafe byte operator_is_equal(PStringBase<char>* rhs)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, byte>)4897232)(ref this, rhs);
            }

            public unsafe byte operator_not_equal(PStringBase<char>* rhs)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, byte>)4897328)(ref this, rhs);
            }

            public unsafe PStringBase<char>* operator_plus(PStringBase<char>* result, PStringBase<char>* rhs)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, PStringBase<char>*, PStringBase<char>*>)4860816)(ref this, result, rhs);
            }

            public unsafe PStringBase<char>* operator_plus_equals(PStringBase<char>* rhs)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, PStringBase<char>*>)4789488)(ref this, rhs);
            }

            public unsafe uint GetPackSize()
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, uint>)5231088)(ref this);
            }

            public unsafe uint Pack(void** addr, uint size)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, void**, uint, uint>)5231248)(ref this, addr, size);
            }

            public unsafe int UnPack(void** addr, uint size)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, void**, uint, int>)5231712)(ref this, addr, size);
            }

            public unsafe void allocate_ref_buffer(uint len)
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, uint, void>)4207968)(ref this, len);
            }

            public unsafe void allocate_ref_buffer<UInt16>(uint len)
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, uint, void>)5518976)(ref this, len);
            }

            public unsafe void append_n_chars(char* str, uint count)
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, char*, uint, void>)4788416)(ref this, str, count);
            }

            public unsafe void break_reference()
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, void>)4266096)(ref this);
            }

            public unsafe void clear()
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, void>)4897168)(ref this);
            }

            public unsafe int cmp(PStringBase<char>* rhs, int case_sensitive)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, int, int>)4897424)(ref this, rhs, case_sensitive);
            }

            public unsafe uint compute_hash()
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, uint>)5235776)(ref this);
            }

            public unsafe byte eq(PStringBase<char>* rhs, int case_sensitive)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, int, byte>)4899664)(ref this, rhs, case_sensitive);
            }

            public unsafe int find_substring(PStringBase<char>* str)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, int>)5516960)(ref this, str);
            }

            public unsafe int replace(PStringBase<char>* search, PStringBase<char>* str)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, PStringBase<char>*, int>)5664016)(ref this, search, str);
            }

            public unsafe void set(char* str)
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, char*, void>)4207808)(ref this, str);
            }

            public unsafe void set(ushort* str)
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, ushort*, void>)5633408)(ref this, str);
            }

            public unsafe PStringBase<char>* substring(PStringBase<char>* result, uint first, uint last)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, uint, uint, PStringBase<char>*>)5939808)(ref this, result, first, last);
            }

            public unsafe PStringBase<char>* to_spstring(PStringBase<char>* result, ushort i_targetCodePage)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<char>*, ushort, PStringBase<char>*>)5530256)(ref this, result, i_targetCodePage);
            }

            public unsafe PStringBase<ushort>* to_wpstring(PStringBase<ushort>* result, ushort i_sourceCodePage)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, PStringBase<ushort>*, ushort, PStringBase<ushort>*>)5599680)(ref this, result, i_sourceCodePage);
            }

            public unsafe void trim(int pre, int post, PStringBase<char> filter)
            {
                ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, int, int, PStringBase<char>, void>)5700000)(ref this, pre, post, filter);
            }

            public unsafe int vsprintf(char* fmt, char* args)
            {
                return ((delegate* unmanaged[Thiscall]<ref PStringBase<T>, char*, char*, int>)4748416)(ref this, fmt, args);
            }
        }

        public struct PSRefBuffer<T> where T : unmanaged
        {
            public Turbine_RefCount _ref;

            public int m_len;

            public uint m_size;

            public uint m_hash;

            public unsafe fixed int m_data[128];
        }

        public struct Turbine_RefCount
        {
            public unsafe Vtbl* vfptr;

            public uint m_cRef;

            public unsafe static delegate* unmanaged[Thiscall]<Turbine_RefCount*, uint, void*> __scaDelDtor = (delegate* unmanaged[Thiscall]<Turbine_RefCount*, uint, void*>)4201520;

            public override string ToString()
            {
                return $"m_cRef:{m_cRef:X8}";
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct Vtbl
        {
            public unsafe static delegate* unmanaged[Thiscall]<int*, uint, void*> __vecDelDtor;
        }

    }
}


