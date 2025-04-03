using System.Runtime.CompilerServices;
using Valkey.Glide.InterOp.Parameter;

namespace Valkey.Glide;

internal static class InternalExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static StringParameter AsRedisInteger(this string value) => value;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static StringParameter AsRedisCommandText(this string value) => value;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static StringParameter AsRedisString(this string value) => value;
}
