using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt64ArrayParameter(ulong[] value) : IParameter
{
    public ulong[] Value { get; } = value;

    public static implicit operator ulong[](
        UInt64ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator UInt64ArrayParameter(
        ulong[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (ulong*)marshalBytes(sizeof(ulong) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.UInt64Array,
            value = new ParameterValue {u64_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
