using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Float64ArrayParameter(double[] value) : IParameter
{
    public double[] Value { get; } = value;

    public static implicit operator double[](
        Float64ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator Float64ArrayParameter(
        double[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (double*)marshalBytes(sizeof(double) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.Float64Array,
            value = new ParameterValue {f64_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
