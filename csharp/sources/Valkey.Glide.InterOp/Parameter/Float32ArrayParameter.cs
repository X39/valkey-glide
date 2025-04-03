using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Float32ArrayParameter(float[] value) : IParameter
{
    public float[] Value { get; } = value;

    public static implicit operator float[](
        Float32ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator Float32ArrayParameter(
        float[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (float*)marshalBytes(sizeof(float) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.Float32Array,
            value = new ParameterValue {f32_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
