using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int16ArrayParameter(short[] value) : IParameter
{
    public short[] Value { get; } = value;

    public static implicit operator short[](
        Int16ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator Int16ArrayParameter(
        short[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (short*)marshalBytes(sizeof(short) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.Int16Array,
            value = new ParameterValue {i16_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
