use std::{net::SocketAddr, str::FromStr};

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
        info!("ClientHandler started!");
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

        if str_msg == "get-ids" {
            let keys: Vec<Uuid> = lobbies().read().await.keys().map(|k| *k).collect();
            let msg: Vec<u8> = keys.iter().flat_map(|k| k.as_bytes().to_vec()).collect();

            match stream.try_write(&msg) {
                Ok(_) => (),
                Err(e) => {
                    warn!("Failed to write to socket: {e}");
                    info!("Close connection");
                    break;
                }
            }
        }

        if str_msg.len() >= 8 && &str_msg[..8] == "get-info" {
            let id = match Uuid::from_str(&str_msg[9..n]) {
                Ok(id) => id,
                Err(e) => {
                    warn!("Error parsing UUID: {e}");
                    break;
                }
            };

            // get lobby
            let lobby = lobbies().read().await.get(&id).cloned();
            let msg = serde_json::to_vec(&SdtClientMessageSend::Lobby(
                lobby.map_or(Err(SdtClientError::LobbyNotExist), |lobby| Ok(lobby)),
            ))
            .unwrap();

            // return info
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
