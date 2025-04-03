using Valkey.Glide.InterOp.Native.Parameter;

namespace Valkey.Glide.InterOp.Parameter;

public readonly struct Int64Parameter(long value) : IParameter
{
    public long Value { get; } = value;

    public static implicit operator long(
        Int64Parameter parameter
    ) => parameter.Value;

    public static implicit operator Int64Parameter(
        long value
    ) => new(value);

    public Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    ) => new()
    {
        kind = EParameterKind.Int64,
        value = new ParameterValue {i64 = Value},
    };
    public override string ToString() => Value.ToString();
}
