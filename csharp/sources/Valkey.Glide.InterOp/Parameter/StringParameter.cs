using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct StringParameter(string value) : IParameter
{
    public string Value { get; } = value;

    public static implicit operator string(
        StringParameter parameter
    ) => parameter.Value;

    public static implicit operator StringParameter(
        string value
    ) => new(value);

    public unsafe Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.String,
        value = new ParameterValue {string_ = marshalString(Value),},
        value_length = checked((uint)Value.Length),
    };
    public override string ToString() => Value.ToString();
}
