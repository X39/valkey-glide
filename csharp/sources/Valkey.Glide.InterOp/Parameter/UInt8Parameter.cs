using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt8Parameter(byte value) : IParameter
{
    public byte Value { get; } = value;

    public static implicit operator byte(
        UInt8Parameter parameter
    ) => parameter.Value;

    public static implicit operator UInt8Parameter(
        byte value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.UInt8,
        value = new ParameterValue {u8 = Value},
    };
    public override string ToString() => Value.ToString();
}
