// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

using System.ComponentModel;

namespace Valkey.Glide.InterOp.Parameter;

/// <summary>
/// This is part of the internal API and subject to change without notice.
/// Do not derive from this interface.
/// </summary>
public interface IParameter
{
    /// <summary>
    /// This is part of the internal API and subject to change without notice.
    /// Do not use this method.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    Native.Parameter.Parameter ToNative(
        MarshalString marshalString,
        MarshalBytes marshalBytes
    );
}
