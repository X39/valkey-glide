using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int16Parameter(short value) : IParameter
{
    public short Value { get; } = value;

    public static implicit operator short(
        Int16Parameter parameter
    ) => parameter.Value;

    public static implicit operator Int16Parameter(
        short value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Int16,
        value = new ParameterValue {i16 = Value},
    };
    public override string ToString() => Value.ToString();
}
