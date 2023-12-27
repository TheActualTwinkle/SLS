use std::net::SocketAddr;

use log::*;

use crate::handlers::{SdtClientHandler, SdtServerHandler};

mod handlers;
mod lobby;

#[tokio::main]
async fn main() {
    tracing_subscriber::fmt::init();

    info!("SNAP Data Transfer Console App");

    let server_addr: SocketAddr = SocketAddr::from(([127, 0, 0, 1], 47920));
    let client_addr: SocketAddr = SocketAddr::from(([127, 0, 0, 1], 47921));

    tokio::spawn(async move {
        SdtServerHandler::new(server_addr).await.run().await;
    });

    tokio::spawn(async move {
        SdtClientHandler::new(client_addr).await.run().await;
    });

    loop {}
}
