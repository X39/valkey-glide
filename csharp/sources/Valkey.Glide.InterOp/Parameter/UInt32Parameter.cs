using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt32Parameter(uint value) : IParameter
{
    public uint Value { get; } = value;

    public static implicit operator uint(
        UInt32Parameter parameter
    ) => parameter.Value;

    public static implicit operator UInt32Parameter(
        uint value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.UInt32,
        value = new ParameterValue {u32 = Value},
    };
    public override string ToString() => Value.ToString();
}
