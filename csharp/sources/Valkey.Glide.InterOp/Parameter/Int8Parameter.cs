using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int8Parameter(sbyte value) : IParameter
{
    public sbyte Value { get; } = value;

    public static implicit operator sbyte(
        Int8Parameter parameter
    ) => parameter.Value;

    public static implicit operator Int8Parameter(
        sbyte value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Int8,
        value = new ParameterValue {i8 = Value},
    };
    public override string ToString() => Value.ToString();
}
