using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int32ArrayParameter(int[] value) : IParameter
{
    public int[] Value { get; } = value;

    public static implicit operator int[](
        Int32ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator Int32ArrayParameter(
        int[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (int*)marshalBytes(sizeof(int) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.Int32Array,
            value = new ParameterValue {i32_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
