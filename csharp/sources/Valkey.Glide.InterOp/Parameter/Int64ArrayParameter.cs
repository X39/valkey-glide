using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int64ArrayParameter(long[] value) : IParameter
{
    public long[] Value { get; } = value;

    public static implicit operator long[](
        Int64ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator Int64ArrayParameter(
        long[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (long*)marshalBytes(sizeof(long) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.Int64Array,
            value = new ParameterValue {i64_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
