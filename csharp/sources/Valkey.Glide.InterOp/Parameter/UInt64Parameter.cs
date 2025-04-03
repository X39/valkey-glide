using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt64Parameter(ulong value) : IParameter
{
    public ulong Value { get; } = value;

    public static implicit operator ulong(
        UInt64Parameter parameter
    ) => parameter.Value;

    public static implicit operator UInt64Parameter(
        ulong value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.UInt64,
        value = new ParameterValue {u64 = Value},
    };

    public override string ToString() => Value.ToString();
}
