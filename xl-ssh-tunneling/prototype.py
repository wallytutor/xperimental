# -*- coding: utf-8 -*-
from socket import socket
from socket import AF_INET, SOCK_STREAM, SOL_SOCKET, SO_REUSEADDR
from threading import Thread

# The remote HPC server details (target server)
TARGET_HOST = "[IP_ADDRESS]"
TARGET_PORT = 22

# Listen on PC1 (accessible via RDP loopback or local network interface)
LISTEN_HOST = "[IP_ADDRESS]"
LISTEN_PORT = 2222


def handle_client(client_socket):
    try:
        target_socket = socket(AF_INET, SOCK_STREAM)
        target_socket.connect((TARGET_HOST, TARGET_PORT))
    except Exception as e:
        print(f"Failed to connect to target: {e}")
        client_socket.close()
        return

    def forward(source, destination):
        try:
            while True:
                if not (data := source.recv(4096)):
                    break

                destination.sendall(data)
        except Exception:
            pass
        finally:
            source.close()
            destination.close()

    Thread(
        target = forward,
        args   = (client_socket, target_socket),
        daemon = True
    ).start()

    Thread(
        target = forward,
        args   = (target_socket, client_socket),
        daemon = True
    ).start()


def main():
    server = socket(AF_INET, SOCK_STREAM)
    server.setsockopt(SOL_SOCKET, SO_REUSEADDR, 1)
    server.bind((LISTEN_HOST, LISTEN_PORT))
    server.listen(5)

    print(
        f"Proxy listening on port {LISTEN_PORT}, "
        f"forwarding to {TARGET_HOST}:{TARGET_PORT}"
    )

    while True:
        client_sock, _ = server.accept()
        Thread(
            target = handle_client,
            args   = (client_sock,),
            daemon = True
        ).start()


if __name__ == "__main__":
    main()