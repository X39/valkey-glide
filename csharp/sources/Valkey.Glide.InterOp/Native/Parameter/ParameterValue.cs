using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Valkey.Glide.InterOp.Native.Parameter;

[EditorBrowsable(EditorBrowsableState.Advanced)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
public unsafe struct ParameterValue {
    [FieldOffset(0)] public byte flag;
    [FieldOffset(0)] public sbyte i8;
    [FieldOffset(0)] public byte u8;
    [FieldOffset(0)] public short i16;
    [FieldOffset(0)] public ushort u16;
    [FieldOffset(0)] public int i32;
    [FieldOffset(0)] public uint u32;
    [FieldOffset(0)] public long i64;
    [FieldOffset(0)] public ulong u64;
    [FieldOffset(0)] public float f32;
    [FieldOffset(0)] public double f64;
    [FieldOffset(0)] public byte* string_;
    [FieldOffset(0)] public byte* flag_array;
    [FieldOffset(0)] public sbyte* i8_array;
    [FieldOffset(0)] public byte* u8_array;
    [FieldOffset(0)] public short* i16_array;
    [FieldOffset(0)] public ushort* u16_array;
    [FieldOffset(0)] public int* i32_array;
    [FieldOffset(0)] public uint* u32_array;
    [FieldOffset(0)] public long* i64_array;
    [FieldOffset(0)] public ulong* u64_array;
    [FieldOffset(0)] public float* f32_array;
    [FieldOffset(0)] public double* f64_array;
    [FieldOffset(0)] public KeyParameterPair* key_parameter_array;
}
