// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

extern crate core;

mod apihandle;
mod data;
mod helpers;

use crate::apihandle::Handle;
use crate::data::*;
use glide_core::client::ConnectionError;
use glide_core::request_type::RequestType;
use std::ffi::{c_int, c_void, CString};
use std::os::raw::c_char;
use std::panic::catch_unwind;
use std::ptr::null;
use std::str::FromStr;
use tokio::runtime::Builder;

/// # Summary
/// Special method to free the returned values.
/// MUST be used!
#[no_mangle]
pub unsafe extern "C-unwind" fn csharp_free_value(mut input: Value) {
    logger_core::log_trace("csharp_ffi", format!("Entered csharp_free_value to free {:?}", input.kind));
    input.free_data();
    logger_core::log_trace("csharp_ffi", "Exiting csharp_free_value");
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
/// Initializes essential parts of the system.
/// Supposed to be called once only.
///
/// # Parameters
/// - ***in_minimal_level*** The minimum file log level
/// - ***in_file_name*** The file name to log to
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
pub extern "C-unwind" fn csharp_system_init(
    in_minimal_level: ELoggerLevel,
    in_file_name: *const c_char,
) -> InitResult {
    logger_core::log_trace("csharp_ffi", "Entered csharp_system_init");
    // ToDo: Rebuild into having a log-callback so that we can manage logging at the dotnet side
    let file_name = match helpers::grab_str(in_file_name) {
        Ok(d) => d,
        Err(_) => {
            return InitResult {
                logger_level: ELoggerLevel::Off,
                success: false as c_int,
            }
        }
    };
    let logger_level = match file_name {
        None => logger_core::init(
            match in_minimal_level {
                ELoggerLevel::Error => Some(logger_core::Level::Error),
                ELoggerLevel::Warn => Some(logger_core::Level::Warn),
                ELoggerLevel::Info => Some(logger_core::Level::Info),
                ELoggerLevel::Debug => Some(logger_core::Level::Debug),
                ELoggerLevel::Trace => Some(logger_core::Level::Trace),
                ELoggerLevel::Off => Some(logger_core::Level::Off),
                ELoggerLevel::None => None,
            },
            None,
        ),
        Some(file_name) => logger_core::init(
            match in_minimal_level {
                ELoggerLevel::Error => Some(logger_core::Level::Error),
                ELoggerLevel::Warn => Some(logger_core::Level::Warn),
                ELoggerLevel::Info => Some(logger_core::Level::Info),
                ELoggerLevel::Debug => Some(logger_core::Level::Debug),
                ELoggerLevel::Trace => Some(logger_core::Level::Trace),
                ELoggerLevel::Off => Some(logger_core::Level::Off),
                ELoggerLevel::None => None,
            },
            Some(file_name.as_str()),
        ),
    };

    logger_core::log_trace("csharp_ffi", "Exiting csharp_system_init");
    InitResult {
        success: true as c_int,
        logger_level: match logger_level {
            logger_core::Level::Error => ELoggerLevel::Error,
            logger_core::Level::Warn => ELoggerLevel::Warn,
            logger_core::Level::Info => ELoggerLevel::Info,
            logger_core::Level::Debug => ELoggerLevel::Debug,
            logger_core::Level::Trace => ELoggerLevel::Trace,
            logger_core::Level::Off => ELoggerLevel::Off,
        },
    }
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
/// ***in_callback_data*** The data to be passed in to *in_callback*
/// ***in_request_type*** The type of command to issue
/// ***in_args*** A C-String array of arguments to be passed, with the size of `in_args_count` and zero terminated.
/// ***in_args_count*** The number of arguments in *in_args*
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
    // ToDo: Rework into parameter struct (understand how Command.arg(...) works first)
    //       handling the different input types.
    in_args: *const *const c_char,
    in_args_count: c_int,
) -> CommandResult {
    logger_core::log_trace("csharp_ffi", "Entered csharp_command");
    if in_client_ptr.is_null() {
        logger_core::log_error(
            "csharp_ffi",
            "Error in csharp_command called with null handle",
        );
        return CommandResult::new_error(helpers::to_cstr_ptr_or_null("Null handle passed"));
    }
    let args = match helpers::grab_vec_str(in_args, in_args_count as usize) {
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
    ffi_handle.runtime.spawn(async move {
        logger_core::log_trace("csharp_ffi", "Entered command task with");
        let data: redis::Value = match handle.command(cmd, args).await {
            Ok(d) => d,
            Err(e) => {
                logger_core::log_error("csharp_ffi", format!("Error handling command in task of csharp_command: {:?}", e.to_string()));
                let value = Value::simple_string_with_null(e.to_string().as_str());
                match catch_unwind(|| unsafe {
                    logger_core::log_trace("csharp_ffi", "Calling command callback of csharp_command");
                    callback(
                        callback_data as *mut c_void,
                        false as c_int,
                        value,
                    );
                    logger_core::log_trace("csharp_ffi", "Called command callback of csharp_command");
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
            match Value::from_redis(&data) {
                Ok(data) => {
                    match catch_unwind(|| {
                        logger_core::log_trace("csharp_ffi", "Calling command callback of csharp_command");
                        callback(callback_data as *mut c_void, true as c_int, data);
                        logger_core::log_trace("csharp_ffi", "Called command callback of csharp_command");
                    }) {
                        Err(e) => logger_core::log_error(
                            "csharp_ffi",
                            format!("Exception in C# callback: {:?}", e),
                        ),
                        _ => {}
                    }
                }
                Err(e) => {
                    logger_core::log_error("csharp_ffi", format!("Error transforming command result in task of csharp_command: {:?}", e.to_string()));
                    match catch_unwind(|| {
                        logger_core::log_trace("csharp_ffi", "Calling command callback of csharp_command");
                        callback(
                            callback_data as *mut c_void,
                            false as c_int,
                            Value::simple_string_with_null(e.to_string().as_str()),
                        );
                        logger_core::log_trace("csharp_ffi", "Called command callback of csharp_command");
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
#[no_mangle]
pub extern "C-unwind" fn csharp_command_blocking(
    in_client_ptr: *const c_void,
    in_request_type: RequestType,
    in_args: *const *const c_char,
    in_args_count: c_int,
) -> BlockingCommandResult {
    logger_core::log_trace("csharp_ffi", "Entered csharp_command_blocking");
    if in_client_ptr.is_null() {
        return BlockingCommandResult::new_error(helpers::to_cstr_ptr_or_null(
            "Null handle passed",
        ));
    }
    let args = match helpers::grab_vec_str(in_args, in_args_count as usize) {
        Ok(d) => d,
        Err(e) => match e {
            Utf8OrEmptyError::Utf8Error(e) => {
                return BlockingCommandResult::new_error(helpers::to_cstr_ptr_or_null(
                    e.to_string().as_str(),
                ))
            }
            Utf8OrEmptyError::Empty => {
                return BlockingCommandResult::new_error(helpers::to_cstr_ptr_or_null(
                    "Null value passed for host",
                ))
            }
        },
    };
    let cmd = match in_request_type.get_command() {
        None => {
            return BlockingCommandResult::new_error(helpers::to_cstr_ptr_or_null(
                "Unknown request type",
            ))
        }
        Some(d) => d,
    };


    let ffi_handle = unsafe { Box::leak(Box::from_raw(in_client_ptr as *mut FFIHandle)) };
    let result: BlockingCommandResult;
    {
        let _runtime_handle = ffi_handle.runtime.enter();
        let handle = ffi_handle.handle.clone();
        result = ffi_handle.runtime.block_on(async move {
            logger_core::log_trace("csharp_ffi", "Entered command task");
            let data = match handle.command(cmd, args).await {
                Ok(d) => d,
                Err(e) => {
                    let message = helpers::to_cstr_ptr_or_null(e.to_string().as_str());
                    logger_core::log_error(
                        "csharp_ffi",
                        format!("Error handling command: {:?}", e),
                    );
                    return BlockingCommandResult::new_error(message);
                }
            };
            return match Value::from_redis(&data) {
                Ok(data) => BlockingCommandResult::new_success(data),
                Err(e) => {
                    let message = helpers::to_cstr_ptr_or_null(e.to_string().as_str());
                    logger_core::log_error(
                        "csharp_ffi",
                        format!("Error transforming result: {:?}", e),
                    );
                    BlockingCommandResult::new_error(message)
                }
            };
        });
    }
    logger_core::log_trace("csharp_ffi", "Exiting csharp_command_blocking");
    result
}

#[cfg(test)]
mod tests {
    const HOST: &str = "localhost";
    const PORT: u16 = 49493;
}
