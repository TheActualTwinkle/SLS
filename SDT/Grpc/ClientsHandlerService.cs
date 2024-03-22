using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SDT.Grpc;

// Constructor with optional parameter is REQUIRED in case of client procedure call. Exception raised otherwise.
public class ClientsHandlerService(string? url = null) : ClientsHandler.ClientsHandlerBase, IClientsHandler
{
    public async Task Run()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Add services to the container.
        builder.Services.AddGrpc();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<ClientsHandlerService>();
        app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client.");
        
        await app.RunAsync(url);
    }
    
    public override Task<GetGuidsResponse> GetGuids(GetGuidsRequest request, ServerCallContext context)
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
            return null!;
        }

        if (Program.LobbyInfos.TryGetValue(guid, out LobbyInfo? lobbyInfo) == false)
        {
            return null!;
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