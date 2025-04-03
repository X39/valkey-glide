using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct BoolArrayParameter(bool[] value) : IParameter
{
    public bool[] Value { get; } = value;

    public static implicit operator bool[](
        BoolArrayParameter parameter
    ) => parameter.Value;

    public static implicit operator BoolArrayParameter(
        bool[] value
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
            ptr[i] = (byte)(value ? 1 : 0);
        }
        return new Native.Parameter.Parameter
        {
            kind = EParameterKind.BoolArray,
            value = new ParameterValue {flag_array = ptr},
        };
    }
    public override string ToString() => Value.ToString();
}
