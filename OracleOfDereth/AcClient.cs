using OracleOfDereth;
using System;
using System.Collections.Generic;
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
