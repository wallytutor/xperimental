use serde::Deserialize;
use std::env;
use std::fs;
use std::io;
use std::net::{Shutdown, TcpListener, TcpStream};
use std::path::Path;
use std::process;
use std::sync::Arc;
use std::thread;

#[derive(Debug, Deserialize, Clone)]
struct EndpointConfig {
    host: String,
    port: u16,
}

impl EndpointConfig {
    fn to_address(&self) -> String {
        format!("{}:{}", self.host, self.port)
    }
}

#[derive(Debug, Deserialize, Clone)]
struct Config {
    target: EndpointConfig,
    listen: EndpointConfig,
}

fn load_config<P: AsRef<Path>>(path: P) -> Result<Config, Box<dyn std::error::Error>> {
    let content = fs::read_to_string(path)?;
    let config: Config = serde_json::from_str(&content)?;
    Ok(config)
}

fn forward(mut source: TcpStream, mut destination: TcpStream) {
    let _ = io::copy(&mut source, &mut destination);
    let _ = destination.shutdown(Shutdown::Both);
    let _ = source.shutdown(Shutdown::Both);
}

fn handle_client(client_stream: TcpStream, target_addr: Arc<String>) {
    let target_stream = match TcpStream::connect(target_addr.as_str()) {
        Ok(stream) => stream,
        Err(err) => {
            eprintln!("Failed to connect to target {}: {}", target_addr, err);
            let _ = client_stream.shutdown(Shutdown::Both);
            return;
        }
    };

    let client_read = match client_stream.try_clone() {
        Ok(s) => s,
        Err(err) => {
            eprintln!("Failed to clone client socket: {}", err);
            return;
        }
    };
    let client_write = client_stream;

    let target_read = match target_stream.try_clone() {
        Ok(s) => s,
        Err(err) => {
            eprintln!("Failed to clone target socket: {}", err);
            return;
        }
    };
    let target_write = target_stream;

    // Client -> Target
    let h1 = thread::spawn(move || {
        forward(client_read, target_write);
    });

    // Target -> Client
    let h2 = thread::spawn(move || {
        forward(target_read, client_write);
    });

    let _ = h1.join();
    let _ = h2.join();
}

fn main() {
    let args: Vec<String> = env::args().collect();
    let config_path = if args.len() > 1 {
        args[1].clone()
    } else {
        "config.json".to_string()
    };

    let config = match load_config(&config_path) {
        Ok(cfg) => cfg,
        Err(err) => {
            eprintln!("Error loading config from '{}': {}", config_path, err);
            eprintln!("Usage: {} [config_file.json]", args.first().map(|s| s.as_str()).unwrap_or("xl-ssh-tunneling"));
            process::exit(1);
        }
    };

    let listen_addr = config.listen.to_address();
    let target_addr = Arc::new(config.target.to_address());

    let listener = match TcpListener::bind(&listen_addr) {
        Ok(l) => l,
        Err(err) => {
            eprintln!("Failed to bind to {}: {}", listen_addr, err);
            process::exit(1);
        }
    };

    println!(
        "Proxy listening on {}, forwarding to {}",
        listen_addr, target_addr
    );

    for incoming in listener.incoming() {
        match incoming {
            Ok(client_stream) => {
                let target_addr_clone = Arc::clone(&target_addr);
                thread::spawn(move || {
                    handle_client(client_stream, target_addr_clone);
                });
            }
            Err(err) => {
                eprintln!("Connection failed: {}", err);
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::{Read, Write};

    #[test]
    fn test_deserialize_config() {
        let json_data = r#"{
            "target": {
                "host": "10.13.52.2",
                "port": 22
            },
            "listen": {
                "host": "127.0.0.1",
                "port": 2222
            }
        }"#;

        let config: Config = serde_json::from_str(json_data).expect("failed to parse JSON");
        assert_eq!(config.target.host, "10.13.52.2");
        assert_eq!(config.target.port, 22);
        assert_eq!(config.target.to_address(), "10.13.52.2:22");

        assert_eq!(config.listen.host, "127.0.0.1");
        assert_eq!(config.listen.port, 2222);
        assert_eq!(config.listen.to_address(), "127.0.0.1:2222");
    }

    #[test]
    fn test_proxy_forwarding() {
        // 1. Mock Target Server
        let target_listener = TcpListener::bind("127.0.0.1:0").expect("bind target listener");
        let target_addr = target_listener.local_addr().unwrap().to_string();

        let server_thread = thread::spawn(move || {
            let (mut stream, _) = target_listener.accept().expect("target accept");
            let mut buf = [0u8; 12];
            stream.read_exact(&mut buf).expect("target read");
            assert_eq!(&buf, b"ping-request");
            stream.write_all(b"pong-reply!!").expect("target write");
        });

        // 2. Mock Proxy
        let proxy_listener = TcpListener::bind("127.0.0.1:0").expect("bind proxy listener");
        let proxy_addr = proxy_listener.local_addr().unwrap().to_string();
        let target_addr_arc = Arc::new(target_addr);

        let proxy_thread = thread::spawn(move || {
            let (client_stream, _) = proxy_listener.accept().expect("proxy accept");
            handle_client(client_stream, target_addr_arc);
        });

        // 3. Mock Client connecting to Proxy
        let mut client = TcpStream::connect(proxy_addr).expect("client connect to proxy");
        client.write_all(b"ping-request").expect("client write");

        let mut reply = [0u8; 12];
        client.read_exact(&mut reply).expect("client read");
        assert_eq!(&reply, b"pong-reply!!");

        let _ = client.shutdown(Shutdown::Both);
        drop(client);

        server_thread.join().expect("join server");
        proxy_thread.join().expect("join proxy");
    }
}


