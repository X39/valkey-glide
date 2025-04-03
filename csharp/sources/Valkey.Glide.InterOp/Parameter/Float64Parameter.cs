using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Float64Parameter(double value) : IParameter
{
    public double Value { get; } = value;

    public static implicit operator double(
        Float64Parameter parameter
    ) => parameter.Value;

    public static implicit operator Float64Parameter(
        double value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Float64,
        value = new ParameterValue {f64 = Value},
    };
    public override string ToString() => Value.ToString();
}
