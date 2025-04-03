using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct BoolParameter(bool value) : IParameter
{
    public bool Value { get; } = value;

    public static implicit operator bool(
        BoolParameter parameter
    ) => parameter.Value;

    public static implicit operator BoolParameter(
        bool value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Bool,
        value = new ParameterValue {flag = (byte)(Value ? 1 : 0)},
    };
    public override string ToString() => Value.ToString();
}
