# SLS
SLS - [SnaP](https://github.com/TheActualTwinkle/SnaP) Lobby Service

## What is this?
* Using *TCP/IP* or [*gRPC*](https://learn.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-8.0)  we getting data about Game Lobbies from **SnaP** Server/Host
* Store them 
* Send this data to **SnaP** Clients

To run with *gRPC* use  `-g` agrument
```bash
dotnet run --project SLS -g
```