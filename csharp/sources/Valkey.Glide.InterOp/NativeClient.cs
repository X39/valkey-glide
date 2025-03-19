﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Valkey.Glide.InterOp.Exceptions;
using Valkey.Glide.InterOp.Native;

namespace Valkey.Glide.InterOp;

/// <summary>
/// Represents a client for interacting with native APIs, encapsulating operations on unmanaged resources
/// and providing functionality for sending commands and requests.
/// </summary>
/// <remarks>
/// This class ensures the proper management of unmanaged resources and provides both synchronous and asynchronous
/// methods for interacting with the native system. It should be disposed of correctly to release resources.
/// </remarks>
public sealed class NativeClient : IDisposable, INativeClient
{
    // Do not modify! We need to keep this reference at a root to prevent GC!
    private static readonly CommandCallbackDelegate CommandCallbackDel = CommandCallback;
    private static readonly nint CommandCallbackFptr = Marshal.GetFunctionPointerForDelegate(CommandCallbackDel);

    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static bool _initialized;
    private nint? _handle;

    internal const int SmallStringOptimizationArgs = 20;
    internal const int SmallStringOptimizationBuffer = 100;


    public unsafe NativeClient(ConnectionRequest request)
    {
        if (!_initialized)
            throw new InvalidOperationException("API is not initialized");
        request.Validate();

        List<nint> strings = new List<nint>();
        NodeAddress[] addresses = new NodeAddress[request.Addresses.Length];
        try
        {
            fixed (NodeAddress* addressesPtr = addresses)
            {
                Native.ConnectionRequest nativeRequest = new Native.ConnectionRequest
                {
                    client_name = MarshalCollectingString(request.ClientName),
                    cluster_mode_enabled = request.ClusterMode ? 1 : 0,
                    connection_retry_strategy = new Native.ConnectionRetryStrategy
                    {
                        ignore = !request.ConnectionRetryStrategy.HasValue ? 1 : default,
                        number_of_retries = request.ConnectionRetryStrategy?.NumberOfRetries ?? default,
                        exponent_base = request.ConnectionRetryStrategy?.ExponentialBase ?? default,
                        factor = request.ConnectionRetryStrategy?.Factor ?? default,
                    },
                    connection_timeout =
                        new OptionalU32
                        {
                            ignore = !request.ConnectionTimeout.HasValue ? 1 : 0,
                            value = (uint?)request.ConnectionTimeout?.TotalMilliseconds ?? default
                        },
                    otel_span_flush_interval_ms =
                        new OptionalU64
                        {
                            ignore = !request.OpenTelemetrySpanFlushInterval.HasValue ? 1 : 0,
                            value = (ulong?)request.OpenTelemetrySpanFlushInterval?.TotalMilliseconds ?? default
                        },
                    request_timeout =
                        new OptionalU32
                        {
                            ignore = !request.RequestTimeout.HasValue ? 1 : 0,
                            value = (uint?)request.RequestTimeout?.TotalMilliseconds ?? default
                        },
                    inflight_requests_limit = new OptionalU32
                    {
                        ignore = !request.InflightRequestsLimit.HasValue ? 1 : 0,
                        value = request.InflightRequestsLimit ?? default
                    },
                    periodic_checks = new Native.PeriodicCheck
                    {
                        kind = request.PeriodicChecks?.Kind switch
                        {
                            EPeriodicCheckKind.Enabled => Native.EPeriodicCheckKind.Enabled,
                            EPeriodicCheckKind.Disabled => Native.EPeriodicCheckKind.Disabled,
                            EPeriodicCheckKind.ManualInterval => Native.EPeriodicCheckKind.ManualInterval,
                            null => Native.EPeriodicCheckKind.None,
                            _ => throw new ArgumentOutOfRangeException(),
                        }
                    },
                    protocol = request.Protocol switch
                    {
                        EProtocolVersion.Resp2 => Native.EProtocolVersion.RESP2,
                        EProtocolVersion.Resp3 => Native.EProtocolVersion.RESP3,
                        null => Native.EProtocolVersion.None,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    tls_mode = request.TlsMode switch
                    {
                        ETlsMode.NoTls => Native.ETlsMode.None,
                        ETlsMode.InsecureTls => Native.ETlsMode.InsecureTls,
                        ETlsMode.SecureTls => Native.ETlsMode.SecureTls,
                        null => Native.ETlsMode.None,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    auth_password = MarshalCollectingString(request.AuthPassword),
                    auth_username = MarshalCollectingString(request.AuthUsername),
                    otel_endpoint = MarshalCollectingString(request.OpenTelemetryEndpoint),
                    read_from = new Native.ReadFrom
                    {
                        kind = request.ReplicationStrategy?.Kind switch
                        {
                            EReadFromKind.Primary => Native.EReadFromKind.Primary,
                            EReadFromKind.PreferReplica => Native.EReadFromKind.PreferReplica,
                            EReadFromKind.AzAffinity => Native.EReadFromKind.AZAffinity,
                            EReadFromKind.AzAffinityReplicasAndPrimary => Native.EReadFromKind
                                .AZAffinityReplicasAndPrimary,
                            null => Native.EReadFromKind.None,
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        value = MarshalCollectingString(request.ReplicationStrategy?.AvailabilityZone),
                    },
                    database_id = request.DatabaseId,
                    addresses_length = (uint)request.Addresses.LongLength,
                    addresses = addressesPtr,
                };
                for (int i = 0; i < request.Addresses.Length; i++)
                {
                    Node host = request.Addresses[i];
                    addresses[i] = new NodeAddress {host = MarshalCollectingString(host.Address), port = host.Port,};
                }

                CreateClientHandleResult result = Imports.create_client_handle(nativeRequest);
                switch (result.result)
                {
                    case ECreateClientHandleCode.Success:
                        if (result.client_handle == 0)
                            throw new Exception("Unknown Error, ClientHandle is NULL");
                        _handle = result.client_handle;
                        break;
                    case ECreateClientHandleCode.ParameterError:
                        if (result.error_string is null)
                            throw new ParameterException("Unknown parameter exception");
                        throw new ParameterException(
                            HandleString(result.error_string) ?? "Unknown parameter exception"
                        );
                    case ECreateClientHandleCode.ThreadCreationError:
                        if (result.error_string is null)
                            throw new ThreadCreationException("Unknown thread creation exception");
                        throw new ThreadCreationException(
                            HandleString(result.error_string) ?? "Unknown thread creation exception"
                        );
                    case ECreateClientHandleCode.ConnectionTimedOutError:
                        if (result.error_string is null)
                            throw new ConnectionToTimeOutException("Unknown connection timeout exception");
                        throw new ConnectionToTimeOutException(
                            HandleString(result.error_string) ?? "Unknown connection timeout exception"
                        );
                    case ECreateClientHandleCode.ConnectionToFailedError:
                        if (result.error_string is null)
                            throw new ConnectionToFailedException("Unknown connection to failed exception");
                        throw new ConnectionToFailedException(
                            HandleString(result.error_string) ?? "Unknown connection to failed exception"
                        );
                    case ECreateClientHandleCode.ConnectionToClusterFailed:
                        if (result.error_string is null)
                            throw new ConnectionToClusterException("Unknown connection to cluster exception");
                        throw new ConnectionToClusterException(
                            HandleString(result.error_string) ?? "Unknown connection to cluster exception"
                        );
                    case ECreateClientHandleCode.ConnectionIoError:
                        if (result.error_string is null)
                            throw new ConnectionIoException("Unknown io exception");
                        throw new ConnectionIoException(HandleString(result.error_string) ?? "Unknown io exception");
                    default:
                        if (result.error_string is null)
                            throw new Exception("Unknown error");
                        throw new Exception(HandleString(result.error_string));
                }
            }
        }
        finally
        {
            foreach (byte* ptr in strings)
            {
                MarshalFreeUtf8String(ptr);
            }
        }

        byte* MarshalCollectingString(string? input)
        {
            if (input is null)
                return null;
            byte* ptr = MarshalUtf8String(input);
            strings.Add((nint)ptr);
            return ptr;
        }
    }

    /// <summary>
    /// Initializes the API.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>This method is safe to be called multiple times but will not have any effect on successive calls.</item>
    /// <item>This method will lock until it is free. Do not use in a hot path!</item>
    /// </list>
    /// </remarks>
    public static unsafe void Initialize(ELoggerLevel loggerLevel = ELoggerLevel.None, string? logFilePath = null)
    {
        Semaphore.Wait();
        try
        {
            if (_initialized)
                return;
            if (logFilePath is not null)
            {
                fixed (char* logFilePathPtr = logFilePath)
                {
                    InitResult result = Imports.system_init(loggerLevel, (byte*)logFilePathPtr);
                    if (result.success == 0 /* is false */)
                        throw new GlideException("Failed to initialize the API.");
                }
            }
            else
            {
                InitResult result = Imports.system_init(loggerLevel, null);
                if (result.success == 0 /* is false */)
                    throw new GlideException("Failed to initialize the API.");
            }

            _initialized = true;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static unsafe void MarshalFreeUtf8String(byte* buffer)
    {
        if (buffer is null)
            return;
        nint ptr = (nint)buffer - 1;
        if (Marshal.ReadByte(ptr) == 0) // Is allocated on heap
            Marshal.FreeCoTaskMem(ptr);
    }

    private static unsafe byte* MarshalUtf8String(string s, byte* buffer = null, int bufferLength = 0)
    {
        if (buffer is null && bufferLength is not 0)
            throw new ArgumentException("Buffer is null and bufferLength is not 0", nameof(buffer));
        byte[] utf8 = Encoding.UTF8.GetBytes(s);
        if (bufferLength is 0 || utf8.Length > bufferLength - 2)
        {
            nint ptr = Marshal.AllocHGlobal(utf8.Length + 2);
            fixed (byte* utf8Ptr = utf8)
            {
                Marshal.WriteByte(ptr, 0); // Is allocated on heap
                for (int i = 0; i < utf8.Length; i++)
                    Marshal.WriteByte(ptr + i + 1, utf8Ptr[i]);
                Marshal.WriteByte(ptr + utf8.Length + 1, 0);
            }

            return (byte*)ptr + 1;
        }
        else
        {
            nint ptr = (nint)buffer;
            fixed (byte* utf8Ptr = utf8)
            {
                Marshal.WriteByte(ptr, 1); // Is allocated on buffer (potentially stack)
                for (int i = 0; i < utf8.Length; i++)
                    Marshal.WriteByte(ptr + i + 1, utf8Ptr[i]);
                Marshal.WriteByte(ptr + utf8.Length + 1, 0);
            }

            return buffer + 1;
        }
    }

    private static unsafe string? HandleString(byte* resultErrorString, int? length = null, bool free = true)
    {
        if (resultErrorString is null)
            return null;
        try
        {
            int len = length ?? Strlen(resultErrorString);
            return Encoding.UTF8.GetString(resultErrorString, len);
        }
        finally
        {
            if (free)
                Imports.free_string(resultErrorString);
        }
    }

    private static unsafe int Strlen(byte* input)
    {
        int i = 0;
        for (; input[i] != 0; i++)
            ;
        return i;
    }

    private delegate void CommandCallbackDelegate([In] nint data, [In] int success, [In] Native.Value payload);

    private static void CommandCallback([In] nint data, [In] int success, [In] Native.Value payload)
    {
        try
        {
            GCHandle dataHandle = GCHandle.FromIntPtr(data);
            TaskCompletionSource<Value>? commandCallbackData;
            try
            {
                commandCallbackData = (TaskCompletionSource<Value>)dataHandle.Target;
            }
            finally
            {
                dataHandle.Free();
            }

            Value value = FromNativeValue(payload);
            if (success != 0 /* is true */)
                commandCallbackData.SetResult(value);
            else if (value.IsString(out string message))
                commandCallbackData.SetException(new Exception(message));
            else
                commandCallbackData.SetException(new Exception("Unknown error"));
        }
        catch (Exception)
        {
            // empty
        }
    }

    private void ReleaseUnmanagedResources()
    {
        lock (this)
        {
            if (_handle.HasValue)
                Imports.free_client_handle(_handle.Value);
            _handle = null;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~NativeClient()
    {
        ReleaseUnmanagedResources();
    }

    public unsafe Task<Value> SendCommandAsync(ERequestType requestType, params string[] args)
    {
        if (_handle is null)
            throw new ObjectDisposedException(nameof(NativeClient), "ClientHandle is null");
        TaskCompletionSource<Value> tcs =
            new TaskCompletionSource<Value>(TaskCreationOptions.RunContinuationsAsynchronously);

        GCHandle dataHandle = GCHandle.Alloc(tcs, GCHandleType.Normal);
        try
        {
            byte*[] argsArr = new byte*[args.Length];
            try
            {
                if (args.Length <= SmallStringOptimizationArgs)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        // ReSharper disable once StackAllocInsideLoop
                        // We do this intentionally here in a "low allocation" (max: 20 * 100 bytes) environment
                        byte* buffer = stackalloc byte[SmallStringOptimizationBuffer];
                        byte* ptr = MarshalUtf8String(args[i], buffer, SmallStringOptimizationBuffer);
                        argsArr[i] = ptr;
                    }
                }
                else
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        byte* ptr = MarshalUtf8String(args[i]);
                        argsArr[i] = ptr;
                    }
                }

                CommandResult result;
                fixed (byte** argsArrPtr = argsArr)
                    result = Imports.command(
                        _handle.Value,
                        CommandCallbackFptr,
                        GCHandle.ToIntPtr(dataHandle),
                        requestType,
                        argsArrPtr,
                        argsArr.Length
                    );

                if (result.success != 0 /* is true */)
                    return tcs.Task;
                else
                    throw new Exception(HandleString(result.error_string));
            }
            finally
            {
                foreach (byte* t in argsArr)
                {
                    MarshalFreeUtf8String(t);
                }
            }
        }
        catch
        {
            dataHandle.Free();
            throw;
        }
    }

    public unsafe Value SendCommand(ERequestType requestType, params string[] args)
    {
        if (_handle is null)
            throw new ObjectDisposedException(nameof(NativeClient), "ClientHandle is null");

        byte*[] argsArr = new byte*[args.Length];
        if (args.Length <= 20)
        {
            for (int i = 0; i < args.Length; i++)
            {
                // ReSharper disable once StackAllocInsideLoop
                // We do this intentionally here in a "low allocation" (max: 20 * 100 bytes) environment
                byte* buffer = stackalloc byte[100];
                byte* ptr = MarshalUtf8String(args[i], buffer, 100);
                argsArr[i] = ptr;
            }
        }
        else
        {
            for (int i = 0; i < args.Length; i++)
            {
                byte* ptr = MarshalUtf8String(args[i]);
                argsArr[i] = ptr;
            }
        }

        BlockingCommandResult result;
        fixed (byte** argsArrPtr = argsArr)
            result = Imports.command_blocking(_handle.Value, requestType, argsArrPtr, argsArr.Length);
        foreach (byte* arg in argsArr)
        {
            MarshalFreeUtf8String(arg);
        }

        if (result.success != 0 /* is true */)
            return FromNativeValue(result.value);
        else
            throw new Exception(HandleString(result.error_string));
    }

    private static unsafe Value FromNativeValue(Native.Value input, bool free = true)
    {
        try
        {
            switch (input.kind)
            {
                case Native.EValueKind.Nil:
                    return Value.CreateNone();
                case Native.EValueKind.Int:
                    return Value.CreateInteger(input.data.i);
                case Native.EValueKind.BulkString:
                    return Value.CreateString(HandleString(input.data.ptr, (int)input.length, false));
                case Native.EValueKind.Array:
                    {
                        Value[] array = new Value[input.length];
                        Native.Value* ptr = (Native.Value*)input.data.ptr;
                        for (int i = 0; i < input.length; i++)
                        {
                            array[i] = FromNativeValue(ptr[i], false);
                        }

                        return Value.CreateArray(array);
                    }
                case Native.EValueKind.SimpleString:
                    return InterOp.Value.CreateString(HandleString(input.data.ptr, (int)input.length, false));
                case Native.EValueKind.Okay:
                    return Value.CreateOkay();
                case Native.EValueKind.Map:
                    {
                        KeyValuePair<Value, Value>[] array = new KeyValuePair<Value, Value>[input.length];
                        Native.Value* ptr = (Native.Value*)input.data.ptr;
                        for (int i = 0, j = 0; i < input.length; i++, j += 2)
                        {
                            Value key = FromNativeValue(ptr[j], false);
                            Value value = FromNativeValue(ptr[j + 1], false);
                            array[i] = new KeyValuePair<Value, Value>(key, value);
                        }

                        return Value.CreatePairs(array);
                    }
                case Native.EValueKind.Attribute:
                    throw new NotImplementedException();
                case Native.EValueKind.Set:
                    {
                        Value[] array = new Value[input.length];
                        Native.Value* ptr = (Native.Value*)input.data.ptr;
                        for (int i = 0; i < input.length; i++)
                        {
                            array[i] = FromNativeValue(ptr[i], false);
                        }

                        return Value.CreateArray(array);
                    }
                case Native.EValueKind.Double:
                    return Value.CreateFloatingPoint(input.data.f);
                case Native.EValueKind.Boolean:
                    return Value.CreateBoolean(input.data.i != 0);
                case Native.EValueKind.VerbatimString:
                    {
                        StringPair* ptr = (StringPair*)input.data.ptr;
                        string? key = HandleString(ptr->a_start, (int)(ptr->a_end - ptr->a_start), false);
                        string? value = HandleString(ptr->a_start, (int)(ptr->a_end - ptr->a_start), false);

                        return Value.CreateFormatString(key, value);
                    }
                case Native.EValueKind.BigNumber:
                    throw new NotImplementedException();
                case Native.EValueKind.Push:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            if (free)
                Imports.free_value(input);
        }
    }
}
