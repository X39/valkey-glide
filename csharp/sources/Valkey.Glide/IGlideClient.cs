using Valkey.Glide.InterOp.Native;
using Valkey.Glide.InterOp.Parameter;
using Valkey.Glide.InterOp.Routing;

namespace Valkey.Glide;

public interface IGlideClient : InterOp.INativeClient
{
    IParameter ToParameter<T>(T value);
}
