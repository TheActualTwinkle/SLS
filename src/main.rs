use std::net::SocketAddr;

use log::*;

use crate::handlers::{SdtClientHandler, SdtServerHandler};

mod handlers;
mod lobby;

#[tokio::main]
async fn main() {
    info!("SDT starting...");

    let server_addr: SocketAddr = SocketAddr::from(([127, 0, 0, 1], 47920));
    let client_addr: SocketAddr = SocketAddr::from(([127, 0, 0, 1], 47921));

    tokio::spawn(async move {
        SdtServerHandler::new(server_addr).await.run().await;
        info!("SDT Server started.");
    });

    tokio::spawn(async move {
        SdtClientHandler::new(client_addr).await.run().await;
        info!("SDT Client started.");
    });
}
