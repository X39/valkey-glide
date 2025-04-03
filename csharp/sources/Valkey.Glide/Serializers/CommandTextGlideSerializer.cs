// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

using Valkey.Glide.Data;
using Valkey.Glide.InterOp.Parameter;

namespace Valkey.Glide.Serializers;

/// <summary>
/// A serializer implementation for converting <see cref="CommandText"/> objects
/// into their string representation for use within the Valkey Glide framework.
/// </summary>
public sealed class CommandTextGlideSerializer : IGlideSerializer<CommandText>
{
    public IParameter ToValkey(CommandText t) => t.Text.AsRedisCommandText();
}
