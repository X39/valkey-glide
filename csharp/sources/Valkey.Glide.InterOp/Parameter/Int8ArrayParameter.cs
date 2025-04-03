using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int8ArrayParameter(sbyte[] value) : IParameter
{
    public sbyte[] Value { get; } = value;

    public static implicit operator sbyte[](
        Int8ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator Int8ArrayParameter(
        sbyte[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (sbyte*)marshalBytes(sizeof(sbyte) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.Int8Array,
            value = new ParameterValue {i8_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
