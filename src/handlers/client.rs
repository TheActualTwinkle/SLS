use std::net::SocketAddr;

use log::*;
use serde::{Deserialize, Serialize};
use tokio::net::{TcpListener, TcpStream};
use uuid::Uuid;

use crate::lobby::{lobbies, Lobby};

#[derive(Debug, Serialize, Deserialize)]
enum SdtClientError {
    LobbyNotExist,
}

#[derive(Serialize, Deserialize)]
enum SdtClientMessageSend {
    Ack,
    Bye,
    Lobby(Result<Lobby, SdtClientError>),
    LobbyIds(Vec<Uuid>),
}

#[derive(Serialize, Deserialize)]
enum SdtClientMessageRecv {
    Ack,
    Bye,
    GetLobby(Uuid),
    GetLobbyIds,
}

pub struct SdtClientHandler {
    listener: TcpListener,
}

impl SdtClientHandler {
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

    loop {
        match stream.try_write(&serde_json::to_vec(&SdtClientMessageSend::Ack).unwrap()) {
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

        let msg: SdtClientMessageRecv = match serde_json::from_slice(&buf[..n]) {
            Ok(msg) => msg,
            Err(e) => {
                warn!("Error parsing ClientMessage: {e}");
                break;
            }
        };

        match msg {
            SdtClientMessageRecv::Ack => (),
            SdtClientMessageRecv::Bye => {
                info!("Client closed the connection.");
                break;
            }
            SdtClientMessageRecv::GetLobby(id) => {
                let lobby = lobbies().read().await.get(&id).cloned();
                let msg = serde_json::to_vec(&SdtClientMessageSend::Lobby(
                    lobby.map_or(Err(SdtClientError::LobbyNotExist), |lobby| Ok(lobby)),
                ))
                .unwrap();

                match stream.try_write(&msg) {
                    Ok(_) => (),
                    Err(e) => {
                        warn!("Failed to write to socket: {e}");
                        info!("Close connection");
                        break;
                    }
                }
            }
            SdtClientMessageRecv::GetLobbyIds => {
                let keys: Vec<Uuid> = lobbies().read().await.keys().map(|k| *k).collect();
                let msg = serde_json::to_vec(&SdtClientMessageSend::LobbyIds(keys)).unwrap();

                match stream.try_write(&msg) {
                    Ok(_) => (),
                    Err(e) => {
                        warn!("Failed to write to socket: {e}");
                        info!("Close connection");
                        break;
                    }
                }
            }
        }
    }
}
