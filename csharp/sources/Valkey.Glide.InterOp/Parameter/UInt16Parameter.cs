using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt16Parameter(ushort value) : IParameter
{
    public ushort Value { get; } = value;

    public static implicit operator ushort(
        UInt16Parameter parameter
    ) => parameter.Value;

    public static implicit operator UInt16Parameter(
        ushort value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.UInt16,
        value = new ParameterValue {u16 = Value},
    };
    public override string ToString() => Value.ToString();
}
