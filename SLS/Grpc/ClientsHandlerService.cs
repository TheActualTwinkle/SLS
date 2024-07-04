using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SLS.Grpc;

// Constructor with optional parameter is REQUIRED in case of client procedure call. Exception raised otherwise.
public class ClientsHandlerService(string? url = null) : ClientsHandler.ClientsHandlerBase, IClientsHandler
{
    private WebApplication? _app;
    
    public async Task Run()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Add services to the container.
        builder.Services.AddGrpc();

        _app = builder.Build();
        
        // Configure the HTTP request pipeline.
        _app.MapGrpcService<ClientsHandlerService>();
        _app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client.");
        
        await _app.RunAsync(url);
    }

    public async Task Stop()
    {
        if (_app == null)
        {
            return;
        }
        
        await _app.StopAsync();
    }
    
    public override Task<GetGuidsResponse> GetGuids(Empty _, ServerCallContext context)
    {
        GetGuidsResponse getGuidsResponse = new()
        {
            Guids = { Program.LobbyInfos.Keys.Select(x => x.ToString()) }
        };
        
        return Task.FromResult(getGuidsResponse);
    }

    public override Task<GetLobbyInfoResponse> GetLobbyInfo(GetLobbyInfoRequest request, ServerCallContext context)
    {
        if (Guid.TryParse(request.Guid, out Guid guid) == false)
        {
            return Task.FromResult(new GetLobbyInfoResponse());
        }

        if (Program.LobbyInfos.TryGetValue(guid, out LobbyDto? lobbyInfo) == false)
        {
            return Task.FromResult(new GetLobbyInfoResponse());
        }
        
        return Task.FromResult(new GetLobbyInfoResponse
        {
            PublicIpAddress = lobbyInfo.PublicIpAddress,
            Port = lobbyInfo.Port,
            MaxSeats = lobbyInfo.MaxSeats,
            PlayersCount = lobbyInfo.PlayersCount,
            LobbyName = lobbyInfo.LobbyName
        });
    }
}