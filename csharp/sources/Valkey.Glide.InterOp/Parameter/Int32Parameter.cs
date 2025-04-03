using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int32Parameter(int value) : IParameter
{
    public int Value { get; } = value;

    public static implicit operator int(
        Int32Parameter parameter
    ) => parameter.Value;

    public static implicit operator Int32Parameter(
        int value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Int32,
        value = new ParameterValue {i32 = Value},
    };
    public override string ToString() => Value.ToString();
}
