using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Float32Parameter(float value) : IParameter
{
    public float Value { get; } = value;

    public static implicit operator float(
        Float32Parameter parameter
    ) => parameter.Value;

    public static implicit operator Float32Parameter(
        float value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Float32,
        value = new ParameterValue {f32 = Value},
    };
    public override string ToString() => Value.ToString();
}
