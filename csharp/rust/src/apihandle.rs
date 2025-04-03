use glide_core::client::{Client, ConnectionError};
use glide_core::ConnectionRequest;
use redis::cluster_routing::RoutingInfo;
use redis::{Cmd, RedisError, RedisWrite, ToRedisArgs, Value};

#[derive(Clone)]
pub(crate) struct Handle {
    client: Client,
}

impl Handle {
    pub async fn create(request: ConnectionRequest) -> Result<Self, ConnectionError> {
        let client = Client::new(request, None).await?;
        Ok(Self { client })
    }

    pub async fn command(
        &self,
        mut cmd: Cmd,
        args: &[CommandParameter],
        routing: Option<RoutingInfo>,
    ) -> Result<Value, RedisError> {
        let mut clone = self.client.clone();
        for arg in args {
            cmd.arg(arg);
        }
        logger_core::log_trace("csharp_ffi::Handle", format!("Sending command {:?}", cmd));
        let result = match clone.send_command(&cmd, routing).await {
            Ok(d) => d,
            Err(e) => return Err(e),
        };
        Ok(result)
    }
}

pub enum CommandParameter {
    Bool(bool),
    Int8(i8),
    Uint8(u8),
    Int16(i16),
    Uint16(u16),
    Int32(i32),
    Uint32(u32),
    Int64(i64),
    Uint64(u64),
    Float32(f32),
    Float64(f64),
    String(String),
    BoolArray(Vec<bool>),
    Int8Array(Vec<i8>),
    Uint8Array(Vec<u8>),
    Int16Array(Vec<i16>),
    Uint16Array(Vec<u16>),
    Int32Array(Vec<i32>),
    Uint32Array(Vec<u32>),
    Int64Array(Vec<i64>),
    Uint64Array(Vec<u64>),
    Float32Array(Vec<f32>),
    Float64Array(Vec<f64>),
    KeyValueArray(Vec<(String, CommandParameter)>),
}
impl ToRedisArgs for CommandParameter {
    fn write_redis_args<W>(&self, out: &mut W)
    where
        W: ?Sized + RedisWrite,
    {
        match self {
            CommandParameter::Bool(value) => value.write_redis_args(out),
            CommandParameter::Int8(value) => value.write_redis_args(out),
            CommandParameter::Uint8(value) => value.write_redis_args(out),
            CommandParameter::Int16(value) => value.write_redis_args(out),
            CommandParameter::Uint16(value) => value.write_redis_args(out),
            CommandParameter::Int32(value) => value.write_redis_args(out),
            CommandParameter::Uint32(value) => value.write_redis_args(out),
            CommandParameter::Int64(value) => value.write_redis_args(out),
            CommandParameter::Uint64(value) => value.write_redis_args(out),
            CommandParameter::Float32(value) => value.write_redis_args(out),
            CommandParameter::Float64(value) => value.write_redis_args(out),
            CommandParameter::String(value) => value.write_redis_args(out),
            CommandParameter::BoolArray(value) => value.write_redis_args(out),
            CommandParameter::Int8Array(value) => value.write_redis_args(out),
            CommandParameter::Uint8Array(value) => value.write_redis_args(out),
            CommandParameter::Int16Array(value) => value.write_redis_args(out),
            CommandParameter::Uint16Array(value) => value.write_redis_args(out),
            CommandParameter::Int32Array(value) => value.write_redis_args(out),
            CommandParameter::Uint32Array(value) => value.write_redis_args(out),
            CommandParameter::Int64Array(value) => value.write_redis_args(out),
            CommandParameter::Uint64Array(value) => value.write_redis_args(out),
            CommandParameter::Float32Array(value) => value.write_redis_args(out),
            CommandParameter::Float64Array(value) => value.write_redis_args(out),
            CommandParameter::KeyValueArray(_value) => {
                todo!("Implement KeyValueArray")
            }
        }
    }
}
