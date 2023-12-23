use std::net::SocketAddr;

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
        match stream.try_write(&serde_json::to_vec(&ServerMessageSend::Ack).unwrap()) {
            Ok(_) => (),
            Err(e) => {
                warn!("Failed to write to socket: {e}");
                info!("Close connection");
                break;
            }
        }

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

        let msg: ServerMessageSend = match serde_json::from_slice(&buf[..n]) {
            Ok(msg) => msg,
            Err(e) => {
                warn!("Error parsing ClientMessage: {e}");
                break;
            }
        };

        match msg {
            ServerMessageSend::Ack => (),
            ServerMessageSend::Bye => {
                info!("Client closed the connection.");
                break;
            }
            ServerMessageSend::CreateLobby(create) => {
                if !snap_server_available(create.public_addr).await {}

                let lobby = Lobby::create(lobby_id, &create);
                lobbies().write().await.insert(lobby_id, lobby);

                info!("Lobby {lobby_id} created.");
            }
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
