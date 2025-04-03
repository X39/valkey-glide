// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

using Valkey.Glide;
using Valkey.Glide.InterOp.Parameter;

namespace CustomSerializers;

public class DateTimeGlideSerializer : IGlideSerializer<DateTime>
{
    public IParameter ToValkey(DateTime t) => new StringParameter(t.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
}
