use std::{
    collections::HashMap,
    net::SocketAddr,
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
pub struct CreateLobby {
    pub public_addr: SocketAddr,
    pub max_seats: usize,
    pub player_count: usize,
    pub lobby_name: String,
}

impl Lobby {
    pub fn create(id: Uuid, create: &CreateLobby) -> Self {
        Self {
            id,
            public_addr: create.public_addr,
            max_seats: create.max_seats,
            player_count: create.player_count,
            lobby_name: create.lobby_name.clone(),
        }
    }
}

type Lobbies = Arc<RwLock<HashMap<Uuid, Lobby>>>;

pub fn lobbies() -> &'static Lobbies {
    static LOBBIES: OnceLock<Lobbies> = OnceLock::new();

    LOBBIES.get_or_init(|| Arc::new(RwLock::new(HashMap::new())))
}
