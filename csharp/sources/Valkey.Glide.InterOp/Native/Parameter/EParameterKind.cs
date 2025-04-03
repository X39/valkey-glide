using System.Diagnostics.CodeAnalysis;

namespace Valkey.Glide.InterOp.Native.Parameter;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum EParameterKind {
    Bool,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float32,
    Float64,
    String,
    BoolArray,
    Int8Array,
    UInt8Array,
    Int16Array,
    UInt16Array,
    Int32Array,
    UInt32Array,
    Int64Array,
    UInt64Array,
    Float32Array,
    Float64Array,
    KeyValueArray, // ToDo: Implement (hashset support)
}
