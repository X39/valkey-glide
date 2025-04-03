using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct UInt32ArrayParameter(uint[] value) : IParameter
{
    public uint[] Value { get; } = value;

    public static implicit operator uint[](
        UInt32ArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator UInt32ArrayParameter(
        uint[] value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    )
    {
        var ptr = (uint*)marshalBytes(sizeof(uint) * Value.Length);
        for (var i = 0; i < Value.Length; i++)
        {
            var value = Value[i];
            ptr[i] = value;
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.UInt32Array,
            value = new ParameterValue {u32_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
