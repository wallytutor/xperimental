# SSH Tunneling

## Use case

Assume you have a network composed of the following:

- **PC1**: a company machine with access to internal network.
- **PC2**: a remote HPC server visible within internal network.
- **PC3**: an isolated machine with no network visibility.

Because it is more powerful/convenient, you connect from **PC1** to **PC3** through RDP. This application provides a means to have direct SSH access to **PC2** from **PC3**. It will run a TCP Proxy on **PC1** which will then enable **PC3** to connect to **PC2** through SSH.

## Requirements

- [Rust toolchain](https://www.rust-lang.org/) (Cargo and rustc 2024 edition or later).

## Configuration

The application reads network connection parameters from a JSON configuration file (by default `config.json` in the current working directory, or a path provided as a command-line argument):

```json
{
    "target": {
        "host": "[IP_ADDRESS]",
        "port": 22
    },
    "listen": {
        "host": "[IP_ADDRESS]",
        "port": 2222
    }
}
```

- **target**: Host and port of the destination server (e.g. PC2 / remote HPC server).
- **listen**: Host interface and port to bind locally on PC1.

An example configuration file is provided in [`config.example.json`](config.example.json).

## Build

To compile a standalone release executable:

```sh
cargo build --release
```

The resulting standalone binary will be located at:
- **Windows**: `target/release/xl-ssh-tunneling.exe`
- **Linux / macOS**: `target/release/xl-ssh-tunneling`

## Usage

Run the executable by passing an optional configuration file path:

```sh
# Using default config.json
./target/release/xl-ssh-tunneling

# Or specifying custom configuration file
./target/release/xl-ssh-tunneling path/to/config.json
```

