using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt16ArrayParameter(ushort[] value) : IParameter
{
    public ushort[] Value { get; } = value;

    public static implicit operator ushort[](
        UInt16ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator UInt16ArrayParameter(
        ushort[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (ushort*)marshalBytes(sizeof(ushort) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.UInt16Array,
            value = new ParameterValue {u16_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
