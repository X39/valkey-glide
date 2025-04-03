using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt8ArrayParameter(byte[] value) : IParameter
{
    public byte[] Value { get; } = value;

    public static implicit operator byte[](
        UInt8ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator UInt8ArrayParameter(
        byte[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (byte*)marshalBytes(sizeof(byte) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.UInt8Array,
            value = new ParameterValue {u8_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
