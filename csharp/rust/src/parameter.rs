use crate::apihandle::CommandParameter;
use crate::data::Utf8OrEmptyError;
use crate::helpers;
use std::ffi::{
    c_char, c_double, c_float, c_int, c_longlong, c_short, c_uchar, c_uint, c_ulonglong, c_ushort,
};

#[repr(C)]
pub struct KeyParameterPair {
    pub key: *const c_char,
    pub key_length: c_uint,
    pub value: Parameter,
}
#[repr(C)]
pub enum EParameterKind {
    Bool,
    Int8,
    Uint8,
    Int16,
    Uint16,
    Int32,
    Uint32,
    Int64,
    Uint64,
    Float32,
    Float64,
    String,
    BoolArray,
    Int8Array,
    Uint8Array,
    Int16Array,
    Uint16Array,
    Int32Array,
    Uint32Array,
    Int64Array,
    Uint64Array,
    Float32Array,
    Float64Array,
    KeyValueArray,
}

#[repr(C)]
pub union ParameterValue {
    pub flag: c_char,
    pub i8: c_char,
    pub u8: c_uchar,
    pub i16: c_short,
    pub u16: c_ushort,
    pub i32: c_int,
    pub u32: c_uint,
    pub i64: c_longlong,
    pub u64: c_ulonglong,
    pub f32: c_float,
    pub f64: c_double,
    pub string: *const c_char,
    pub flag_array: *const c_char,
    pub i8_array: *const c_char,
    pub u8_array: *const c_uchar,
    pub i16_array: *const c_short,
    pub u16_array: *const c_ushort,
    pub i32_array: *const c_int,
    pub u32_array: *const c_uint,
    pub i64_array: *const c_longlong,
    pub u64_array: *const c_ulonglong,
    pub f32_array: *const c_float,
    pub f64_array: *const c_double,
    pub key_parameter_array: *const KeyParameterPair,
}
#[repr(C)]
pub struct Parameter {
    pub kind: EParameterKind,
    pub value: ParameterValue,
    pub value_length: c_uint,
}


impl Parameter {
    pub unsafe fn to_command_parameter(&self) -> Result<CommandParameter, Utf8OrEmptyError> {
        Ok(match self.kind {
            EParameterKind::Bool => CommandParameter::Bool(self.value.flag != 0),
            EParameterKind::Int8 => CommandParameter::Int8(self.value.i8),
            EParameterKind::Uint8 => CommandParameter::Uint8(self.value.u8),
            EParameterKind::Int16 => CommandParameter::Int16(self.value.i16),
            EParameterKind::Uint16 => CommandParameter::Uint16(self.value.u16),
            EParameterKind::Int32 => CommandParameter::Int32(self.value.i32),
            EParameterKind::Uint32 => CommandParameter::Uint32(self.value.u32),
            EParameterKind::Int64 => CommandParameter::Int64(self.value.i64),
            EParameterKind::Uint64 => CommandParameter::Uint64(self.value.u64),
            EParameterKind::Float32 => CommandParameter::Float32(self.value.f32),
            EParameterKind::Float64 => CommandParameter::Float64(self.value.f64),
            EParameterKind::String => {
                let str = helpers::grab_str_not_null(self.value.string)?;
                CommandParameter::String(str)
            }
            EParameterKind::BoolArray => {
                let arr =
                    helpers::grab_vec(self.value.flag_array, self.value_length as usize, |flag| {
                        Ok::<bool, ()>(*flag != 0)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::BoolArray(arr)
            }
            EParameterKind::Int8Array => {
                let arr =
                    helpers::grab_vec(self.value.i8_array, self.value_length as usize, |i8| {
                        Ok::<i8, ()>(*i8)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Int8Array(arr)
            }
            EParameterKind::Uint8Array => {
                let arr =
                    helpers::grab_vec(self.value.u8_array, self.value_length as usize, |u8| {
                        Ok::<u8, ()>(*u8)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Uint8Array(arr)
            }
            EParameterKind::Int16Array => {
                let arr =
                    helpers::grab_vec(self.value.i16_array, self.value_length as usize, |i16| {
                        Ok::<i16, ()>(*i16)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Int16Array(arr)
            }
            EParameterKind::Uint16Array => {
                let arr =
                    helpers::grab_vec(self.value.u16_array, self.value_length as usize, |u16| {
                        Ok::<u16, ()>(*u16)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Uint16Array(arr)
            }
            EParameterKind::Int32Array => {
                let arr =
                    helpers::grab_vec(self.value.i32_array, self.value_length as usize, |i32| {
                        Ok::<i32, ()>(*i32)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Int32Array(arr)
            }
            EParameterKind::Uint32Array => {
                let arr =
                    helpers::grab_vec(self.value.u32_array, self.value_length as usize, |u32| {
                        Ok::<u32, ()>(*u32)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Uint32Array(arr)
            }
            EParameterKind::Int64Array => {
                let arr =
                    helpers::grab_vec(self.value.i64_array, self.value_length as usize, |i64| {
                        Ok::<i64, ()>(*i64)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Int64Array(arr)
            }
            EParameterKind::Uint64Array => {
                let arr =
                    helpers::grab_vec(self.value.u64_array, self.value_length as usize, |u64| {
                        Ok::<u64, ()>(*u64)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Uint64Array(arr)
            }
            EParameterKind::Float32Array => {
                let arr =
                    helpers::grab_vec(self.value.f32_array, self.value_length as usize, |f32| {
                        Ok::<f32, ()>(*f32)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Float32Array(arr)
            }
            EParameterKind::Float64Array => {
                let arr =
                    helpers::grab_vec(self.value.f64_array, self.value_length as usize, |f64| {
                        Ok::<f64, ()>(*f64)
                    })
                    .unwrap(); // Safe because the grab func will never return non-ok values
                CommandParameter::Float64Array(arr)
            }
            EParameterKind::KeyValueArray => {
                let arr = helpers::grab_vec(
                    self.value.key_parameter_array,
                    self.value_length as usize,
                    |pair| {
                        let key = helpers::grab_str_not_null(pair.key)?;
                        let value = pair.value.to_command_parameter()?;
                        Ok::<(std::string::String, CommandParameter), Utf8OrEmptyError>((key, value))
                    },
                )?;
                CommandParameter::KeyValueArray(arr)
            }
        })
    }
}