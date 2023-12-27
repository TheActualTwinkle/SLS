use std::{
    collections::HashMap,
    net::{Ipv4Addr, SocketAddr},
    sync::{Arc, OnceLock},
};

use serde::{Deserialize, Serialize};
use tokio::sync::RwLock;
use uuid::Uuid;

#[derive(Serialize, Deserialize, Clone)]
pub struct Lobby {
    id: Uuid,
    public_addr: SocketAddr,
    max_seats: usize,
    player_count: usize,
    lobby_name: String,
}

#[derive(Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct CreateLobby {
    pub address: String,
    pub port: String,
    pub max_seats: usize,
    pub player_count: usize,
    pub lobby_name: String,
}

#[derive(Debug)]
pub enum LobbyError {
    AddressParseError,
    PortParseError,
}

impl Lobby {
    pub fn create(id: Uuid, create: &CreateLobby) -> Result<Self, LobbyError> {
        let address: Ipv4Addr = create
            .address
            .parse()
            .map_err(|_| LobbyError::AddressParseError)?;
        let port: u16 = create
            .port
            .parse()
            .map_err(|_| LobbyError::PortParseError)?;

        let public_addr = SocketAddr::from((address, port));

        Ok(Self {
            id,
            public_addr,
            max_seats: create.max_seats,
            player_count: create.player_count,
            lobby_name: create.lobby_name.clone(),
        })
    }

    pub fn address(&self) -> SocketAddr {
        self.public_addr
    }
}

type Lobbies = Arc<RwLock<HashMap<Uuid, Lobby>>>;

pub fn lobbies() -> &'static Lobbies {
    static LOBBIES: OnceLock<Lobbies> = OnceLock::new();

    LOBBIES.get_or_init(|| Arc::new(RwLock::new(HashMap::new())))
}
