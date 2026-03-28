using OracleOfDereth;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AcClient;

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

    //public unsafe override string ToStringOriginalFromUtilityBelt()
    //{
    //    if (m_buffer == null || m_buffer->m_len == 0)
    //    {
    //        return "null";
    //    }

    //    if (typeof(T) == typeof(ushort))
    //    {
    //        return new string((char*)(&m_buffer->m_data), 0, m_buffer->m_len - 1);
    //    }

    //    return new string((sbyte*)(&m_buffer->m_data), 0, m_buffer->m_len - 1);
    //}

    public unsafe override string ToString()
    {
        if (m_buffer == null || m_buffer->m_len == 0) return "null";

        // Handle ushort (wide char) strings
        if (typeof(T) == typeof(ushort)) { return new string((char*)(&m_buffer->m_data), 0, m_buffer->m_len - 1); }

        // Handle char strings using your working offset
        // Based on your dump, the real string starts at offset 16 + 4
        byte* raw = (byte*)m_buffer;
        byte* strPtr = raw + 16 + 4;

        int len = 0;
        while (strPtr[len] != 0) len++;

        return new string((sbyte*)strPtr, 0, len);
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


public struct ClientFellowshipSystem
{
    public ClientSystem a0;

    public Turbine_RefCount m_cTurbineRefCount;

    public unsafe CFellowship* m_pFellowship;

    public unsafe static ClientFellowshipSystem** s_pFellowshipSystem = (ClientFellowshipSystem**)8852748;

    public unsafe override string ToString()
    {
        return $"a0(ClientSystem):{a0}, m_cTurbineRefCount(Turbine_RefCount):{m_cTurbineRefCount}, m_pFellowship:->(CFellowship*)0x{(int)m_pFellowship:X8}";
    }

    public unsafe void __Ctor()
    {
        ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, void>)5677072)(ref this);
    }

    public unsafe void DeleteFellowship()
    {
        ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, void>)5679952)(ref this);
    }

    public unsafe static ClientFellowshipSystem* GetFellowshipSystem()
    {
        return ((delegate* unmanaged[Cdecl]<ClientFellowshipSystem*>)5676592)();
    }

    public unsafe uint Handle_Fellowship__Dismiss(uint dismissed)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, uint, uint>)5680304)(ref this, dismissed);
    }

    public unsafe uint Handle_Fellowship__FullUpdate(CFellowship* fellowship)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, CFellowship*, uint>)5677408)(ref this, fellowship);
    }

    public unsafe uint Handle_Fellowship__Quit(uint quitter)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, uint, uint>)5680176)(ref this, quitter);
    }

    public unsafe uint Handle_Fellowship__UpdateFellow(uint id, Fellow* fellow, uint updateType)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, uint, Fellow*, uint, uint>)5676640)(ref this, id, fellow, updateType);
    }

    public unsafe byte IsFellow(uint i_iid)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, uint, byte>)5676752)(ref this, i_iid);
    }

    public unsafe byte IsFellowshipLeader(uint i_iid)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, uint, byte>)5676784)(ref this, i_iid);
    }

    public unsafe void OnEndCharacterSession()
    {
        ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, void>)5676608)(ref this);
    }

    public unsafe void OnShutdown()
    {
        ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, void>)5680112)(ref this);
    }

    public unsafe TResult* QueryInterface(TResult* result, Turbine_GUID* i_rcInterface, void** o_ppvInterface)
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, TResult*, Turbine_GUID*, void**, TResult*>)5676816)(ref this, result, i_rcInterface, o_ppvInterface);
    }

    public unsafe uint Release()
    {
        return ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, uint>)5677024)(ref this);
    }

    public unsafe void SelectNextFellow()
    {
        ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, void>)5677712)(ref this);
    }

    public unsafe void SelectPreviousFellow()
    {
        ((delegate* unmanaged[Thiscall]<ref ClientFellowshipSystem, void>)5677920)(ref this);
    }
}

public struct CFellowship
{
    public Fellowship a0;

    public override string ToString()
    {
        return a0.ToString();
    }

    public unsafe void __Ctor()
    {
        ((delegate* unmanaged[Thiscall]<ref CFellowship, void>)5879152)(ref this);
    }
}


public struct Fellowship
{
    public PackObj a0;

    public PackableHashTable<uint, Fellow> _fellowship_table;

    public PStringBase<char> _name;

    public uint _leader;

    public int _share_xp;

    public int _even_xp_split;

    public int _open_fellow;

    public int _locked;

    public PackableHashTable<uint, uint> _fellows_departed;

    public override string ToString()
    {
        return $"a0(PackObj):{a0}, _fellowship_table(PackableHashTable<UInt32,Fellow>):{_fellowship_table}, _name(AC1Legacy.PStringBase<char>):{_name}, _leader:{_leader:X8}, _share_xp(int):{_share_xp}, _even_xp_split(int):{_even_xp_split}, _open_fellow(int):{_open_fellow}, _locked(int):{_locked}, _fellows_departed(PackableHashTable<UInt32,UInt32>):{_fellows_departed}";
    }

    public unsafe void __Ctor(Fellowship* __that)
    {
        ((delegate* unmanaged[Thiscall]<ref Fellowship, Fellowship*, void>)5679824)(ref this, __that);
    }

    public unsafe void __Ctor()
    {
        ((delegate* unmanaged[Thiscall]<ref Fellowship, void>)6007008)(ref this);
    }

    public unsafe Fellowship* operator_equals()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, Fellowship*>)6007136)(ref this);
    }

    public unsafe int AddFellow(uint fellow_id, Fellow* fellow)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, Fellow*, int>)6006208)(ref this, fellow_id, fellow);
    }

    public unsafe uint CalculateExperienceProportionSum()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint>)6005360)(ref this);
    }

    public unsafe Fellow* GetFellow(uint fellow)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, Fellow*>)4779424)(ref this, fellow);
    }

    public unsafe uint GetLeadersLevel()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint>)6005488)(ref this);
    }

    public unsafe uint GetNonLeaderFellowID()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint>)6005568)(ref this);
    }

    public unsafe uint GetPackSize()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint>)6005984)(ref this);
    }

    public unsafe void HandleLockedRemoveFellow(uint fellow_id)
    {
        ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, void>)6005680)(ref this, fellow_id);
    }

    public unsafe int InqFellow(uint fellow, Fellow* retval)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, Fellow*, int>)6005264)(ref this, fellow, retval);
    }

    public unsafe int IsFellow(uint fellow)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, int>)6005184)(ref this, fellow);
    }

    public unsafe int IsFull()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, int>)6005168)(ref this);
    }

    public unsafe uint Pack(void** addr, uint size)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, void**, uint, uint>)6006064)(ref this, addr, size);
    }

    public unsafe void RecalculateEvenXPSplitting()
    {
        ((delegate* unmanaged[Thiscall]<ref Fellowship, void>)6005792)(ref this);
    }

    public unsafe int RemoveFellow(uint fellow)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, int>)6006720)(ref this, fellow);
    }

    public unsafe int UnPack(void** addr, uint size)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, void**, uint, int>)6006320)(ref this, addr, size);
    }

    public unsafe int UpdateFellow(uint fellow_id, Fellow* fellow)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellowship, uint, Fellow*, int>)6006896)(ref this, fellow_id, fellow);
    }
}


public struct Fellow
{
    public PackObj a0;

    public PStringBase<char> _name;

    public uint _level;

    public uint _cp_cache;

    public uint _lum_cache;

    public int _share_loot;

    public uint _max_health;

    public uint _max_stamina;

    public uint _max_mana;

    public uint _current_health;

    public uint _current_stamina;

    public uint _current_mana;

    public override string ToString()
    {
        return $"a0(PackObj):{a0}, _name(AC1Legacy.PStringBase<char>):{_name}, _level:{_level:X8}, _cp_cache:{_cp_cache:X8}, _lum_cache:{_lum_cache:X8}, _share_loot(int):{_share_loot}, _max_health:{_max_health:X8}, _max_stamina:{_max_stamina:X8}, _max_mana:{_max_mana:X8}, _current_health:{_current_health:X8}, _current_stamina:{_current_stamina:X8}, _current_mana:{_current_mana:X8}";
    }

    public unsafe void __Ctor(Fellow* rhs)
    {
        ((delegate* unmanaged[Thiscall]<ref Fellow, Fellow*, void>)6007344)(ref this, rhs);
    }

    public unsafe void __Ctor()
    {
        ((delegate* unmanaged[Thiscall]<ref Fellow, void>)6007264)(ref this);
    }

    public unsafe Fellow* operator_equals()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellow, Fellow*>)6007456)(ref this);
    }

    public unsafe uint GetPackSize()
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellow, uint>)6007600)(ref this);
    }

    public unsafe uint Pack(void** addr, uint size)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellow, void**, uint, uint>)6007632)(ref this, addr, size);
    }

    public unsafe int UnPack(void** addr, uint size)
    {
        return ((delegate* unmanaged[Thiscall]<ref Fellow, void**, uint, int>)6007824)(ref this, addr, size);
    }
}

public struct PackObj
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct Vtbl
    {
        public unsafe static delegate* unmanaged[Thiscall]<PackObj*, uint, void*> __vecDelDtor;
    }

    public unsafe Vtbl* vfptr;

    public unsafe override string ToString()
    {
        return $"vfptr:->(PackObj.Vtbl*)0x{(int)vfptr:X8}";
    }

    public unsafe static int ALIGN_PTR(void** ptr, uint* size)
    {
        return ((delegate* unmanaged[Cdecl]<void**, uint*, int>)5244432)(ptr, size);
    }

    public unsafe static uint ALIGN_PTR(void** ptr)
    {
        return ((delegate* unmanaged[Cdecl]<void**, uint>)5231024)(ptr);
    }

    public unsafe static uint GET_SIZE_LEFT(void* addr, void* start, uint size)
    {
        return ((delegate* unmanaged[Cdecl]<void*, void*, uint, uint>)5402000)(addr, start, size);
    }

    public unsafe uint GetPackSize()
    {
        return ((delegate* unmanaged[Thiscall]<ref PackObj, uint>)4198544)(ref this);
    }

    public unsafe static int UNPACK_TYPE(int* data_r, void** buffer_vpr, uint* size_r)
    {
        return ((delegate* unmanaged[Cdecl]<int*, void**, uint*, int>)5230976)(data_r, buffer_vpr, size_r);
    }

    public unsafe static int VERIFY_ADDR(void* addr, void* start, uint size)
    {
        return ((delegate* unmanaged[Cdecl]<void*, void*, uint, int>)5402032)(addr, start, size);
    }
}

public struct PackableHashTable<KEY, DATA> where KEY : unmanaged where DATA : unmanaged
{
    public PackObj a0;

    public int m_fThrowawayDuplicateKeysOnUnPack;

    public unsafe PackableHashData<KEY, DATA>** _buckets;

    public uint _table_size;

    public uint _currNum;

    public unsafe static delegate* unmanaged[Cdecl]<void*, void> operator_delete = (delegate* unmanaged[Cdecl]<void*, void>)6156638;

    public int Count => (int)_currNum;

    public bool IsReadOnly => false;

    public unsafe PackableHashData<KEY, DATA>* this[int index]
    {
        get
        {
            return GetByIndex(index);
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public unsafe PackableHashData<KEY, DATA>* lookup(KEY _key)
    {
        if (_table_size == 0 || _buckets == null)
        {
            return null;
        }

        PackableHashData<KEY, DATA>* ptr = _buckets[*(uint*)(&_key) % _table_size];
        if (ptr != null)
        {
            while (!_key.Equals(ptr->_key))
            {
                ptr = ptr->_next;
                if (ptr == null)
                {
                    return null;
                }
            }

            return ptr;
        }

        return null;
    }

    public unsafe bool Contains(KEY item)
    {
        return lookup(item) != null;
    }

    public unsafe PackableHashData<KEY, DATA>* GetByIndex(int _index)
    {
        if (_table_size == 0 || _buckets == null || _index < 0 || _index >= (int)_currNum)
        {
            return null;
        }

        int num = 0;
        int num2 = 0;
        PackableHashData<KEY, DATA>* ptr = _buckets[num2];
        bool flag = false;
        do
        {
            if (ptr != null)
            {
                if (num == _index)
                {
                    return ptr;
                }

                ptr = ptr->_next;
                num++;
            }

            if (ptr != null)
            {
                continue;
            }

            do
            {
                num2++;
                if (num2 >= _table_size)
                {
                    flag = true;
                    break;
                }

                ptr = _buckets[num2];
            }
            while (ptr == null);
        }
        while (!flag);
        return null;
    }

    public unsafe override string ToString()
    {
        return $"a0(PackObj):{a0}, m_fThrowawayDuplicateKeysOnUnPack(int):{m_fThrowawayDuplicateKeysOnUnPack}, _buckets:->(PackableHashData<UInt32,UInt32>**)0x{(int)_buckets:X8}, _table_size:{_table_size:X8}, _currNum:{_currNum:X8}";
    }

    //
    // Summary:
    //     copy to (pre-initialized) array.
    //
    // Parameters:
    //   array:
    //     pre initialized array to hold items from table
    //
    //   _index:
    //     index to start at (for chunking)
    public unsafe void CopyTo(PackableHashData<KEY, DATA>*[] array, int _index)
    {
        if (_table_size == 0 || _buckets == null || _index < 0 || _index >= (int)_currNum)
        {
            return;
        }

        int num = 0;
        int num2 = 0;
        int num3 = 0;
        PackableHashData<KEY, DATA>* ptr = _buckets[num2];
        bool flag = false;
        do
        {
            if (ptr != null)
            {
                if (num >= _index)
                {
                    if (num3 == array.Length)
                    {
                        break;
                    }

                    array[num3] = ptr;
                    num3++;
                }

                ptr = ptr->_next;
                num++;
            }

            if (ptr != null)
            {
                continue;
            }

            do
            {
                num2++;
                if (num2 >= _table_size)
                {
                    flag = true;
                    break;
                }

                ptr = _buckets[num2];
            }
            while (ptr == null);
        }
        while (!flag);
    }
}

public struct PackableHashData<KEY, DATA> where KEY : unmanaged where DATA : unmanaged
{
    public KEY _key;

    public DATA _data;

    public unsafe PackableHashData<KEY, DATA>* _next;

    public int _hashVal;

    public unsafe override string ToString()
    {
        return $"_key:{_key}, _data:({_data}), _next:->0x{(int)_next:X8}, _hashVal:{_hashVal}";
    }
}

public struct ClientSystem
{
    public Interface a0;

    public gmNoticeHandler gmnoticeHandler;
}

public struct _GUID
{
    public uint Data1;

    public ushort Data2;

    public ushort Data3;

    public unsafe fixed byte Data4[8];

    public unsafe override string ToString()
    {
        return $"{Data1:x8}-{Data2:x4}-{Data3:x4}-{Data4[0]:x2}{Data4[1]:x2}-{Data4[2]:x2}{Data4[3]:x2}{Data4[4]:x2}{Data4[5]:x2}{Data4[6]:x2}{Data4[7]:x2}";
    }
}


public struct Interface
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct Vtbl
    {
        public unsafe static delegate* unmanaged[Thiscall]<Interface*, _GUID*, void**, int> IUnknown_QueryInterface;

        public unsafe static delegate* unmanaged[Thiscall]<Interface*, uint> IUnknown_AddRef;

        public unsafe static delegate* unmanaged[Thiscall]<Interface*, uint> IUnknown_Release;

        public unsafe static delegate* unmanaged[Thiscall]<Interface*, TResult*, Turbine_GUID*, void**, TResult*> QueryInterface;

        public unsafe static delegate* unmanaged[Thiscall]<Interface*, uint> AddRef;

        public unsafe static delegate* unmanaged[Thiscall]<Interface*, uint> Release;
    }

    public unsafe Vtbl* vfptr;

    public unsafe override string ToString()
    {
        return $"vfptr:->(Interface.Vtbl*)0x{(int)vfptr:X8}";
    }

    public unsafe uint IUnknown_AddRef()
    {
        return ((delegate* unmanaged[Thiscall]<ref Interface, uint>)4201488)(ref this);
    }

    public unsafe int IUnknown_QueryInterface(_GUID* iid, void** ppvObject)
    {
        return ((delegate* unmanaged[Thiscall]<ref Interface, _GUID*, void**, int>)4201456)(ref this, iid, ppvObject);
    }

    public unsafe uint IUnknown_Release()
    {
        return ((delegate* unmanaged[Thiscall]<ref Interface, uint>)4201504)(ref this);
    }
}

public struct gmNoticeHandler
{
    public NoticeHandler a0;

    public override string ToString()
    {
        return $"a0(NoticeHandler):{a0}";
    }
}


public struct NoticeHandler
{
    public struct Vtbl
    {
        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, byte> IsEngine;

        public unsafe fixed byte gap4[8];

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, byte, uint, uint, void> RecvNotice_RuntimeDDDStatus;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, void> RecvNotice_ItemAttributesChanged;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, void> RecvNotice_ServerSaysAttemptFailed;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, uint, uint, uint, int, uint, uint, void> RecvNotice_ServerSaysMoveItem;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, void> RecvNotice_SetSelectedItem;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, CharError, void> RecvNotice_CharacterError;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, void> RecvNotice_ServerDied;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, PStringBase<char>*, void> RecvNotice_WorldName;

        //public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, CWeenieObject*, void> RecvNotice_BeingDeleted;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, void> RecvNotice_CreateObject;

        //public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, PropertyCollection*, void> RecvNotice_CloseDialog;

        //public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, StringInfo*, StringInfo*, uint, void> RecvNotice_DisplayFinalStringInfo;

        //public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, StringInfo*, void> RecvNotice_DisplayStringInfo;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, PStringBase<char>*, void> RecvNotice_DisplayWeenieError;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, void> RecvNotice_OpenDialog;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, void> RecvNotice_SmartBoxObjectFound;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, void> RecvNotice_TextTag_DIDClick;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, void> RecvNotice_TextTag_IIDClick;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, uint, void> RecvNotice_TextTag_IIDEnumClick;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, PStringBase<ushort>*, void> RecvNotice_TextTag_IIDStringClick;

        public unsafe static delegate* unmanaged[Thiscall]<NoticeHandler*, uint, uint, uint, uint, void> RecvNotice_UpdateGameView;
    }

    public unsafe Vtbl* vfptr;

    public unsafe override string ToString()
    {
        return $"vfptr:->(NoticeHandler.Vtbl*)0x{(int)vfptr:X8}";
    }

    //public unsafe void RecvNotice_DisplayFinalStringInfo(uint i_vendorID, VendorProfile* i_vendorProfile, PackableList<ItemProfile>* i_itemProfileList, ShopMode i_startMode)
    //{
    //    ((delegate* unmanaged[Thiscall]<ref NoticeHandler, uint, VendorProfile*, PackableList<ItemProfile>*, ShopMode, void>)4199920)(ref this, i_vendorID, i_vendorProfile, i_itemProfileList, i_startMode);
    //}

    public unsafe void RecvNotice_ServerSaysMoveItem(uint i_itemID, uint i_oldContainer, uint i_oldWielder, uint i_oldLocation, uint i_newContainer, int i_place, uint i_newWielder, uint i_newLocation)
    {
        ((delegate* unmanaged[Thiscall]<ref NoticeHandler, uint, uint, uint, uint, uint, int, uint, uint, void>)4199904)(ref this, i_itemID, i_oldContainer, i_oldWielder, i_oldLocation, i_newContainer, i_place, i_newWielder, i_newLocation);
    }
}

public struct TResult
{
    public uint m_val;

    public override string ToString()
    {
        return $"m_val:{m_val:X8}";
    }
}

public struct Turbine_GUID
{
    public uint m_data1;

    public ushort m_data2;

    public ushort m_data3;

    public unsafe fixed char m_data4[8];

    public unsafe override string ToString()
    {
        return $"{m_data1:x8}-{m_data2:x4}-{m_data3:x4}-{m_data4[0]:x2}{m_data4[1]:x2}-{m_data4[2]:x2}{m_data4[3]:x2}{m_data4[4]:x2}{m_data4[5]:x2}{m_data4[6]:x2}{m_data4[7]:x2}";
    }
}

public enum CharError : uint
{
    Char_ERROR_UNDEF = 0u,
    Char_ERROR_LOGON = 1u,
    Char_ERROR_LOGGED_ON = 2u,
    Char_ERROR_ACCOUNT_LOGON = 3u,
    Char_ERROR_SERVER_CRASH = 4u,
    Char_ERROR_LOGOFF = 5u,
    Char_ERROR_DELETE = 6u,
    Char_ERROR_NO_PREMADE = 7u,
    Char_ERROR_ACCOUNT_IN_USE = 8u,
    Char_ERROR_ACCOUNT_INVALID = 9u,
    Char_ERROR_ACCOUNT_DOESNT_EXIST = 10u,
    Char_ERROR_ENTER_GAME_GENERIC = 11u,
    Char_ERROR_ENTER_GAME_STRESS_ACCOUNT = 12u,
    Char_ERROR_ENTER_GAME_CharACTER_IN_WORLD = 13u,
    Char_ERROR_ENTER_GAME_PLAYER_ACCOUNT_MISSING = 14u,
    Char_ERROR_ENTER_GAME_CharACTER_NOT_OWNED = 15u,
    Char_ERROR_ENTER_GAME_CharACTER_IN_WORLD_SERVER = 16u,
    Char_ERROR_ENTER_GAME_OLD_CharACTER = 17u,
    Char_ERROR_ENTER_GAME_CORRUPT_CharACTER = 18u,
    Char_ERROR_ENTER_GAME_START_SERVER_DOWN = 19u,
    Char_ERROR_ENTER_GAME_COULDNT_PLACE_CharACTER = 20u,
    Char_ERROR_LOGON_SERVER_FULL = 21u,
    Char_ERROR_CharACTER_IS_BOOTED = 22u,
    Char_ERROR_ENTER_GAME_CharACTER_LOCKED = 23u,
    Char_ERROR_SUBSCRIPTION_EXPIRED = 24u,
    Char_ERROR_NUM_ERRORS = 25u,
    FORCE_CharError_32_BIT = 2147483647u
}
