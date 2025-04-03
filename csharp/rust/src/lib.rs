// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

extern crate core;

mod apihandle;
mod buffering;
mod conreq;
mod data;
mod helpers;
mod logging;
mod parameter;
mod routing;
mod value;

use crate::apihandle::{CommandParameter, Handle};
use crate::buffering::FFIBuffer;
use crate::conreq::ConnectionRequest;
use crate::data::*;
use crate::parameter::Parameter;
use crate::value::Value;
use glide_core::client::ConnectionError;
use glide_core::request_type::RequestType;
use logger_core::{LazyRollingFileAppender, Reloads, INITIATE_ONCE};
use std::ffi::{c_int, c_void, CString};
use std::os::raw::c_char;
use std::panic::catch_unwind;
use std::ptr::null;
use std::str::FromStr;
use std::sync::RwLock;
use tokio::runtime::Builder;
use tracing_core::LevelFilter;
use tracing_subscriber::layer::SubscriberExt;
use tracing_subscriber::util::SubscriberInitExt;
use tracing_subscriber::{filter, reload, Layer};

/// # Summary
/// Special method to free the returned values.
/// MUST be used!
#[no_mangle]
pub unsafe extern "C-unwind" fn csharp_free_value(_input: Value) {
    // We use this just to make the pattern more "future-proof".
    // Right now, no freeing is done here
}
/// # Summary
/// Special method to free the returned strings.
/// MUST be used instead of calling c-free!
#[no_mangle]
pub unsafe extern "C-unwind" fn csharp_free_string(input: *const c_char) {
    logger_core::log_trace("csharp_ffi", "Entered csharp_free_string");
    let str = CString::from_raw(input as *mut c_char);
    drop(str);
    logger_core::log_trace("csharp_ffi", "Exiting csharp_free_string");
}

/// # Summary
/// Sets the provided callbacks for logging.
///
/// # Remarks
/// The provided data and callbacks cannot be freed once set!
/// Consecutive calls will hence leak!
#[no_mangle]
pub extern "C-unwind" fn csharp_set_logging_hooks(
    data: *mut c_void,
    is_enabled_callback: logging::IsEnabledCallback,
    new_spawn_callback: logging::NewSpawnCallback,
    record_callback: logging::RecordCallback,
    event_callback: logging::EventCallback,
    enter_callback: logging::EnterCallback,
    exit_callback: logging::ExitCallback,
) {
    if INITIATE_ONCE.init_once.get().is_none() {
        INITIATE_ONCE.init_once.get_or_init(|| {
            // ToDo: Rework result so this hacky way of shutting up things is no longer necessary

            let stdout_fmt = tracing_subscriber::fmt::layer().with_filter(LevelFilter::OFF);
            let file_appender = LazyRollingFileAppender::stfu();
            let file_fmt = tracing_subscriber::fmt::layer()
                .with_writer(file_appender)
                .with_filter(LevelFilter::OFF);

            let (_, stdout_reload) = reload::Layer::new(stdout_fmt);
            let (_, file_reload) = reload::Layer::new(file_fmt);

            let subscriber = logging::CallbackSubscriber {
                data,
                is_enabled_callback,
                new_spawn_callback,
                record_callback,
                event_callback,
                enter_callback,
                exit_callback,
            };
            let targets_filter = filter::Targets::new()
                .with_target("glide", LevelFilter::TRACE)
                .with_target("redis", LevelFilter::TRACE)
                .with_target("logger_core", LevelFilter::TRACE)
                .with_target(std::env!("CARGO_PKG_NAME"), LevelFilter::TRACE);
            tracing_subscriber::registry()
                .with(subscriber)
                .with(targets_filter)
                .init();

            let reloads: Reloads = Reloads {
                console_reload: RwLock::new(stdout_reload),
                file_reload: RwLock::new(file_reload),
            };
            reloads
        });
    };
}

/// # Summary
/// Creates a new client to the given address.
///
/// # Input Safety (in_...)
/// The data passed in is considered "caller responsibility".
/// Any pointers hence will be left unreleased after leaving.
///
/// # Output Safety (out_... / return ...)
/// The data returned is considered "caller responsibility".
/// The caller must release any non-null pointers.
///
/// # Reference Safety (ref_...)
/// Any reference data is considered "caller owned".
///
/// # Freeing data allocated by the API
/// To free data returned by the API, use the corresponding `free_...` methods of the API.
/// It is **not optional** to call them to free data allocated by the API!
#[no_mangle]
pub extern "C-unwind" fn csharp_create_client_handle(
    in_connection_request: ConnectionRequest,
) -> CreateClientHandleResult {
    let request = match in_connection_request.to_redis() {
        Ok(d) => d,
        Err(e) => match e {
            Utf8OrEmptyError::Utf8Error(e) => {
                return CreateClientHandleResult {
                    result: ECreateClientHandleCode::ParameterError,
                    client_handle: null(),
                    error_string: match CString::from_str(e.to_string().as_str()) {
                        Ok(d) => d.into_raw(),
                        Err(_) => null(),
                    },
                }
            }
            Utf8OrEmptyError::Empty => {
                return CreateClientHandleResult {
                    result: ECreateClientHandleCode::ParameterError,
                    client_handle: null(),
                    error_string: match CString::from_str("Null value passed for host") {
                        Ok(d) => d.into_raw(),
                        Err(_) => null(),
                    },
                }
            }
        },
    };

    let runtime = match Builder::new_multi_thread()
        .enable_all()
        .thread_name("GLIDE C# thread")
        .build()
    {
        Ok(d) => d,
        Err(e) => {
            return CreateClientHandleResult {
                result: ECreateClientHandleCode::ThreadCreationError,
                client_handle: null(),
                error_string: match CString::from_str(e.to_string().as_str()) {
                    Ok(d) => d.into_raw(),
                    Err(_) => null(),
                },
            }
        }
    };
    let handle: Handle;
    {
        let _runtime_handle = runtime.enter();
        handle = match runtime.block_on(Handle::create(request)) {
            Ok(d) => d,
            Err(e) => {
                let str = e.to_string();
                return CreateClientHandleResult {
                    result: match e {
                        // ToDo: Improve error return codes even further to get more fine control at dotnet side
                        ConnectionError::Standalone(_) => {
                            ECreateClientHandleCode::ConnectionToFailedError
                        }
                        ConnectionError::Cluster(_) => {
                            ECreateClientHandleCode::ConnectionToClusterFailed
                        }
                        ConnectionError::Timeout => {
                            ECreateClientHandleCode::ConnectionTimedOutError
                        }
                        ConnectionError::IoError(_) => ECreateClientHandleCode::ConnectionIoError,
                    },
                    client_handle: null(),
                    error_string: match CString::from_str(str.as_str()) {
                        Ok(d) => d.into_raw(),
                        Err(_) => null(),
                    },
                };
            }
        };
    }
    CreateClientHandleResult {
        result: ECreateClientHandleCode::Success,
        client_handle: Box::into_raw(Box::new(FFIHandle { runtime, handle })) as *const c_void,
        error_string: null(),
    }
}

/// # Summary
/// Frees the previously created client_handle, making it unusable.
///
/// # Input Safety (in_...)
/// The data passed in is considered "caller responsibility".
/// Any pointers hence will be left unreleased after leaving.
///
/// # Output Safety (out_... / return ...)
/// The data returned is considered "caller responsibility".
/// The caller must release any non-null pointers.
///
/// # Reference Safety (ref_...)
/// Any reference data is considered "caller owned".
///
/// # Freeing data allocated by the API
/// To free data returned by the API, use the corresponding `free_...` methods of the API.
/// It is **not optional** to call them to free data allocated by the API!
#[no_mangle]
pub extern "C-unwind" fn csharp_free_client_handle(in_client_ptr: *const c_void) {
    logger_core::log_trace("csharp_ffi", "Entered csharp_free_client_handle");
    let client_ptr = unsafe { Box::from_raw(in_client_ptr as *mut FFIHandle) };
    let _runtime_handle = client_ptr.runtime.enter();
    drop(client_ptr);
    logger_core::log_trace("csharp_ffi", "Exiting csharp_free_client_handle");
}

/// # Summary
/// Method to invoke a command.
///
/// # Params
/// ***in_client_ptr*** An active client handle
/// ***in_callback*** A callback method with the signature:
///                   `void Callback(void * in_data, int out_success, const Value value)`.
///                   The first arg contains the data of the parameter *in_callback_data*;
///                   the second arg indicates whether the third parameter contains the error or result;
///                   the third arg contains either the result and MUST be freed by the callback.
/// ***in_callback_data*** The data to be passed in to *in_callback*.
/// ***in_request_type*** The type of command to issue.
/// ***in_routing_info*** Either nullptr or the routing info to use for the command.
/// ***in_args*** An array of arguments to be passed, with the size of `in_args_count`.
/// ***in_args_count*** The number of arguments in *in_args*.
///
/// # Input Safety (in_...)
/// The data passed in is considered "caller responsibility".
/// Any pointers hence will be left unreleased after leaving.
///
/// # Output Safety (out_... / return ...)
/// The data returned is considered "caller responsibility".
/// The caller must release any non-null pointers.
///
/// # Reference Safety (ref_...)
/// Any reference data is considered "caller owned".
///
/// # Freeing data allocated by the API
/// To free data returned by the API, use the corresponding `free_...` methods of the API.
/// It is **not optional** to call them to free data allocated by the API!
#[no_mangle]
pub extern "C-unwind" fn csharp_command(
    in_client_ptr: *const c_void,
    in_callback: CommandCallback,
    in_callback_data: *mut c_void,
    in_request_type: RequestType,
    in_routing_info: *const routing::RoutingInfo,
    // ToDo: Rework into parameter struct (understand how Command.arg(...) works first)
    //       handling the different input types.
    in_args: *const Parameter,
    in_args_count: c_int,
    // ToDo: Pass in ActivityContext and connect C# OTEL with Rust OTEL
) -> CommandResult {
    logger_core::log_trace("csharp_ffi", "Entered csharp_command");
    if in_client_ptr.is_null() {
        logger_core::log_error(
            "csharp_ffi",
            "Error in csharp_command called with null handle",
        );
        return CommandResult::new_error(helpers::to_cstr_ptr_or_null("Null handle passed"));
    }
    let args = match helpers::grab_vec(in_args, in_args_count as usize, |el| {
        Ok::<CommandParameter, Utf8OrEmptyError>(unsafe { el.to_command_parameter() }?)
    }) {
        Ok(d) => d,
        Err(e) => {
            logger_core::log_error(
                "csharp_ffi",
                format!("Error in string transformation: {:?}", e.to_string()),
            );
            return match e {
                Utf8OrEmptyError::Utf8Error(e) => {
                    CommandResult::new_error(helpers::to_cstr_ptr_or_null(e.to_string().as_str()))
                }
                Utf8OrEmptyError::Empty => CommandResult::new_error(helpers::to_cstr_ptr_or_null(
                    "Null value passed for host",
                )),
            };
        }
    };
    let cmd = match in_request_type.get_command() {
        None => {
            logger_core::log_error(
                "csharp_ffi",
                "Error in csharp_command called with unknown request type",
            );
            return CommandResult::new_error(helpers::to_cstr_ptr_or_null("Unknown request type"));
        }
        Some(d) => d,
    };
    let callback = in_callback;
    let callback_data = in_callback_data as usize;

    let ffi_handle = unsafe { Box::leak(Box::from_raw(in_client_ptr as *mut FFIHandle)) };
    let handle = ffi_handle.handle.clone();
    let routing_info = if in_routing_info.is_null() {
        None
    } else {
        Some(unsafe {
            match (*in_routing_info).to_redis() {
                Ok(d) => d,
                Err(e) => {
                    logger_core::log_error(
                        "csharp_ffi",
                        format!(
                            "Error while parsing route in string transformation: {:?}",
                            e.to_string()
                        ),
                    );
                    return match e {
                        Utf8OrEmptyError::Utf8Error(e) => CommandResult::new_error(
                            helpers::to_cstr_ptr_or_null(e.to_string().as_str()),
                        ),
                        Utf8OrEmptyError::Empty => {
                            CommandResult::new_error(helpers::to_cstr_ptr_or_null(
                                "Routing info incomplete, null value passed in string",
                            ))
                        }
                    };
                }
            }
        })
    };
    ffi_handle.runtime.spawn(async move {
        let args = args;
        logger_core::log_trace("csharp_ffi", "Entered command task with");
        let data: redis::Value = match handle.command(cmd, args.as_slice(), routing_info).await {
            Ok(d) => d,
            Err(e) => {
                logger_core::log_error(
                    "csharp_ffi",
                    format!(
                        "Error handling command in task of csharp_command: {:?}",
                        e.to_string()
                    ),
                );
                let value = Value::simple_string_with_null(e.to_string().as_str());
                match catch_unwind(|| unsafe {
                    logger_core::log_trace(
                        "csharp_ffi",
                        "Calling command callback of csharp_command",
                    );
                    callback(callback_data as *mut c_void, false as c_int, value);
                    logger_core::log_trace(
                        "csharp_ffi",
                        "Called command callback of csharp_command",
                    );
                }) {
                    Err(e) => logger_core::log_error(
                        "csharp_ffi",
                        format!("Exception in C# callback: {:?}", e),
                    ),
                    _ => {}
                };
                return;
            }
        };
        unsafe {
            let mut buffer = FFIBuffer::new();

            // "Simulation" run
            _ = Value::from_redis(&data, &mut buffer);
            buffer.switch_mode();

            match Value::from_redis(&data, &mut buffer) {
                Ok(data) => {
                    match catch_unwind(|| {
                        logger_core::log_trace(
                            "csharp_ffi",
                            "Calling command callback of csharp_command",
                        );
                        callback(callback_data as *mut c_void, true as c_int, data);
                        logger_core::log_trace(
                            "csharp_ffi",
                            "Called command callback of csharp_command",
                        );
                    }) {
                        Err(e) => logger_core::log_error(
                            "csharp_ffi",
                            format!("Exception in C# callback: {:?}", e),
                        ),
                        _ => {}
                    }
                }
                Err(e) => {
                    logger_core::log_error(
                        "csharp_ffi",
                        format!(
                            "Error transforming command result in task of csharp_command: {:?}",
                            e.to_string()
                        ),
                    );
                    match catch_unwind(|| {
                        logger_core::log_trace(
                            "csharp_ffi",
                            "Calling command callback of csharp_command",
                        );
                        callback(
                            callback_data as *mut c_void,
                            false as c_int,
                            Value::simple_string_with_null(e.to_string().as_str()),
                        );
                        logger_core::log_trace(
                            "csharp_ffi",
                            "Called command callback of csharp_command",
                        );
                    }) {
                        Err(e) => logger_core::log_error(
                            "csharp_ffi",
                            format!("Exception in C# callback: {:?}", e),
                        ),
                        _ => {}
                    };
                }
            }
        }

        logger_core::log_trace("csharp_ffi", "Exiting tokio spawn from csharp_command");
    });

    logger_core::log_trace("csharp_ffi", "Exiting csharp_command");
    CommandResult::new_success()
}

#[cfg(test)]
mod tests {
    #[allow(dead_code)]
    const HOST: &str = "localhost";
    #[allow(dead_code)]
    const PORT: u16 = 49493;
}
