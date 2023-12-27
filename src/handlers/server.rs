use std::{net::SocketAddr, str::FromStr};

use log::*;
use serde::{Deserialize, Serialize};
use tokio::net::{TcpListener, TcpStream, UdpSocket};
use uuid::Uuid;

use crate::lobby::{lobbies, CreateLobby, Lobby};

#[derive(Serialize, Deserialize)]
enum ServerMessageRecv {
    Ack,
    Bye,
}

#[derive(Serialize, Deserialize)]
enum ServerMessageSend {
    Ack,
    Bye,
    CreateLobby(CreateLobby),
}

pub struct SdtServerHandler {
    listener: TcpListener,
}

impl SdtServerHandler {
    pub async fn new(addr: SocketAddr) -> Self {
        let listener = TcpListener::bind(addr)
            .await
            .expect("Unable to bind TcpListener to provided server socket.");

        Self { listener }
    }

    pub async fn run(&mut self) {
        info!("ServerHandler started!");
        loop {
            let (stream, _) = self.listener.accept().await.unwrap();

            tokio::spawn(async move {
                handle(stream).await;
            });
        }
    }
}

async fn handle(stream: TcpStream) {
    let mut buf = [0; 512];
    let lobby_id = Uuid::new_v4();

    loop {
        stream.writable().await.expect("Socket is unwritable");
        match stream.try_write(&serde_json::to_vec(&ServerMessageSend::Ack).unwrap()) {
            Ok(_) => (),
            Err(e) => {
                warn!("Failed to write to socket: {e}");
                info!("Close connection");
                break;
            }
        }

        stream.readable().await.expect("Socket is unreadable");
        let n = match stream.try_read(&mut buf) {
            Ok(n) if n <= 0 => {
                info!("Client closed connection");
                return;
            }
            Ok(n) => n,
            Err(e) => {
                warn!("Failed to read from socket: {e}.");
                info!("Close connection.");
                break;
            }
        };

        let str_msg = match std::str::from_utf8(&buf[..n]) {
            Ok(msg) => msg,
            Err(e) => {
                warn!("Error reading ClientMessage: {e}");
                break;
            }
        };

        if str_msg == "close" {
            info!("Client closed the connection.");
            break;
        }

        // lobby creation

        let create: CreateLobby = match serde_json::from_slice(&buf[..n]) {
            Ok(create) => create,
            Err(e) => {
                warn!("Error parsing CreateLobby command: {e}");
                break;
            }
        };

        let id = match Uuid::from_str(&str_msg[9..n]) {
            Ok(id) => id,
            Err(e) => {
                warn!("Error parsing UUID: {e}");
                break;
            }
        };

        let lobby = match Lobby::create(id, &create) {
            Ok(lobby) => lobby,
            Err(e) => {
                warn!("Error creation lobby: {e:?}");
                break;
            }
        };

        if snap_server_available(lobby.address()).await {
            lobbies().write().await.insert(id, lobby);
            info!("Lobby {id} created.");
        } else {
            warn!("Lobby {id} is not created: SNAP server is anavailable.");
            break;
        }
    }

    lobbies().write().await.remove(&lobby_id);
    info!("Lobby {lobby_id} closed.");
}

async fn snap_server_available(addr: SocketAddr) -> bool {
    let udp = UdpSocket::bind(addr)
        .await
        .expect("Couldn't bind to address.");

    let mut buf = [0; 48];
    match udp.try_recv(&mut buf) {
        Ok(_) => true,
        Err(_) => false,
    }
}
